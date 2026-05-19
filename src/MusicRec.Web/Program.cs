using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using MusicRec.Web.Components;
using MusicRec.Web.Options;
using MusicRec.Web.Services;
using AuthenticationStateProvider = Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<ServiceEndpointsOptions>(
    builder.Configuration.GetSection(ServiceEndpointsOptions.SectionName));
builder.Services.Configure<SpotifyAuthOptions>(
    builder.Configuration.GetSection(SpotifyAuthOptions.SectionName));

var endpoints = builder.Configuration.GetSection(ServiceEndpointsOptions.SectionName).Get<ServiceEndpointsOptions>()
    ?? new ServiceEndpointsOptions();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecretKey = jwtSection["SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey is required.");
var jwtIssuer = jwtSection["Issuer"] ?? "MusicRec.Identity.Api";
var jwtAudience = jwtSection["Audience"] ?? "MusicRec.Client";

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
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

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<ProtectedLocalStorage>();
builder.Services.AddScoped<UserSessionState>();
builder.Services.AddScoped<SpotifySessionState>();
builder.Services.AddScoped<LibraryState>();
builder.Services.AddScoped<PlayerState>();
builder.Services.AddScoped<PreferenceState>();
builder.Services.AddScoped<SpotifyAuthService>();
builder.Services.AddScoped<AuthTokenHandler>();
builder.Services.AddScoped<CookieAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CookieAuthenticationStateProvider>());
builder.Services.AddHttpClient<IdentityApiClient>(client => client.BaseAddress = new Uri(endpoints.IdentityApi));
builder.Services.AddHttpClient<CatalogApiClient>(client => client.BaseAddress = new Uri(endpoints.CatalogApi))
    .AddHttpMessageHandler<AuthTokenHandler>();
builder.Services.AddHttpClient<RecommendationApiClient>(client => client.BaseAddress = new Uri(endpoints.RecommendationApi))
    .AddHttpMessageHandler<AuthTokenHandler>();
builder.Services.AddHttpClient();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseExceptionHandler(exceptionApp =>
    {
        exceptionApp.Run(async context =>
        {
            var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await Results.Problem(
                title: "Unhandled server error",
                detail: exception?.Message,
                statusCode: StatusCodes.Status500InternalServerError)
                .ExecuteAsync(context);
        });
    });
}

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
