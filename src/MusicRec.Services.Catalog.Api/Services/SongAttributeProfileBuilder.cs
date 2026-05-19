using MusicRec.Services.Catalog.Api.Data.Entities;

namespace MusicRec.Services.Catalog.Api.Services;

public static class SongAttributeProfileBuilder
{
    public static void Apply(Song song, string genreName)
    {
        song.TempoTag = ResolveTempoTag(song, genreName);
        song.MoodTag = ResolveMoodTag(song, genreName);
        song.EnergyLevel = ResolveEnergyLevel(song, genreName);
    }

    private static string ResolveTempoTag(Song song, string genreName)
    {
        var genre = genreName.ToLowerInvariant();

        if (genre.Contains("dance") || genre.Contains("electro"))
        {
            return "fast";
        }

        if (genre.Contains("alternative") || song.DurationSeconds >= 240)
        {
            return "medium";
        }

        if (song.DurationSeconds <= 165)
        {
            return "fast";
        }

        return "medium";
    }

    private static string ResolveMoodTag(Song song, string genreName)
    {
        var text = $"{song.Title} {song.Artist} {genreName}".ToLowerInvariant();

        if (text.Contains("night") || text.Contains("midnight") || text.Contains("tears") || text.Contains("lonely"))
        {
            return "melancholic";
        }

        if (text.Contains("love") || text.Contains("golden") || text.Contains("sun") || text.Contains("flowers"))
        {
            return "romantic";
        }

        if (genreName.Contains("dance", StringComparison.OrdinalIgnoreCase) ||
            genreName.Contains("pop", StringComparison.OrdinalIgnoreCase))
        {
            return "uplifting";
        }

        return "chill";
    }

    private static double ResolveEnergyLevel(Song song, string genreName)
    {
        var score = song.PopularityScore;

        if (genreName.Contains("dance", StringComparison.OrdinalIgnoreCase) ||
            genreName.Contains("electro", StringComparison.OrdinalIgnoreCase))
        {
            score += 0.15;
        }

        if (song.DurationSeconds < 180)
        {
            score += 0.05;
        }

        if (song.Title.Contains("slow", StringComparison.OrdinalIgnoreCase))
        {
            score -= 0.15;
        }

        return Math.Clamp(Math.Round(score, 4), 0.05, 1.0);
    }
}
