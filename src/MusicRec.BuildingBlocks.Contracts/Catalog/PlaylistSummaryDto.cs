namespace MusicRec.BuildingBlocks.Contracts.Catalog;

public sealed record PlaylistSummaryDto(
    Guid Id,
    string Name,
    bool IsSystemPlaylist,
    bool IsPublic,
    int SongCount,
    string? Description);
