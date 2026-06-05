namespace MusicRec.BuildingBlocks.Contracts.Catalog;

public sealed record TrackSearchHistoryRequest(
    Guid UserId,
    string Keyword,
    string SearchType,
    string Source,
    int ResultCount,
    DateTimeOffset SearchedAtUtc);
