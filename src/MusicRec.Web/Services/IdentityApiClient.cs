using System.Net.Http.Json;
using MusicRec.BuildingBlocks.Contracts.Auth;

namespace MusicRec.Web.Services;

public sealed class IdentityApiClient(HttpClient httpClient)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/identity/register", request, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Identity API");
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/identity/login", request, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Identity API");
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken))!;
    }
}
