namespace MusicRec.BuildingBlocks.Contracts.Catalog;

public sealed record PlaylistDetailsDto(
    Guid Id,
    string Name,
    bool IsSystemPlaylist,
    bool IsPublic,
    string? Description,
    IReadOnlyList<SongCardDto> Songs);
