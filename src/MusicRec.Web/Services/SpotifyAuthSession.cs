namespace MusicRec.Web.Services;

public sealed class SpotifyAuthSession
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public string? DisplayName { get; set; }
    public string? SpotifyUserId { get; set; }
    public string[] Scopes { get; set; } = [];
}
