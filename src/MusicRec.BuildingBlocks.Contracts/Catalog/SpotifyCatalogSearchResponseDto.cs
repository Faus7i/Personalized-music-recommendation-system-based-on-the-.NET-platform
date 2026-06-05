namespace MusicRec.BuildingBlocks.Contracts.Catalog;

public sealed record SpotifyCatalogSearchResponseDto(
    IReadOnlyList<SpotifyTrackSearchItemDto> Tracks,
    IReadOnlyList<SpotifyAlbumSearchItemDto> Albums,
    IReadOnlyList<SpotifyArtistSearchItemDto> Artists);
