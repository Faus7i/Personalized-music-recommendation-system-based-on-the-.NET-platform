namespace MusicRec.Web.Services;

public static class CoverImageResolver
{
    private const string ImageApiBaseUrl = "https://coresg-normal.trae.ai/api/ide/v1/text_to_image";

    public static string BuildSongCover(string title, string artist)
    {
        var safeTitle = Clean(title, "Untitled track");
        var safeArtist = Clean(artist, "independent artist");
        var prompt = Uri.EscapeDataString(
            $"{safeTitle} single cover art, inspired by {safeArtist}, premium square album artwork, polished cinematic lighting, realistic commercial music art");

        return $"{ImageApiBaseUrl}?prompt={prompt}&image_size=square_hd";
    }

    public static string BuildArtistImage(string artistName)
    {
        var safeArtist = Clean(artistName, "music artist");
        var prompt = Uri.EscapeDataString(
            $"{safeArtist} artist portrait, premium editorial music photography, studio lighting, realistic commercial portrait, square composition");

        return $"{ImageApiBaseUrl}?prompt={prompt}&image_size=square_hd";
    }

    public static bool IsUsable(string? imageUrl) =>
        !string.IsNullOrWhiteSpace(imageUrl) &&
        !imageUrl.Contains("placehold.co", StringComparison.OrdinalIgnoreCase);

    public static string ResolveSongCover(string? imageUrl, string title, string artist) =>
        IsUsable(imageUrl) ? imageUrl! : BuildSongCover(title, artist);

    public static string ResolveArtistImage(string? imageUrl, string artistName) =>
        IsUsable(imageUrl) ? imageUrl! : BuildArtistImage(artistName);

    private static string Clean(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
