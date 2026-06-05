using Microsoft.EntityFrameworkCore;
using MusicRec.Services.Recommendation.Api.Data;
using MusicRec.Services.Recommendation.Api.Data.Entities;
using MusicRec.Services.Recommendation.Api.Services;

namespace MusicRec.Tests;

public sealed class HybridRecommendationServiceTests
{
    [Fact]
    public async Task GetRecommendationsAsync_ColdStart_ReturnsPopularityFallback()
    {
        await using var dbContext = CreateInMemoryContext();
        await SeedTestData(dbContext);

        var algorithm = new ContentBasedRecommendationAlgorithm();
        var service = new HybridRecommendationService(dbContext, [algorithm]);

        var result = await service.GetRecommendationsAsync(Guid.NewGuid());

        Assert.Equal("PopularityFallback", result.Strategy);
        Assert.NotEmpty(result.Items);
    }

    [Fact]
    public async Task GetRecommendationsAsync_WithLikes_ReturnsHybridResults()
    {
        await using var dbContext = CreateInMemoryContext();
        await SeedTestData(dbContext);

        var userPreferences = await CreateUserPreferences(dbContext);
        var userId = userPreferences.First().UserId;

        var contentAlgorithm = new ContentBasedRecommendationAlgorithm();
        var collaborativeAlgorithm = new CollaborativeFilteringRecommendationAlgorithm(dbContext);
        var service = new HybridRecommendationService(dbContext, [contentAlgorithm, collaborativeAlgorithm]);

        var result = await service.GetRecommendationsAsync(userId);

        Assert.StartsWith("Hybrid", result.Strategy);
        Assert.NotEmpty(result.Items);
    }

    [Fact]
    public async Task EvaluateAsync_InsufficientData_ReturnsInsufficientSampleMessage()
    {
        await using var dbContext = CreateInMemoryContext();
        await SeedTestData(dbContext);

        var algorithm = new ContentBasedRecommendationAlgorithm();
        var service = new HybridRecommendationService(dbContext, [algorithm]);

        var userId = Guid.NewGuid();
        await dbContext.UserSongPreferences.AddAsync(new UserSongPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SongId = (await dbContext.Songs.FirstAsync()).Id,
            FeedbackType = "like",
            Source = "test",
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var evaluation = await service.EvaluateAsync(userId);

        Assert.Equal("样本不足，至少需要 3 条喜欢行为才能执行评估。", evaluation.Notes);
        Assert.Equal(0, evaluation.CandidateCount);
    }

    private static RecommendationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<RecommendationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new RecommendationDbContext(options);
    }

    private static async Task SeedTestData(RecommendationDbContext dbContext)
    {
        var genres = new[]
        {
            new Genre { Id = Guid.NewGuid(), Name = "Pop" },
            new Genre { Id = Guid.NewGuid(), Name = "Rock" },
            new Genre { Id = Guid.NewGuid(), Name = "Jazz" }
        };
        await dbContext.Genres.AddRangeAsync(genres);
        await dbContext.SaveChangesAsync();

        var popGenre = genres[0];
        var songs = new[]
        {
            new Song
            {
                Id = Guid.NewGuid(),
                Title = "Popular Pop Song",
                Artist = "Pop Artist",
                Album = "Pop Album",
                GenreId = popGenre.Id,
                Genre = popGenre,
                TempoTag = "medium",
                MoodTag = "uplifting",
                EnergyLevel = 0.8,
                PopularityScore = 0.95,
                IsActive = true,
                IsColdStartCandidate = true
            },
            new Song
            {
                Id = Guid.NewGuid(),
                Title = "Another Pop Song",
                Artist = "Pop Artist",
                Album = "Another Album",
                GenreId = popGenre.Id,
                Genre = popGenre,
                TempoTag = "fast",
                MoodTag = "uplifting",
                EnergyLevel = 0.9,
                PopularityScore = 0.85,
                IsActive = true,
                IsColdStartCandidate = true
            },
            new Song
            {
                Id = Guid.NewGuid(),
                Title = "Rock Song",
                Artist = "Rock Band",
                Album = "Rock Album",
                GenreId = genres[1].Id,
                Genre = genres[1],
                TempoTag = "fast",
                MoodTag = "chill",
                EnergyLevel = 0.75,
                PopularityScore = 0.7,
                IsActive = true,
                IsColdStartCandidate = false
            }
        };
        await dbContext.Songs.AddRangeAsync(songs);
        await dbContext.SaveChangesAsync();
    }

    private static async Task<List<UserSongPreference>> CreateUserPreferences(RecommendationDbContext dbContext)
    {
        var songs = await dbContext.Songs.Where(s => s.IsActive).ToListAsync();
        var userId = Guid.NewGuid();

        var preferences = new List<UserSongPreference>();
        foreach (var song in songs.Take(2))
        {
            preferences.Add(new UserSongPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SongId = song.Id,
                FeedbackType = "like",
                Source = "test",
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        await dbContext.UserSongPreferences.AddRangeAsync(preferences);
        await dbContext.SaveChangesAsync();

        return preferences;
    }
}