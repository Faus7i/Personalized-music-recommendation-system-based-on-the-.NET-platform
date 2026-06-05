namespace MusicRec.BuildingBlocks.Contracts.Recommendations;

public sealed record RecommendationItemDto(
    Guid SongId,
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
    string Reason,
    double Score);
