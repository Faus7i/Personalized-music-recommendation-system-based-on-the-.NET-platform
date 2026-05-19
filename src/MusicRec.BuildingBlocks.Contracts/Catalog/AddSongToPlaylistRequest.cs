namespace MusicRec.BuildingBlocks.Contracts.Catalog;

public sealed record AddSongToPlaylistRequest(
    Guid UserId,
    Guid PlaylistId,
    Guid SongId);
