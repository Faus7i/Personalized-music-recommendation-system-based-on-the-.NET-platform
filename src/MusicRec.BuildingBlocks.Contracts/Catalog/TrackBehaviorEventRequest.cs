namespace MusicRec.BuildingBlocks.Contracts.Catalog;

public sealed record TrackBehaviorEventRequest(
    Guid UserId,
    Guid SongId,
    string EventType,
    string ContextType,
    string? Source,
    double? CompletionRate,
    int? PlaybackPositionSeconds,
    int? PlaybackDurationSeconds,
    DateTimeOffset OccurredAtUtc);
