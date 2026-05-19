namespace MusicRec.BuildingBlocks.Contracts.Catalog;

public sealed record UpdatePlaylistRequest(
    Guid UserId,
    string Name,
    string? Description,
    bool IsPublic);
