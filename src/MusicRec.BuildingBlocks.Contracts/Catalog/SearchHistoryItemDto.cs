namespace MusicRec.BuildingBlocks.Contracts.Catalog;

public sealed record SearchHistoryItemDto(
    Guid Id,
    Guid UserId,
    string Keyword,
    string SearchType,
    string Source,
    int ResultCount,
    DateTimeOffset SearchedAtUtc);
