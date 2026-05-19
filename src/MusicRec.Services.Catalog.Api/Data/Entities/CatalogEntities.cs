using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MusicRec.Services.Catalog.Api.Data.Entities;

public sealed class Genre
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Song> Songs { get; set; } = new List<Song>();
}

public sealed class Song
{
    public Guid Id { get; set; }
    public string Source { get; set; } = "seed";
    public string? SpotifyTrackId { get; set; }
    public string? SpotifyUri { get; set; }
    public string? ExternalUrl { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public Guid GenreId { get; set; }
    public Genre Genre { get; set; } = null!;
    public string TempoTag { get; set; } = "medium";
    public string MoodTag { get; set; } = "chill";
    public double EnergyLevel { get; set; } = 0.5;
    public string CoverUrl { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;
    public DateOnly? ReleaseDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public double PopularityScore { get; set; }
    public bool IsColdStartCandidate { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Playlist
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystemPlaylist { get; set; }
    public bool IsPublic { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public ICollection<PlaylistSong> Songs { get; set; } = new List<PlaylistSong>();
}

public sealed class PlaylistSong
{
    public Guid PlaylistId { get; set; }
    public Playlist Playlist { get; set; } = null!;
    public Guid SongId { get; set; }
    public Song Song { get; set; } = null!;
    public DateTimeOffset AddedAtUtc { get; set; }
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

public sealed class GenreConfiguration : IEntityTypeConfiguration<Genre>
{
    public void Configure(EntityTypeBuilder<Genre> builder)
    {
        builder.ToTable("Genres");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class SongConfiguration : IEntityTypeConfiguration<Song>
{
    public void Configure(EntityTypeBuilder<Song> builder)
    {
        builder.ToTable("Songs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Source).HasMaxLength(32).IsRequired();
        builder.Property(x => x.SpotifyTrackId).HasMaxLength(64);
        builder.Property(x => x.SpotifyUri).HasMaxLength(256);
        builder.Property(x => x.ExternalUrl).HasMaxLength(1024);
        builder.Property(x => x.Title).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Artist).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Album).HasMaxLength(128).IsRequired();
        builder.Property(x => x.TempoTag).HasMaxLength(32).IsRequired();
        builder.Property(x => x.MoodTag).HasMaxLength(32).IsRequired();
        builder.Property(x => x.EnergyLevel).IsRequired();
        builder.Property(x => x.CoverUrl).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.AudioUrl).HasMaxLength(512).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.DurationSeconds).IsRequired();
        builder.Property(x => x.PopularityScore).IsRequired();
        builder.Property(x => x.IsColdStartCandidate).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasOne(x => x.Genre)
            .WithMany(x => x.Songs)
            .HasForeignKey(x => x.GenreId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.SpotifyTrackId)
            .IsUnique()
            .HasFilter("[SpotifyTrackId] IS NOT NULL");
    }
}

public sealed class PlaylistConfiguration : IEntityTypeConfiguration<Playlist>
{
    public void Configure(EntityTypeBuilder<Playlist> builder)
    {
        builder.ToTable("Playlists");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsSystemPlaylist).IsRequired();
        builder.Property(x => x.IsPublic).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.Name }).IsUnique();
    }
}

public sealed class PlaylistSongConfiguration : IEntityTypeConfiguration<PlaylistSong>
{
    public void Configure(EntityTypeBuilder<PlaylistSong> builder)
    {
        builder.ToTable("PlaylistSongs");
        builder.HasKey(x => new { x.PlaylistId, x.SongId });
        builder.Property(x => x.AddedAtUtc).IsRequired();
        builder.HasOne(x => x.Playlist)
            .WithMany(x => x.Songs)
            .HasForeignKey(x => x.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Song)
            .WithMany()
            .HasForeignKey(x => x.SongId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class UserSongPreferenceConfiguration : IEntityTypeConfiguration<UserSongPreference>
{
    public void Configure(EntityTypeBuilder<UserSongPreference> builder)
    {
        builder.ToTable("UserSongPreferences");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FeedbackType).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.HasOne(x => x.Song)
            .WithMany()
            .HasForeignKey(x => x.SongId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.UserId, x.SongId }).IsUnique();
    }
}

public sealed class UserBehaviorEventConfiguration : IEntityTypeConfiguration<UserBehaviorEvent>
{
    public void Configure(EntityTypeBuilder<UserBehaviorEvent> builder)
    {
        builder.ToTable("UserBehaviorEvents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ContextType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(64).IsRequired();
        builder.Property(x => x.OccurredAtUtc).IsRequired();
        builder.HasOne(x => x.Song)
            .WithMany()
            .HasForeignKey(x => x.SongId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.UserId, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.SongId, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.EventType, x.OccurredAtUtc });
    }
}

public sealed class UserSearchHistoryConfiguration : IEntityTypeConfiguration<UserSearchHistory>
{
    public void Configure(EntityTypeBuilder<UserSearchHistory> builder)
    {
        builder.ToTable("UserSearchHistories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Keyword).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NormalizedKeyword).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SearchType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ResultCount).IsRequired();
        builder.Property(x => x.SearchedAtUtc).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.SearchedAtUtc });
        builder.HasIndex(x => new { x.UserId, x.NormalizedKeyword });
    }
}
