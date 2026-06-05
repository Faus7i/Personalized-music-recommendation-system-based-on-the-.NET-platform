using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MusicRec.BuildingBlocks.Contracts.Auth;
using MusicRec.BuildingBlocks.Shared.ServiceDefaults;
using MusicRec.Services.Identity.Api.Data;
using MusicRec.Services.Identity.Api.Data.Entities;
using MusicRec.Services.Identity.Api.Services;
using System.Net.Mail;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("IdentityDatabase")));

builder.Services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
builder.Services.AddProblemDetails();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtOptions = new JwtTokenOptions(
    Issuer: jwtSection["Issuer"] ?? "MusicRec.Identity.Api",
    Audience: jwtSection["Audience"] ?? "MusicRec.Client",
    SecretKey: jwtSection["SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey is required."),
    ValidFor: TimeSpan.FromHours(int.Parse(jwtSection["ValidForHours"] ?? "24"))
);
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddScoped<JwtTokenService>();

var app = builder.Build();
var startedAtUtc = DateTimeOffset.UtcNow;

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await Results.Problem(
            title: "Unhandled identity error",
            detail: exception?.Message,
            statusCode: StatusCodes.Status500InternalServerError)
            .ExecuteAsync(context);
    });
});
app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new ServiceMetadata("identity-api", "v0.3.0", startedAtUtc)));

var identityGroup = app.MapGroup("/api/identity").WithTags("Identity");

identityGroup.MapPost("/register", async (
    RegisterRequest request,
    IdentityDbContext dbContext,
    IPasswordHasher<UserAccount> passwordHasher,
    JwtTokenService jwtTokenService) =>
{
    var userName = request.UserName.Trim();
    var email = request.Email.Trim();
    var phoneNumber = NormalizePhoneNumber(request.PhoneNumber);

    if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["UserName"] = ["User name is required."],
            ["Email"] = ["Email is required."]
        });
    }

    if (!IsValidEmail(email))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["Email"] = ["Email format is invalid."]
        });
    }

    if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && phoneNumber is null)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["PhoneNumber"] = ["Phone number format is invalid."]
        });
    }

    if (request.Password != request.ConfirmPassword)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["ConfirmPassword"] = ["Password and confirmation do not match."]
        });
    }

    if (!IsStrongPassword(request.Password))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["Password"] = ["Password must be at least 8 characters long and contain both letters and numbers."]
        });
    }

    var normalizedUserName = userName.ToUpperInvariant();
    var normalizedEmail = email.ToUpperInvariant();
    var hasPhoneNumber = !string.IsNullOrWhiteSpace(phoneNumber);

    var exists = await dbContext.UserAccounts.AnyAsync(x =>
        x.NormalizedUserName == normalizedUserName ||
        x.NormalizedEmail == normalizedEmail ||
        (hasPhoneNumber && x.NormalizedPhoneNumber == phoneNumber));

    if (exists)
    {
        return Results.Problem(
            title: "Account already exists",
            detail: "The user name, email or phone number is already registered.",
            statusCode: StatusCodes.Status409Conflict);
    }

    var user = new UserAccount
    {
        Id = Guid.NewGuid(),
        UserName = userName,
        NormalizedUserName = normalizedUserName,
        Email = email,
        NormalizedEmail = normalizedEmail,
        PhoneNumber = request.PhoneNumber?.Trim(),
        NormalizedPhoneNumber = phoneNumber,
        DisplayName = userName,
        CreatedAtUtc = DateTimeOffset.UtcNow
    };

    user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

    dbContext.UserAccounts.Add(user);
    await dbContext.SaveChangesAsync();

    var token = jwtTokenService.GenerateToken(user.Id, user.UserName);

    return Results.Created($"/api/identity/users/{user.Id}", new AuthResponse(
        user.Id,
        user.UserName,
        token,
        DateTimeOffset.UtcNow.AddHours(24)));
});

identityGroup.MapPost("/login", async (
    LoginRequest request,
    IdentityDbContext dbContext,
    IPasswordHasher<UserAccount> passwordHasher,
    JwtTokenService jwtTokenService) =>
{
    if (string.IsNullOrWhiteSpace(request.UserNameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["UserNameOrEmail"] = ["Account, email or phone number is required."],
            ["Password"] = ["Password is required."]
        });
    }

    var normalizedInput = request.UserNameOrEmail.Trim().ToUpperInvariant();
    var normalizedPhone = NormalizePhoneNumber(request.UserNameOrEmail);
    var hasNormalizedPhone = !string.IsNullOrWhiteSpace(normalizedPhone);

    var user = await dbContext.UserAccounts.FirstOrDefaultAsync(x =>
        x.NormalizedUserName == normalizedInput ||
        x.NormalizedEmail == normalizedInput ||
        (hasNormalizedPhone && x.NormalizedPhoneNumber == normalizedPhone));

    if (user is null || !user.IsActive)
    {
        return Results.Problem(
            title: "Authentication failed",
            detail: "Invalid account or password.",
            statusCode: StatusCodes.Status401Unauthorized);
    }

    var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
    if (verificationResult == PasswordVerificationResult.Failed)
    {
        return Results.Problem(
            title: "Authentication failed",
            detail: "Invalid account or password.",
            statusCode: StatusCodes.Status401Unauthorized);
    }

    user.LastLoginAtUtc = DateTimeOffset.UtcNow;
    await dbContext.SaveChangesAsync();

    var token = jwtTokenService.GenerateToken(user.Id, user.UserName);

    return Results.Ok(new AuthResponse(
        user.Id,
        user.UserName,
        token,
        DateTimeOffset.UtcNow.AddHours(24)));
});

app.Run();

static bool IsValidEmail(string email)
{
    try
    {
        _ = new MailAddress(email);
        return true;
    }
    catch
    {
        return false;
    }
}

static string? NormalizePhoneNumber(string? phoneNumber)
{
    if (string.IsNullOrWhiteSpace(phoneNumber))
    {
        return null;
    }

    var normalized = Regex.Replace(phoneNumber, "[^0-9+]", string.Empty);
    if (normalized.Length is < 7 or > 20)
    {
        return null;
    }

    return normalized.ToUpperInvariant();
}

static bool IsStrongPassword(string password)
{
    if (password.Length < 8)
    {
        return false;
    }

    return password.Any(char.IsLetter) && password.Any(char.IsDigit);
}

public partial class Program;
