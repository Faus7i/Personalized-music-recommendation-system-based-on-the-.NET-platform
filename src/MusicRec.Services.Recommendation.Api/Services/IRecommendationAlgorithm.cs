using MusicRec.Services.Recommendation.Api.Data.Entities;

namespace MusicRec.Services.Recommendation.Api.Services;

public interface IRecommendationAlgorithm
{
    string Name { get; }

    Task<IReadOnlyDictionary<Guid, double>> ScoreAsync(
        IReadOnlyList<Song> candidateSongs,
        RecommendationContext context,
        CancellationToken cancellationToken = default);
}
