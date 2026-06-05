using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using MusicRec.Web.Options;

namespace MusicRec.Web.Services;

public sealed class SpotifyAuthService(
    IHttpClientFactory httpClientFactory,
    IOptions<SpotifyAuthOptions> options,
    SpotifySessionState spotifySessionState)
{
    private readonly SpotifyAuthOptions spotifyOptions = options.Value;
    private static readonly string[] DefaultScopes =
    [
        "streaming",
        "user-read-email",
        "user-read-private",
        "user-read-playback-state",
        "user-modify-playback-state"
    ];

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(spotifyOptions.ClientId) &&
        !string.IsNullOrWhiteSpace(spotifyOptions.ClientSecret);

    public string BuildAuthorizeUrl(string? returnUrl = null)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Spotify auth credentials are not configured.");
        }

        #region debug-point spotify-oauth-black-screen-auth-url
        _ = DebugReportAsync("auth.build-authorize-url", new
        {
            returnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl.Trim(),
            redirectUri = spotifyOptions.RedirectUri,
            accountsBaseUrl = spotifyOptions.AccountsBaseUrl,
            scopeCount = DefaultScopes.Length,
            showDialog = spotifyOptions.ShowDialog
        });
        #endregion

        var state = EncodeState(returnUrl);
        var query = new Dictionary<string, string>
        {
            ["client_id"] = spotifyOptions.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = spotifyOptions.RedirectUri,
            ["scope"] = string.Join(' ', DefaultScopes),
            ["state"] = state
        };

        if (spotifyOptions.ShowDialog)
        {
            query["show_dialog"] = "true";
        }

        return $"{spotifyOptions.AccountsBaseUrl.TrimEnd('/')}/authorize?{string.Join("&", query.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"))}";
    }

    public async Task<SpotifyAuthSession> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Spotify auth credentials are not configured.");
        }

        #region debug-point spotify-oauth-black-screen-exchange-start
        await DebugReportAsync("auth.exchange-code.start", new
        {
            codeLength = string.IsNullOrWhiteSpace(code) ? 0 : code.Length,
            redirectUri = spotifyOptions.RedirectUri,
            accountsBaseUrl = spotifyOptions.AccountsBaseUrl
        });
        #endregion

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(spotifyOptions.AccountsBaseUrl);

            var request = new HttpRequestMessage(HttpMethod.Post, "api/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", BuildBasicCredentials());
            request.Content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", spotifyOptions.RedirectUri)
            ]);

            using var response = await client.SendAsync(request, cancellationToken);

            #region debug-point spotify-oauth-black-screen-exchange-response
            await DebugReportAsync("auth.exchange-code.response", new
            {
                statusCode = (int)response.StatusCode,
                reasonPhrase = response.ReasonPhrase
            });
            #endregion

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                #region debug-point spotify-oauth-black-screen-exchange-failed
                await DebugReportAsync("auth.exchange-code.failed", new
                {
                    statusCode = (int)response.StatusCode,
                    reasonPhrase = response.ReasonPhrase,
                    body = body.Length > 1200 ? body[..1200] : body
                });
                #endregion
            }

            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<SpotifyTokenResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Spotify token response is empty.");

            var session = await BuildSessionAsync(token, cancellationToken);
            await spotifySessionState.SetAsync(session);

            #region debug-point spotify-oauth-black-screen-exchange-success
            await DebugReportAsync("auth.exchange-code.success", new
            {
                hasRefreshToken = !string.IsNullOrWhiteSpace(session.RefreshToken),
                expiresAtUtc = session.ExpiresAtUtc,
                displayName = session.DisplayName
            });
            #endregion

            return session;
        }
        catch (Exception ex)
        {
            #region debug-point spotify-oauth-black-screen-exchange-exception
            await DebugReportAsync("auth.exchange-code.exception", new
            {
                exceptionType = ex.GetType().FullName,
                message = ex.Message
            });
            #endregion

            throw;
        }
    }

    public async Task<SpotifyAuthSession?> EnsureValidSessionAsync(CancellationToken cancellationToken = default)
    {
        #region debug-point spotify-oauth-black-screen-ensure-start
        await DebugReportAsync("auth.ensure-valid.start", new
        {
            hasInit = spotifySessionState.IsReady
        });
        #endregion

        await spotifySessionState.InitializeAsync();

        if (spotifySessionState.Current is null)
        {
            #region debug-point spotify-oauth-black-screen-ensure-no-session
            await DebugReportAsync("auth.ensure-valid.no-session", new { });
            #endregion
            return null;
        }

        if (spotifySessionState.Current.ExpiresAtUtc > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            #region debug-point spotify-oauth-black-screen-ensure-still-valid
            await DebugReportAsync("auth.ensure-valid.still-valid", new
            {
                expiresAtUtc = spotifySessionState.Current.ExpiresAtUtc,
                displayName = spotifySessionState.Current.DisplayName
            });
            #endregion
            return spotifySessionState.Current;
        }

        if (string.IsNullOrWhiteSpace(spotifySessionState.Current.RefreshToken))
        {
            #region debug-point spotify-oauth-black-screen-ensure-no-refresh
            await DebugReportAsync("auth.ensure-valid.no-refresh-token", new
            {
                expiresAtUtc = spotifySessionState.Current.ExpiresAtUtc
            });
            #endregion
            await spotifySessionState.ClearAsync();
            return null;
        }

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(spotifyOptions.AccountsBaseUrl);

            var request = new HttpRequestMessage(HttpMethod.Post, "api/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", BuildBasicCredentials());
            request.Content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", spotifySessionState.Current.RefreshToken)
            ]);

            using var response = await client.SendAsync(request, cancellationToken);

            #region debug-point spotify-oauth-black-screen-refresh-response
            await DebugReportAsync("auth.refresh.response", new
            {
                statusCode = (int)response.StatusCode,
                reasonPhrase = response.ReasonPhrase
            });
            #endregion

            if (!response.IsSuccessStatusCode)
            {
                await spotifySessionState.ClearAsync();
            }

            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<SpotifyTokenResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Spotify refresh response is empty.");

            token.RefreshToken ??= spotifySessionState.Current.RefreshToken;

            var session = await BuildSessionAsync(token, cancellationToken);
            await spotifySessionState.SetAsync(session);
            return session;
        }
        catch
        {
            await spotifySessionState.ClearAsync();
            throw;
        }
    }

    public async Task DisconnectAsync() => await spotifySessionState.ClearAsync();

    private async Task<SpotifyAuthSession> BuildSessionAsync(SpotifyTokenResponse token, CancellationToken cancellationToken)
    {
        var profile = await FetchProfileAsync(token.AccessToken, cancellationToken);

        return new SpotifyAuthSession
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken ?? string.Empty,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn),
            DisplayName = profile.DisplayName,
            SpotifyUserId = profile.Id,
            Scopes = string.IsNullOrWhiteSpace(token.Scope)
                ? DefaultScopes
                : token.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        };
    }

    private async Task<SpotifyMeResponse> FetchProfileAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(spotifyOptions.ApiBaseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.GetAsync("me", cancellationToken);

        #region debug-point spotify-oauth-black-screen-me-response
        await DebugReportAsync("auth.me.response", new
        {
            statusCode = (int)response.StatusCode,
            reasonPhrase = response.ReasonPhrase
        });
        #endregion

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SpotifyMeResponse>(cancellationToken: cancellationToken)
            ?? new SpotifyMeResponse();
    }

    private string BuildBasicCredentials() =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{spotifyOptions.ClientId}:{spotifyOptions.ClientSecret}"));

    public string ResolveReturnUrl(string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            return "/";
        }

        try
        {
            var payload = Encoding.UTF8.GetString(Convert.FromBase64String(state));
            var parts = payload.Split('|', 2, StringSplitOptions.TrimEntries);
            var returnUrl = parts.FirstOrDefault();
            return string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl!;
        }
        catch
        {
            return "/";
        }
    }

    private static string EncodeState(string? returnUrl)
    {
        var normalizedReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl.Trim();
        var payload = $"{normalizedReturnUrl}|{Convert.ToHexString(RandomNumberGenerator.GetBytes(16))}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
    }

    #region debug-point spotify-oauth-black-screen-reporter
    private async Task DebugReportAsync(string point, object payload)
    {
        try
        {
            var url = ResolveDebugServerUrl();
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            using var client = new HttpClient();
            await client.PostAsJsonAsync(url, new
            {
                ts = DateTimeOffset.UtcNow,
                sessionId = "spotify-oauth-black-screen",
                component = "MusicRec.Web.SpotifyAuthService",
                point,
                payload
            });
        }
        catch
        {
        }
    }

    private static string? ResolveDebugServerUrl()
    {
        try
        {
            var dir = Directory.GetCurrentDirectory();
            for (var i = 0; i < 6 && !string.IsNullOrWhiteSpace(dir); i++)
            {
                var envPath = Path.Combine(dir, ".dbg", "spotify-oauth-black-screen.env");
                if (File.Exists(envPath))
                {
                    var line = File.ReadLines(envPath).FirstOrDefault(x => x.StartsWith("DEBUG_SERVER_URL=", StringComparison.Ordinal));
                    if (line is null)
                    {
                        return null;
                    }

                    return line["DEBUG_SERVER_URL=".Length..].Trim();
                }

                dir = Directory.GetParent(dir)?.FullName;
            }
        }
        catch
        {
        }

        return null;
    }
    #endregion

    private sealed class SpotifyTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }

    private sealed class SpotifyMeResponse
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}
