namespace MusicRec.Services.Catalog.Api.Options;

public sealed class SpotifyOptions
{
    public const string SectionName = "Spotify";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://api.spotify.com/v1/";
    public string AccountsBaseUrl { get; set; } = "https://accounts.spotify.com/";
}
