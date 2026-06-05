using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MusicRec.BuildingBlocks.Shared.ServiceDefaults;
using MusicRec.Services.Recommendation.Api.Data;
using MusicRec.Services.Recommendation.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RecommendationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RecommendationDatabase")));
builder.Services.AddScoped<HybridRecommendationService>();
builder.Services.AddScoped<IRecommendationAlgorithm, ContentBasedRecommendationAlgorithm>();
builder.Services.AddScoped<IRecommendationAlgorithm, CollaborativeFilteringRecommendationAlgorithm>();
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtIssuer = jwtSection["Issuer"] ?? "MusicRec.Identity.Api";
var jwtAudience = jwtSection["Audience"] ?? "MusicRec.Client";
var jwtSecretKey = jwtSection["SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey is required.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();

var app = builder.Build();
var startedAtUtc = DateTimeOffset.UtcNow;

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await Results.Problem(
            title: "Unhandled recommendation error",
            detail: exception?.Message,
            statusCode: StatusCodes.Status500InternalServerError)
            .ExecuteAsync(context);
    });
});
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new ServiceMetadata("recommendation-api", "v0.4.0", startedAtUtc)));

var recommendationGroup = app.MapGroup("/api/recommendations").WithTags("Recommendations");

recommendationGroup.MapGet("/{userId:guid}", async (
    Guid userId,
    Guid[]? excludeSongIds,
    HttpContext httpContext,
    HybridRecommendationService recommendationService,
    CancellationToken cancellationToken) =>
{
    var authorizationResult = EnsureAuthorizedUser(httpContext.User, userId);
    if (authorizationResult is not null)
    {
        return authorizationResult;
    }

    var result = await recommendationService.GetRecommendationsAsync(
        userId,
        excludeSongIds ?? [],
        cancellationToken);

    return Results.Ok(result);
});

recommendationGroup.MapGet("/{userId:guid}/evaluate", async (
    Guid userId,
    HttpContext httpContext,
    HybridRecommendationService recommendationService,
    CancellationToken cancellationToken) =>
{
    var authorizationResult = EnsureAuthorizedUser(httpContext.User, userId);
    if (authorizationResult is not null)
    {
        return authorizationResult;
    }

    var evaluation = await recommendationService.EvaluateAsync(userId, cancellationToken);
    return Results.Ok(evaluation);
});

app.Run();

static IResult? EnsureAuthorizedUser(ClaimsPrincipal user, Guid requestedUserId)
{
    if (user.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    var subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
    if (!Guid.TryParse(subject, out var authenticatedUserId))
    {
        return Results.Unauthorized();
    }

    return authenticatedUserId == requestedUserId
        ? null
        : Results.Forbid();
}
