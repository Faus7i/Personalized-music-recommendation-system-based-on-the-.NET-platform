using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using MusicRec.BuildingBlocks.Contracts.Catalog;
using MusicRec.Services.Catalog.Api.Options;

namespace MusicRec.Services.Catalog.Api.Services;

public sealed class SpotifyCatalogService(
    IHttpClientFactory httpClientFactory,
    IOptions<SpotifyOptions> options)
{
    private readonly SpotifyOptions spotifyOptions = options.Value;
    private SpotifyTokenCache? tokenCache;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(spotifyOptions.ClientId) &&
        !string.IsNullOrWhiteSpace(spotifyOptions.ClientSecret);

    public async Task<IReadOnlyList<SpotifyTrackSearchItemDto>> SearchTracksAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var result = await SearchCatalogAsync(query, "track", cancellationToken);
        return result.Tracks;
    }

    public async Task<SpotifyCatalogSearchResponseDto> SearchCatalogAsync(
        string query,
        string searchType = "track,album,artist",
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Spotify credentials are not configured.");
        }

        var token = await GetAccessTokenAsync(cancellationToken);
        using var client = CreateApiClient(token);
        using var response = await client.GetAsync(
            $"search?q={Uri.EscapeDataString(query)}&type={Uri.EscapeDataString(searchType)}&limit=10",
            cancellationToken);
        await EnsureSuccessAsync(response, "Spotify search");

        var payload = await response.Content.ReadFromJsonAsync<SpotifySearchResponse>(cancellationToken: cancellationToken)
            ?? new SpotifySearchResponse();

        return new SpotifyCatalogSearchResponseDto(
            payload.Tracks.Items.Select(MapTrack).ToList(),
            payload.Albums.Items.Select(MapAlbum).ToList(),
            payload.Artists.Items.Select(MapArtist).ToList());
    }

    public async Task<IReadOnlyList<SpotifyTrackSearchItemDto>> GetTracksAsync(
        IReadOnlyList<string> spotifyTrackIds,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Spotify credentials are not configured.");
        }

        if (spotifyTrackIds.Count == 0)
        {
            return [];
        }

        var token = await GetAccessTokenAsync(cancellationToken);
        using var client = CreateApiClient(token);
        using var response = await client.GetAsync(
            $"tracks?ids={string.Join(",", spotifyTrackIds)}",
            cancellationToken);
        await EnsureSuccessAsync(response, "Spotify tracks");

        var payload = await response.Content.ReadFromJsonAsync<SpotifyTracksResponse>(cancellationToken: cancellationToken)
            ?? new SpotifyTracksResponse();

        return payload.Tracks
            .Where(x => x is not null)
            .Select(x => MapTrack(x!))
            .ToList();
    }

    public async Task<SpotifyTrackSearchItemDto?> GetTrackAsync(
        string spotifyTrackId,
        CancellationToken cancellationToken = default)
    {
        var tracks = await GetTracksAsync([spotifyTrackId], cancellationToken);
        return tracks.FirstOrDefault();
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (tokenCache is not null && tokenCache.ExpiresAtUtc > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return tokenCache.AccessToken;
        }

        using var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(spotifyOptions.AccountsBaseUrl);
        var request = new HttpRequestMessage(HttpMethod.Post, "api/token");
        var credentials = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{spotifyOptions.ClientId}:{spotifyOptions.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        ]);

        using var response = await client.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, "Spotify token");

        var payload = await response.Content.ReadFromJsonAsync<SpotifyTokenResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Spotify token response is empty.");

        tokenCache = new SpotifyTokenCache(
            payload.AccessToken,
            DateTimeOffset.UtcNow.AddSeconds(payload.ExpiresIn));

        return tokenCache.AccessToken;
    }

    private HttpClient CreateApiClient(string token)
    {
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(spotifyOptions.ApiBaseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException($"{operation} failed: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
    }

    private static SpotifyTrackSearchItemDto MapTrack(SpotifyTrackResponse track)
    {
        var coverUrl = track.Album.Images
            .OrderByDescending(x => x.Width ?? 0)
            .Select(x => x.Url)
            .FirstOrDefault()
            ?? string.Empty;

        var externalUrl = track.ExternalUrls.TryGetValue("spotify", out var url) ? url : string.Empty;

        return new SpotifyTrackSearchItemDto(
            track.Id,
            track.Uri,
            track.Name,
            string.Join(", ", track.Artists.Select(x => x.Name)),
            track.Album.Name,
            track.PreviewUrl,
            coverUrl,
            externalUrl,
            Math.Max(track.DurationMs / 1000, 0),
            track.Popularity,
            track.Album.ReleaseDate,
            track.Explicit);
    }

    private static SpotifyAlbumSearchItemDto MapAlbum(SpotifyAlbumResponse album)
    {
        var coverUrl = album.Images
            .OrderByDescending(x => x.Width ?? 0)
            .Select(x => x.Url)
            .FirstOrDefault()
            ?? string.Empty;

        var externalUrl = album.ExternalUrls.TryGetValue("spotify", out var url) ? url : string.Empty;

        return new SpotifyAlbumSearchItemDto(
            album.Id,
            album.Name,
            string.Join(", ", album.Artists.Select(x => x.Name)),
            coverUrl,
            album.ReleaseDate,
            album.TotalTracks,
            externalUrl);
    }

    private static SpotifyArtistSearchItemDto MapArtist(SpotifyArtistResponse artist)
    {
        var coverUrl = artist.Images
            .OrderByDescending(x => x.Width ?? 0)
            .Select(x => x.Url)
            .FirstOrDefault()
            ?? string.Empty;

        var externalUrl = artist.ExternalUrls.TryGetValue("spotify", out var url) ? url : string.Empty;

        return new SpotifyArtistSearchItemDto(
            artist.Id,
            artist.Name,
            artist.Genres.ToArray(),
            coverUrl,
            artist.Popularity,
            externalUrl);
    }

    private sealed record SpotifyTokenCache(string AccessToken, DateTimeOffset ExpiresAtUtc);

    private sealed class SpotifyTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private sealed class SpotifySearchResponse
    {
        [JsonPropertyName("tracks")]
        public SpotifyTrackCollection Tracks { get; set; } = new();

        [JsonPropertyName("albums")]
        public SpotifyAlbumCollection Albums { get; set; } = new();

        [JsonPropertyName("artists")]
        public SpotifyArtistCollection Artists { get; set; } = new();
    }

    private sealed class SpotifyTracksResponse
    {
        [JsonPropertyName("tracks")]
        public List<SpotifyTrackResponse?> Tracks { get; set; } = [];
    }

    private sealed class SpotifyTrackCollection
    {
        [JsonPropertyName("items")]
        public List<SpotifyTrackResponse> Items { get; set; } = [];
    }

    private sealed class SpotifyAlbumCollection
    {
        [JsonPropertyName("items")]
        public List<SpotifyAlbumResponse> Items { get; set; } = [];
    }

    private sealed class SpotifyArtistCollection
    {
        [JsonPropertyName("items")]
        public List<SpotifyArtistResponse> Items { get; set; } = [];
    }

    private sealed class SpotifyTrackResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("preview_url")]
        public string? PreviewUrl { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;

        [JsonPropertyName("explicit")]
        public bool Explicit { get; set; }

        [JsonPropertyName("duration_ms")]
        public int DurationMs { get; set; }

        [JsonPropertyName("popularity")]
        public int Popularity { get; set; }

        [JsonPropertyName("album")]
        public SpotifyAlbumResponse Album { get; set; } = new();

        [JsonPropertyName("artists")]
        public List<SpotifyArtistResponse> Artists { get; set; } = [];

        [JsonPropertyName("external_urls")]
        public Dictionary<string, string> ExternalUrls { get; set; } = [];
    }

    private sealed class SpotifyAlbumResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("total_tracks")]
        public int TotalTracks { get; set; }

        [JsonPropertyName("images")]
        public List<SpotifyImageResponse> Images { get; set; } = [];

        [JsonPropertyName("artists")]
        public List<SpotifyArtistResponse> Artists { get; set; } = [];

        [JsonPropertyName("external_urls")]
        public Dictionary<string, string> ExternalUrls { get; set; } = [];
    }

    private sealed class SpotifyArtistResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("genres")]
        public List<string> Genres { get; set; } = [];

        [JsonPropertyName("popularity")]
        public int Popularity { get; set; }

        [JsonPropertyName("images")]
        public List<SpotifyImageResponse> Images { get; set; } = [];

        [JsonPropertyName("external_urls")]
        public Dictionary<string, string> ExternalUrls { get; set; } = [];
    }

    private sealed class SpotifyImageResponse
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("width")]
        public int? Width { get; set; }
    }
}
