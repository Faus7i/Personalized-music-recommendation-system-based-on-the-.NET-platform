namespace MusicRec.BuildingBlocks.Contracts.Catalog;

public sealed record SpotifyArtistSearchItemDto(
    string SpotifyArtistId,
    string Name,
    string[] Genres,
    string CoverUrl,
    int Popularity,
    string ExternalUrl);
