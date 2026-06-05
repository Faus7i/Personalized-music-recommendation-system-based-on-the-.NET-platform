namespace MusicRec.BuildingBlocks.Contracts.Catalog;

public sealed record BehaviorSummaryDto(
    Guid UserId,
    int TotalEvents,
    int PlayCount,
    int CompletePlayCount,
    int SkipCount,
    int LikeCount,
    int DislikeCount,
    IReadOnlyList<BehaviorTopGenreDto> TopGenres);

public sealed record BehaviorTopGenreDto(
    string Genre,
    int Count);
