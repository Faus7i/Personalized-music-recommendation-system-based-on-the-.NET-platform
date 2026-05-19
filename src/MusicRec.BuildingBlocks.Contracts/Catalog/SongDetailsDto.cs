namespace MusicRec.BuildingBlocks.Contracts.Catalog;

public sealed record SongDetailsDto(
    Guid Id,
    string Title,
    string Artist,
    string Album,
    string Genre,
    string TempoTag,
    string MoodTag,
    double EnergyLevel,
    string? SpotifyUri,
    string? ExternalUrl,
    string CoverUrl,
    string AudioUrl,
    DateOnly? ReleaseDate,
    int DurationSeconds,
    double PopularityScore,
    string Description);
