namespace MusicRec.BuildingBlocks.Contracts.Catalog;

public sealed record RemoveSongFromPlaylistRequest(
    Guid UserId,
    Guid PlaylistId,
    Guid SongId);
