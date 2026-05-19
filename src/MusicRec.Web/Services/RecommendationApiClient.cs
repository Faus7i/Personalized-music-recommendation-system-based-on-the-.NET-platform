using System.Net.Http.Json;
using System.Net.Http.Headers;
using MusicRec.BuildingBlocks.Contracts.Recommendations;

namespace MusicRec.Web.Services;

public sealed class RecommendationApiClient(HttpClient httpClient, UserSessionState sessionState)
{
    public async Task<RecommendationResultDto?> GetRecommendationsAsync(
        Guid userId,
        IEnumerable<Guid>? excludeSongIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = excludeSongIds?
            .Distinct()
            .Select(x => $"excludeSongIds={Uri.EscapeDataString(x.ToString())}")
            .ToArray() ?? [];

        var url = query.Length == 0
            ? $"/api/recommendations/{userId}"
            : $"/api/recommendations/{userId}?{string.Join("&", query)}";

        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, url);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Recommendation API");
        return await response.Content.ReadFromJsonAsync<RecommendationResultDto>(cancellationToken: cancellationToken);
    }

    public async Task<RecommendationEvaluationDto?> EvaluateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, $"/api/recommendations/{userId}/evaluate");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Recommendation API");
        return await response.Content.ReadFromJsonAsync<RecommendationEvaluationDto>(cancellationToken: cancellationToken);
    }

    private async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(HttpMethod method, string url)
    {
        if (!sessionState.IsReady)
        {
            try
            {
                await sessionState.InitializeAsync();
            }
            catch
            {
            }
        }

        var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrWhiteSpace(sessionState.CurrentUser?.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionState.CurrentUser.Token);
        }

        return request;
    }
}
