using Microsoft.EntityFrameworkCore;

namespace MusicRec.Services.Recommendation.Api.Data.Entities;

public sealed class Genre
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class Song
{
    public Guid Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? SpotifyTrackId { get; set; }
    public string? SpotifyUri { get; set; }
    public string? ExternalUrl { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public Guid GenreId { get; set; }
    public Genre Genre { get; set; } = null!;
    public string TempoTag { get; set; } = string.Empty;
    public string MoodTag { get; set; } = string.Empty;
    public double EnergyLevel { get; set; }
    public string CoverUrl { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;
    public DateOnly? ReleaseDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public double PopularityScore { get; set; }
    public bool IsColdStartCandidate { get; set; }
    public bool IsActive { get; set; }
}

public sealed class UserSongPreference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SongId { get; set; }
    public Song Song { get; set; } = null!;
    public string FeedbackType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class UserBehaviorEvent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SongId { get; set; }
    public Song Song { get; set; } = null!;
    public string EventType { get; set; } = string.Empty;
    public string ContextType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public double? CompletionRate { get; set; }
    public int? PlaybackPositionSeconds { get; set; }
    public int? PlaybackDurationSeconds { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
}

public sealed class UserSearchHistory
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public string NormalizedKeyword { get; set; } = string.Empty;
    public string SearchType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public int ResultCount { get; set; }
    public DateTimeOffset SearchedAtUtc { get; set; }
}
