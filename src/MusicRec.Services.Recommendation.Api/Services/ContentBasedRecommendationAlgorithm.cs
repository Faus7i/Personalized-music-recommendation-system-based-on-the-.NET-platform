using MusicRec.Services.Recommendation.Api.Data.Entities;

namespace MusicRec.Services.Recommendation.Api.Services;

public sealed class ContentBasedRecommendationAlgorithm : IRecommendationAlgorithm
{
    public string Name => "ContentBased";

    public Task<IReadOnlyDictionary<Guid, double>> ScoreAsync(
        IReadOnlyList<Song> candidateSongs,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        var likedPreferences = context.Preferences
            .Where(x => x.FeedbackType == "like")
            .ToList();
        var dislikedPreferences = context.Preferences
            .Where(x => x.FeedbackType == "dislike")
            .ToList();

        var likedGenreCounts = likedPreferences
            .GroupBy(x => x.Song.Genre.Name)
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
        var likedArtistCounts = likedPreferences
            .GroupBy(x => x.Song.Artist)
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
        var likedTempoCounts = likedPreferences
            .GroupBy(x => x.Song.TempoTag)
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
        var likedMoodCounts = likedPreferences
            .GroupBy(x => x.Song.MoodTag)
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
        var dislikedGenreCounts = dislikedPreferences
            .GroupBy(x => x.Song.Genre.Name)
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);

        var longPlaySongIds = context.BehaviorEvents
            .Where(x => x.EventType == "complete" || (x.CompletionRate ?? 0) >= 0.75)
            .Select(x => x.SongId)
            .ToHashSet();

        var recentYear = DateTime.UtcNow.Year - 5;
        var recentKeywords = context.SearchHistories
            .OrderByDescending(x => x.SearchedAtUtc)
            .Take(20)
            .SelectMany(x => x.Keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(x => x.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var scores = new Dictionary<Guid, double>();

        foreach (var song in candidateSongs)
        {
            var score = song.PopularityScore * 0.45;

            if (likedGenreCounts.TryGetValue(song.Genre.Name, out var likedGenreCount))
            {
                score += likedGenreCount * 0.9;
            }

            if (likedArtistCounts.TryGetValue(song.Artist, out var likedArtistCount))
            {
                score += likedArtistCount * 1.1;
            }

            if (likedTempoCounts.TryGetValue(song.TempoTag, out var likedTempoCount))
            {
                score += likedTempoCount * 0.45;
            }

            if (likedMoodCounts.TryGetValue(song.MoodTag, out var likedMoodCount))
            {
                score += likedMoodCount * 0.55;
            }

            if (dislikedGenreCounts.TryGetValue(song.Genre.Name, out var dislikedGenreCount))
            {
                score -= dislikedGenreCount * 0.8;
            }

            if (song.ReleaseDate is not null && song.ReleaseDate.Value.Year >= recentYear)
            {
                score += 0.15;
            }

            if (recentKeywords.Any(keyword =>
                    song.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    song.Artist.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    song.Album.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    song.Genre.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                score += 0.65;
            }

            if (longPlaySongIds.Contains(song.Id))
            {
                score += 0.3;
            }

            score += song.EnergyLevel * 0.25;

            if (song.Source.Equals("spotify", StringComparison.OrdinalIgnoreCase))
            {
                score += 0.1;
            }

            scores[song.Id] = Math.Round(score, 4);
        }

        return Task.FromResult<IReadOnlyDictionary<Guid, double>>(scores);
    }
}
