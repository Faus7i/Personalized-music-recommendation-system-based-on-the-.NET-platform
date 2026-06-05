using Microsoft.EntityFrameworkCore;
using MusicRec.Services.Recommendation.Api.Data.Entities;

namespace MusicRec.Services.Recommendation.Api.Data;

public sealed class RecommendationDbContext(DbContextOptions<RecommendationDbContext> options) : DbContext(options)
{
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Song> Songs => Set<Song>();
    public DbSet<UserSongPreference> UserSongPreferences => Set<UserSongPreference>();
    public DbSet<UserBehaviorEvent> UserBehaviorEvents => Set<UserBehaviorEvent>();
    public DbSet<UserSearchHistory> UserSearchHistories => Set<UserSearchHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Genre>().ToTable("Genres");
        modelBuilder.Entity<Genre>().HasKey(x => x.Id);

        modelBuilder.Entity<Song>().ToTable("Songs");
        modelBuilder.Entity<Song>().HasKey(x => x.Id);
        modelBuilder.Entity<Song>().HasOne(x => x.Genre).WithMany().HasForeignKey(x => x.GenreId);

        modelBuilder.Entity<UserSongPreference>().ToTable("UserSongPreferences");
        modelBuilder.Entity<UserSongPreference>().HasKey(x => x.Id);
        modelBuilder.Entity<UserSongPreference>().HasOne(x => x.Song).WithMany().HasForeignKey(x => x.SongId);

        modelBuilder.Entity<UserBehaviorEvent>().ToTable("UserBehaviorEvents");
        modelBuilder.Entity<UserBehaviorEvent>().HasKey(x => x.Id);
        modelBuilder.Entity<UserBehaviorEvent>().HasOne(x => x.Song).WithMany().HasForeignKey(x => x.SongId);

        modelBuilder.Entity<UserSearchHistory>().ToTable("UserSearchHistories");
        modelBuilder.Entity<UserSearchHistory>().HasKey(x => x.Id);

        base.OnModelCreating(modelBuilder);
    }
}
