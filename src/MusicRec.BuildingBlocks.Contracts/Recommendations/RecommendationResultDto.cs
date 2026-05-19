namespace MusicRec.BuildingBlocks.Contracts.Recommendations;

public sealed record RecommendationResultDto(
    Guid UserId,
    string Strategy,
    IReadOnlyList<RecommendationItemDto> Items);
