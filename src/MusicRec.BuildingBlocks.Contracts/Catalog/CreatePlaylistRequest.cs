namespace MusicRec.BuildingBlocks.Contracts.Catalog;

public sealed record CreatePlaylistRequest(
    Guid UserId,
    string Name,
    string? Description);
