namespace MusicRec.BuildingBlocks.Contracts.Catalog;

public sealed record SpotifyAlbumSearchItemDto(
    string SpotifyAlbumId,
    string Name,
    string Artist,
    string CoverUrl,
    string? ReleaseDate,
    int TotalTracks,
    string ExternalUrl);
