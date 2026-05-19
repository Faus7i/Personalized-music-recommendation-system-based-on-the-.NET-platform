using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MusicRec.BuildingBlocks.Contracts.Auth;
using MusicRec.Services.Identity.Api.Data;

namespace MusicRec.Tests;

public sealed class IdentityApiTests : IClassFixture<IdentityApiFactory>
{
    private readonly HttpClient httpClient;
    private readonly IdentityApiFactory factory;

    public IdentityApiTests(IdentityApiFactory factory)
    {
        this.factory = factory;
        httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsValidationProblem()
    {
        var response = await httpClient.PostAsJsonAsync("/api/identity/register", new RegisterRequest(
            "tester",
            "invalid-email",
            "13800138000",
            "Pass1234",
            "Pass1234"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(payload);
        Assert.Equal("One or more validation errors occurred.", json.RootElement.GetProperty("title").GetString());
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty("Email", out _));
    }

    [Fact]
    public async Task Register_ThenLoginByPhone_ReturnsToken()
    {
        var phoneNumber = $"138{Random.Shared.Next(10000000, 99999999)}";
        var registerResponse = await httpClient.PostAsJsonAsync("/api/identity/register", new RegisterRequest(
            $"tester-{Guid.NewGuid():N}",
            $"tester-{Guid.NewGuid():N}@example.com",
            phoneNumber,
            "Pass1234",
            "Pass1234"));

        registerResponse.EnsureSuccessStatusCode();

        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrWhiteSpace(authResponse!.Token));

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var storedUser = await dbContext.UserAccounts.FindAsync(authResponse.UserId);
            Assert.NotNull(storedUser);
            Assert.Equal(phoneNumber, storedUser!.PhoneNumber);
            Assert.Equal(phoneNumber, storedUser.NormalizedPhoneNumber);
        }

        var loginResponse = await httpClient.PostAsJsonAsync("/api/identity/login", new LoginRequest(
            phoneNumber,
            "Pass1234"));

        var loginPayloadText = await loginResponse.Content.ReadAsStringAsync();
        Assert.True(loginResponse.IsSuccessStatusCode, loginPayloadText);

        var loginPayload = JsonSerializer.Deserialize<AuthResponse>(loginPayloadText, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        Assert.NotNull(loginPayload);
        Assert.Equal(authResponse.UserId, loginPayload!.UserId);
        Assert.False(string.IsNullOrWhiteSpace(loginPayload.Token));
    }
}
