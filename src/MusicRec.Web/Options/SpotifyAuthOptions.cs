namespace MusicRec.Web.Options;

public sealed class SpotifyAuthOptions
{
    public const string SectionName = "Spotify";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = "http://127.0.0.1:5175/callback";
    public string ApiBaseUrl { get; set; } = "https://api.spotify.com/v1/";
    public string AccountsBaseUrl { get; set; } = "https://accounts.spotify.com/";
    public bool ShowDialog { get; set; }
}
