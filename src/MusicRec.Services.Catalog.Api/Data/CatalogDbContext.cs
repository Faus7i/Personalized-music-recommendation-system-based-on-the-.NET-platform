using Microsoft.EntityFrameworkCore;
using MusicRec.Services.Catalog.Api.Data.Entities;

namespace MusicRec.Services.Catalog.Api.Data;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Song> Songs => Set<Song>();
    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<PlaylistSong> PlaylistSongs => Set<PlaylistSong>();
    public DbSet<UserSongPreference> UserSongPreferences => Set<UserSongPreference>();
    public DbSet<UserBehaviorEvent> UserBehaviorEvents => Set<UserBehaviorEvent>();
    public DbSet<UserSearchHistory> UserSearchHistories => Set<UserSearchHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
