using System.Net.Http.Json;
using System.Net.Http.Headers;
using MusicRec.BuildingBlocks.Contracts.Catalog;

namespace MusicRec.Web.Services;

public sealed class CatalogApiClient(HttpClient httpClient, UserSessionState sessionState)
{
    public async Task<IReadOnlyList<SongCardDto>> GetColdStartSongsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<List<SongCardDto>>($"/api/catalog/cold-start?count={count}", cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<SongCardDto>> GetLikedSongsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, $"/api/catalog/liked/{userId}", cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Catalog API");
        return await response.Content.ReadFromJsonAsync<List<SongCardDto>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<SongCardDto>> GetSongsAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, "/api/catalog/songs", cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Catalog API");
        return await response.Content.ReadFromJsonAsync<List<SongCardDto>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<SongCardDto>> SearchSongsAsync(string keyword, CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, $"/api/catalog/songs/search?q={Uri.EscapeDataString(keyword)}", cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Catalog API");
        return await response.Content.ReadFromJsonAsync<List<SongCardDto>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task<SongDetailsDto?> GetSongDetailsAsync(Guid songId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, $"/api/catalog/songs/{songId}", cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Catalog API");
        return await response.Content.ReadFromJsonAsync<SongDetailsDto>(cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<PlaylistSummaryDto>> GetPlaylistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, $"/api/catalog/playlists/{userId}", cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Catalog API");
        return await response.Content.ReadFromJsonAsync<List<PlaylistSummaryDto>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task<PlaylistDetailsDto?> GetPlaylistDetailsAsync(Guid userId, Guid playlistId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, $"/api/catalog/playlists/{playlistId}/details?userId={userId}", cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Catalog API");
        return await response.Content.ReadFromJsonAsync<PlaylistDetailsDto>(cancellationToken: cancellationToken);
    }

    public async Task<PlaylistSummaryDto> CreatePlaylistAsync(CreatePlaylistRequest request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = await CreateAuthorizedRequestAsync(HttpMethod.Post, "/api/catalog/playlists", cancellationToken);
        httpRequest.Content = JsonContent.Create(request);
        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Catalog API");
        return (await response.Content.ReadFromJsonAsync<PlaylistSummaryDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<PlaylistSummaryDto> UpdatePlaylistAsync(Guid playlistId, UpdatePlaylistRequest request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = await CreateAuthorizedRequestAsync(HttpMethod.Put, $"/api/catalog/playlists/{playlistId}", cancellationToken);
        httpRequest.Content = JsonContent.Create(request);
        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Catalog API");
        return (await response.Content.ReadFromJsonAsync<PlaylistSummaryDto>(cancellationToken: cancellationToken))!;
    }

    public async Task DeletePlaylistAsync(Guid userId, Guid playlistId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Delete, $"/api/catalog/playlists/{playlistId}?userId={userId}", cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Catalog API");
    }

    public async Task AddSongToPlaylistAsync(AddSongToPlaylistRequest request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = await CreateAuthorizedRequestAsync(HttpMethod.Post, "/api/catalog/playlists/song", cancellationToken);
        httpRequest.Content = JsonContent.Create(request);
        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Catalog API");
    }

    public async Task RemoveSongFromPlaylistAsync(RemoveSongFromPlaylistRequest request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = await CreateAuthorizedRequestAsync(HttpMethod.Post, "/api/catalog/playlists/song/remove", cancellationToken);
        httpRequest.Content = JsonContent.Create(request);
        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Catalog API");
    }

    public async Task<SongPreferenceResultDto> SubmitPreferenceAsync(SubmitSongPreferenceRequest request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = await CreateAuthorizedRequestAsync(HttpMethod.Post, "/api/catalog/preferences", cancellationToken);
        httpRequest.Content = JsonContent.Create(request);
        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Catalog API");
        return (await response.Content.ReadFromJsonAsync<SongPreferenceResultDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<IReadOnlyList<SongPreferenceResultDto>> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, $"/api/catalog/preferences/{userId}", cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Catalog API");
        return await response.Content.ReadFromJsonAsync<List<SongPreferenceResultDto>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task TrackBehaviorAsync(TrackBehaviorEventRequest request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = await CreateAuthorizedRequestAsync(HttpMethod.Post, "/api/catalog/behavior/events", cancellationToken);
        httpRequest.Content = JsonContent.Create(request);
        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Catalog API");
    }

    public async Task<BehaviorSummaryDto?> GetBehaviorSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, $"/api/catalog/behavior/summary/{userId}", cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Catalog API");
        return await response.Content.ReadFromJsonAsync<BehaviorSummaryDto>(cancellationToken: cancellationToken);
    }

    public async Task TrackSearchHistoryAsync(TrackSearchHistoryRequest request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = await CreateAuthorizedRequestAsync(HttpMethod.Post, "/api/catalog/search/history", cancellationToken);
        httpRequest.Content = JsonContent.Create(request);
        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Catalog API");
    }

    public async Task<IReadOnlyList<SearchHistoryItemDto>> GetSearchHistoryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, $"/api/catalog/search/history/{userId}", cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Catalog API");
        return await response.Content.ReadFromJsonAsync<List<SearchHistoryItemDto>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task<SpotifyCatalogStatusDto?> GetSpotifyStatusAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<SpotifyCatalogStatusDto>("/api/catalog/admin/spotify/status", cancellationToken);
    }

    public async Task<SpotifyCatalogSearchResponseDto> SearchSpotifyCatalogAsync(
        string keyword,
        string? searchType = null,
        CancellationToken cancellationToken = default)
    {
        var typeQuery = string.IsNullOrWhiteSpace(searchType)
            ? string.Empty
            : $"&type={Uri.EscapeDataString(searchType)}";

        return await httpClient.GetFromJsonAsync<SpotifyCatalogSearchResponseDto>(
            $"/api/catalog/admin/spotify/search?q={Uri.EscapeDataString(keyword)}{typeQuery}",
            cancellationToken) ?? new SpotifyCatalogSearchResponseDto([], [], []);
    }

    public async Task<SongCardDto> EnsureSpotifyTrackAsync(
        EnsureSpotifyTrackRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/catalog/admin/spotify/ensure-track", request, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Catalog API");
        return (await response.Content.ReadFromJsonAsync<SongCardDto>(cancellationToken: cancellationToken))!;
    }

    public async Task ImportSpotifyTracksAsync(ImportSpotifyTracksRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/catalog/admin/spotify/import", request, cancellationToken);
        await ApiErrorHelper.EnsureSuccessAsync(response, "Catalog API");
    }

    private async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(HttpMethod method, string url, CancellationToken cancellationToken)
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
