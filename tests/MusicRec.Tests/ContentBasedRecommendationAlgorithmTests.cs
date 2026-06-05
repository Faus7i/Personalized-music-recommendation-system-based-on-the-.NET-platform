using MusicRec.Services.Recommendation.Api.Data.Entities;
using MusicRec.Services.Recommendation.Api.Services;

namespace MusicRec.Tests;

public sealed class ContentBasedRecommendationAlgorithmTests
{
    [Fact]
    public async Task ScoreAsync_SearchKeywordBoostsMatchingSong()
    {
        var genre = new Genre
        {
            Id = Guid.NewGuid(),
            Name = "Pop"
        };

        var matchingSong = new Song
        {
            Id = Guid.NewGuid(),
            Source = "spotify",
            Title = "Taylor Nights",
            Artist = "Artist A",
            Album = "Midnight Blue",
            Genre = genre,
            GenreId = genre.Id,
            TempoTag = "medium",
            MoodTag = "uplifting",
            EnergyLevel = 0.8,
            PopularityScore = 0.5,
            IsActive = true
        };

        var unrelatedSong = new Song
        {
            Id = Guid.NewGuid(),
            Source = "spotify",
            Title = "Ocean Lights",
            Artist = "Artist B",
            Album = "Sea Echo",
            Genre = genre,
            GenreId = genre.Id,
            TempoTag = "medium",
            MoodTag = "uplifting",
            EnergyLevel = 0.8,
            PopularityScore = 0.5,
            IsActive = true
        };

        var context = new RecommendationContext(
            Guid.NewGuid(),
            [],
            [],
            [
                new UserSearchHistory
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    Keyword = "Taylor",
                    NormalizedKeyword = "TAYLOR",
                    SearchType = "track",
                    Source = "spotify",
                    ResultCount = 10,
                    SearchedAtUtc = DateTimeOffset.UtcNow
                }
            ],
            []);

        var algorithm = new ContentBasedRecommendationAlgorithm();
        var result = await algorithm.ScoreAsync([matchingSong, unrelatedSong], context);

        Assert.True(result[matchingSong.Id] > result[unrelatedSong.Id]);
    }
}
