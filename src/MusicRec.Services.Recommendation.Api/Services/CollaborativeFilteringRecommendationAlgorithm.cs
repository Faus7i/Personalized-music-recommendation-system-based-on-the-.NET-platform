using Microsoft.EntityFrameworkCore;
using MusicRec.Services.Recommendation.Api.Data;
using MusicRec.Services.Recommendation.Api.Data.Entities;

namespace MusicRec.Services.Recommendation.Api.Services;

public sealed class CollaborativeFilteringRecommendationAlgorithm(
    RecommendationDbContext dbContext) : IRecommendationAlgorithm
{
    public string Name => "CollaborativeFiltering";

    public async Task<IReadOnlyDictionary<Guid, double>> ScoreAsync(
        IReadOnlyList<Song> candidateSongs,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        var positiveSeedSongIds = context.Preferences
            .Where(x => x.FeedbackType == "like")
            .Select(x => x.SongId)
            .Concat(context.BehaviorEvents
                .Where(x => x.EventType == "complete" || (x.CompletionRate ?? 0) >= 0.8)
                .Select(x => x.SongId))
            .Distinct()
            .ToArray();

        if (positiveSeedSongIds.Length == 0)
        {
            return new Dictionary<Guid, double>();
        }

        var neighborUserIds = await dbContext.UserSongPreferences
            .AsNoTracking()
            .Where(x => x.UserId != context.UserId && positiveSeedSongIds.Contains(x.SongId) && x.FeedbackType == "like")
            .Select(x => x.UserId)
            .Distinct()
            .Take(200)
            .ToListAsync(cancellationToken);

        if (neighborUserIds.Count == 0)
        {
            return new Dictionary<Guid, double>();
        }

        var neighborPreferences = await dbContext.UserSongPreferences
            .AsNoTracking()
            .Include(x => x.Song)
            .ThenInclude(x => x.Genre)
            .Where(x => neighborUserIds.Contains(x.UserId) && x.FeedbackType == "like")
            .ToListAsync(cancellationToken);

        var currentUserSeedSet = positiveSeedSongIds.ToHashSet();
        var neighborSimilarity = neighborPreferences
            .GroupBy(x => x.UserId)
            .ToDictionary(
                x => x.Key,
                x =>
                {
                    var neighborSongSet = x.Select(p => p.SongId).ToHashSet();
                    var overlap = neighborSongSet.Count(currentUserSeedSet.Contains);
                    return overlap / Math.Sqrt(Math.Max(neighborSongSet.Count, 1) * Math.Max(currentUserSeedSet.Count, 1));
                });

        var candidateIds = candidateSongs.Select(x => x.Id).ToHashSet();
        var scores = neighborPreferences
            .Where(x => candidateIds.Contains(x.SongId) && !context.ExcludedSongIds.Contains(x.SongId))
            .GroupBy(x => x.SongId)
            .ToDictionary(
                x => x.Key,
                x => Math.Round(x.Sum(p => neighborSimilarity.GetValueOrDefault(p.UserId) * 1.5) + x.Count() * 0.05, 4));

        return scores;
    }
}
