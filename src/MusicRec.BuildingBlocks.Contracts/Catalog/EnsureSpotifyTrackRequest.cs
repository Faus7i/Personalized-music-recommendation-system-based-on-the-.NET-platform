namespace MusicRec.BuildingBlocks.Contracts.Catalog;

public sealed record EnsureSpotifyTrackRequest(
    string SpotifyTrackId,
    string? GenreName,
    string? SpotifyUri,
    string? Title,
    string? Artist,
    string? Album,
    string? PreviewUrl,
    string? CoverUrl,
    string? ExternalUrl,
    int? DurationSeconds,
    int? Popularity,
    string? ReleaseDate);
