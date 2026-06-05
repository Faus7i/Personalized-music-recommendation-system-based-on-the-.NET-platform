using Microsoft.EntityFrameworkCore;
using MusicRec.BuildingBlocks.Contracts.Recommendations;
using MusicRec.Services.Recommendation.Api.Data;
using MusicRec.Services.Recommendation.Api.Data.Entities;

namespace MusicRec.Services.Recommendation.Api.Services;

public sealed class HybridRecommendationService(
    RecommendationDbContext dbContext,
    IEnumerable<IRecommendationAlgorithm> algorithms)
{
    private readonly IRecommendationAlgorithm[] registeredAlgorithms = algorithms.ToArray();

    public async Task<RecommendationResultDto> GetRecommendationsAsync(
        Guid userId,
        IReadOnlyCollection<Guid>? excludeSongIds = null,
        CancellationToken cancellationToken = default)
    {
        var excludedSongIds = excludeSongIds?.ToHashSet() ?? [];

        var preferences = await dbContext.UserSongPreferences
            .AsNoTracking()
            .Include(x => x.Song)
            .ThenInclude(x => x.Genre)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var behaviorEvents = await dbContext.UserBehaviorEvents
            .AsNoTracking()
            .Include(x => x.Song)
            .ThenInclude(x => x.Genre)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(500)
            .ToListAsync(cancellationToken);

        var searchHistories = await dbContext.UserSearchHistories
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.SearchedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        var interactedSongIds = preferences
            .Select(x => x.SongId)
            .Concat(behaviorEvents.Select(x => x.SongId))
            .Concat(excludedSongIds)
            .ToHashSet();

        var candidateSongs = await dbContext.Songs
            .AsNoTracking()
            .Include(x => x.Genre)
            .Where(x => x.IsActive && !interactedSongIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (candidateSongs.Count == 0)
        {
            return new RecommendationResultDto(userId, "EmptyCatalog", []);
        }

        if (preferences.Count == 0 && behaviorEvents.Count == 0 && searchHistories.Count == 0)
        {
            return BuildPopularityFallback(userId, candidateSongs);
        }

        var context = new RecommendationContext(userId, preferences, behaviorEvents, searchHistories, interactedSongIds);
        var algorithmScores = new Dictionary<string, IReadOnlyDictionary<Guid, double>>();

        foreach (var algorithm in registeredAlgorithms)
        {
            algorithmScores[algorithm.Name] = await algorithm.ScoreAsync(candidateSongs, context, cancellationToken);
        }

        var blendedItems = candidateSongs
            .Select(song =>
            {
                var contentScore = algorithmScores.GetValueOrDefault("ContentBased")?.GetValueOrDefault(song.Id) ?? 0;
                var collaborativeScore = algorithmScores.GetValueOrDefault("CollaborativeFiltering")?.GetValueOrDefault(song.Id) ?? 0;
                var blendedScore = (contentScore * 0.55) + (collaborativeScore * 0.35) + (song.PopularityScore * 0.10);
                var reasonParts = new List<string>();

                if (contentScore > 0)
                {
                    reasonParts.Add($"匹配你的内容偏好（{song.Genre.Name}/{song.MoodTag}/{song.TempoTag}）");
                }

                if (collaborativeScore > 0)
                {
                    reasonParts.Add("与相似用户偏好一致");
                }

                if (song.AudioUrl.Length == 0 && !string.IsNullOrWhiteSpace(song.ExternalUrl))
                {
                    reasonParts.Add("当前仅提供元数据展示，可跳转 Spotify 查看");
                }

                if (reasonParts.Count == 0)
                {
                    reasonParts.Add("结合全站热度与探索策略");
                }

                return new RecommendationItemDto(
                    song.Id,
                    song.Title,
                    song.Artist,
                    song.Album,
                    song.Genre.Name,
                    song.TempoTag,
                    song.MoodTag,
                    song.EnergyLevel,
                    song.SpotifyUri,
                    song.ExternalUrl,
                    song.CoverUrl,
                    song.AudioUrl,
                    song.ReleaseDate,
                    song.DurationSeconds,
                    string.Join("，", reasonParts),
                    Math.Round(blendedScore, 4));
            })
            .OrderByDescending(x => x.Score)
            .Take(4)
            .ToList();

        return new RecommendationResultDto(
            userId,
            "Hybrid(ContentBased+CollaborativeFiltering)",
            blendedItems);
    }

    public async Task<RecommendationEvaluationDto> EvaluateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var likedPreferences = await dbContext.UserSongPreferences
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.FeedbackType == "like")
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (likedPreferences.Count < 3)
        {
            return new RecommendationEvaluationDto(
                userId,
                0,
                0,
                0,
                0,
                0,
                0,
                "样本不足，至少需要 3 条喜欢行为才能执行评估。");
        }

        var holdoutSongIds = likedPreferences
            .Take(Math.Min(2, likedPreferences.Count / 2))
            .Select(x => x.SongId)
            .ToHashSet();

        var recommendations = await GetRecommendationsAsync(userId, holdoutSongIds, cancellationToken);
        var top5 = recommendations.Items.Take(5).Select(x => x.SongId).ToHashSet();
        var top10 = recommendations.Items.Take(10).Select(x => x.SongId).ToHashSet();

        var hitsAt5 = holdoutSongIds.Count(top5.Contains);
        var hitsAt10 = holdoutSongIds.Count(top10.Contains);

        return new RecommendationEvaluationDto(
            userId,
            recommendations.Items.Count,
            holdoutSongIds.Count,
            holdoutSongIds.Count == 0 ? 0 : Math.Round(hitsAt5 / 5d, 4),
            holdoutSongIds.Count == 0 ? 0 : Math.Round(hitsAt5 / (double)holdoutSongIds.Count, 4),
            holdoutSongIds.Count == 0 ? 0 : Math.Round(hitsAt10 / 10d, 4),
            holdoutSongIds.Count == 0 ? 0 : Math.Round(hitsAt10 / (double)holdoutSongIds.Count, 4),
            "使用用户最近喜欢歌曲构造留出集，计算 Precision@K 与 Recall@K。");
    }

    private static RecommendationResultDto BuildPopularityFallback(Guid userId, IReadOnlyList<Song> candidateSongs)
    {
        var items = candidateSongs
            .OrderByDescending(x => x.PopularityScore)
            .Take(4)
            .Select(song => new RecommendationItemDto(
                song.Id,
                song.Title,
                song.Artist,
                song.Album,
                song.Genre.Name,
                song.TempoTag,
                song.MoodTag,
                song.EnergyLevel,
                song.SpotifyUri,
                song.ExternalUrl,
                song.CoverUrl,
                song.AudioUrl,
                song.ReleaseDate,
                song.DurationSeconds,
                "基于新用户冷启动与全站热度推荐",
                song.PopularityScore))
            .ToList();

        return new RecommendationResultDto(userId, "PopularityFallback", items);
    }
}
