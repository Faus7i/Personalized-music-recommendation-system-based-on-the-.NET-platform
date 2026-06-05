using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MusicRec.BuildingBlocks.Contracts.Catalog;
using MusicRec.BuildingBlocks.Shared.ServiceDefaults;
using MusicRec.Services.Catalog.Api.Data;
using MusicRec.Services.Catalog.Api.Data.Entities;
using MusicRec.Services.Catalog.Api.Options;
using MusicRec.Services.Catalog.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CatalogDatabase")));
builder.Services.Configure<SpotifyOptions>(
    builder.Configuration.GetSection(SpotifyOptions.SectionName));
builder.Services.AddHttpClient();
builder.Services.AddScoped<SpotifyCatalogService>();
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtIssuer = jwtSection["Issuer"] ?? "MusicRec.Identity.Api";
var jwtAudience = jwtSection["Audience"] ?? "MusicRec.Client";
var jwtSecretKey = jwtSection["SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey is required.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();

var app = builder.Build();
var startedAtUtc = DateTimeOffset.UtcNow;

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await Results.Problem(
            title: "Unhandled catalog error",
            detail: exception?.Message,
            statusCode: StatusCodes.Status500InternalServerError)
            .ExecuteAsync(context);
    });
});
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new ServiceMetadata("catalog-api", "v0.5.0", startedAtUtc)));

var catalogGroup = app.MapGroup("/api/catalog").WithTags("Catalog");

catalogGroup.MapGet("/cold-start", async (int? count, CatalogDbContext dbContext, SpotifyCatalogService spotifyService) =>
{
    // 设置数量范围：最小9首，最大20首
    var requestedCount = Math.Clamp(count ?? 10, 9, 20);

    // 随机关键词列表，用于从 Spotify 获取多样化的歌曲
    var randomKeywords = new[]
    {
        "pop", "rock", "jazz", "classical", "hip-hop",
        "electronic", "indie", "r&b", "latin", "k-pop",
        "country", "metal", "reggae", "blues", "folk"
    };

    var random = new Random();
    var selectedKeywords = randomKeywords.OrderBy(_ => random.Next()).Take(5).ToArray();

    var allTracks = new List<(SpotifyTrackSearchItemDto Track, string Genre)>();

    foreach (var keyword in selectedKeywords)
    {
        try
        {
            var tracks = await spotifyService.SearchTracksAsync(keyword);
            allTracks.AddRange(tracks.Select(t => (t, CapitalizeFirstLetter(keyword))));
            await Task.Delay(1000);
        }
        catch
        {
            // 忽略单个关键词搜索失败，继续其他关键词
        }
    }

    // 随机打乱并选择指定数量的歌曲
    var shuffled = allTracks.OrderBy(_ => random.Next()).Take(requestedCount).ToList();

    var result = shuffled.Select(item => new SongCardDto(
        Guid.Empty,
        item.Track.Title,
        item.Track.Artist,
        item.Track.Album,
        item.Genre,
        string.Empty,
        string.Empty,
        0,
        item.Track.SpotifyUri,
        item.Track.ExternalUrl,
        item.Track.CoverUrl,
        item.Track.PreviewUrl,
        DateOnly.TryParse(item.Track.ReleaseDate, out var releaseDate) ? releaseDate : null,
        item.Track.DurationSeconds,
        item.Track.Popularity)).ToList();

    return Results.Ok(result);
});

catalogGroup.MapGet("/songs", async (CatalogDbContext dbContext) =>
{
    var songs = await dbContext.Songs
        .AsNoTracking()
        .Include(x => x.Genre)
        .Where(x => x.IsActive)
        .OrderByDescending(x => x.PopularityScore)
        .ToListAsync();

    return Results.Ok(songs.Select(MapSongCard).ToList());
});

catalogGroup.MapGet("/songs/search", async (string q, CatalogDbContext dbContext) =>
{
    if (string.IsNullOrWhiteSpace(q))
    {
        return Results.Ok(new List<SongCardDto>());
    }

    var keyword = q.Trim();
    var songs = await dbContext.Songs
        .AsNoTracking()
        .Include(x => x.Genre)
        .Where(x => x.IsActive &&
            (x.Title.Contains(keyword) ||
             x.Artist.Contains(keyword) ||
             x.Album.Contains(keyword) ||
             x.Genre.Name.Contains(keyword)))
        .OrderByDescending(x => x.PopularityScore)
        .Take(30)
        .ToListAsync();

    return Results.Ok(songs.Select(MapSongCard).ToList());
});

catalogGroup.MapGet("/songs/{songId:guid}", async (Guid songId, CatalogDbContext dbContext) =>
{
    var song = await dbContext.Songs
        .AsNoTracking()
        .Include(x => x.Genre)
        .FirstOrDefaultAsync(x => x.Id == songId && x.IsActive);

    if (song is null)
    {
        return Results.NotFound("Song was not found.");
    }

    return Results.Ok(MapSongDetails(song));
});

catalogGroup.MapGet("/liked/{userId:guid}", async (Guid userId, HttpContext httpContext, CatalogDbContext dbContext) =>
{
    var authorizationResult = EnsureAuthorizedUser(httpContext.User, userId);
    if (authorizationResult is not null)
    {
        return authorizationResult;
    }

    var likedSongs = await dbContext.UserSongPreferences
        .AsNoTracking()
        .Include(x => x.Song)
        .ThenInclude(x => x!.Genre)
        .Where(x => x.UserId == userId && x.FeedbackType == "like")
        .OrderByDescending(x => x.CreatedAtUtc)
        .ToListAsync();

    return Results.Ok(likedSongs
        .Select(x => x.Song)
        .DistinctBy(x => x!.Id)
        .Select(x => MapSongCard(x!))
        .ToList());
});

catalogGroup.MapPost("/preferences", async (SubmitSongPreferenceRequest request, HttpContext httpContext, CatalogDbContext dbContext) =>
{
    var authorizationResult = EnsureAuthorizedUser(httpContext.User, request.UserId);
    if (authorizationResult is not null)
    {
        return authorizationResult;
    }

    var feedbackType = request.FeedbackType.Trim().ToLowerInvariant();
    if (feedbackType is not "like" and not "dislike")
    {
        return Results.BadRequest("FeedbackType must be 'like' or 'dislike'.");
    }

    var song = await dbContext.Songs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.SongId);
    if (song is null)
    {
        return Results.NotFound("Song was not found.");
    }

    var existingPreferences = await dbContext.UserSongPreferences
        .Where(x => x.UserId == request.UserId && x.SongId == request.SongId)
        .ToListAsync();

    if (existingPreferences.Any())
    {
        foreach (var existingPref in existingPreferences)
        {
            existingPref.FeedbackType = feedbackType;
            existingPref.CreatedAtUtc = DateTimeOffset.UtcNow;
        }
        await dbContext.SaveChangesAsync();

        var latestPref = existingPreferences.OrderByDescending(x => x.CreatedAtUtc).First();
        return Results.Ok(new SongPreferenceResultDto(
            latestPref.UserId,
            latestPref.SongId,
            latestPref.FeedbackType,
            latestPref.Source,
            latestPref.CreatedAtUtc));
    }

    var preference = new UserSongPreference
    {
        Id = Guid.NewGuid(),
        UserId = request.UserId,
        SongId = request.SongId,
        FeedbackType = feedbackType,
        Source = string.IsNullOrWhiteSpace(request.Source) ? "cold-start" : request.Source.Trim().ToLowerInvariant(),
        CreatedAtUtc = DateTimeOffset.UtcNow
    };

    dbContext.UserSongPreferences.Add(preference);
    await dbContext.SaveChangesAsync();

    return Results.Ok(new SongPreferenceResultDto(
        preference.UserId,
        preference.SongId,
        preference.FeedbackType,
        preference.Source,
        preference.CreatedAtUtc));
});

catalogGroup.MapGet("/playlists/{userId:guid}", async (Guid userId, HttpContext httpContext, CatalogDbContext dbContext) =>
{
    var authorizationResult = EnsureAuthorizedUser(httpContext.User, userId);
    if (authorizationResult is not null)
    {
        return authorizationResult;
    }

    await EnsureLikesPlaylistAsync(dbContext, userId);

    var likedCount = await dbContext.UserSongPreferences
        .AsNoTracking()
        .Where(x => x.UserId == userId && x.FeedbackType == "like")
        .Select(x => x.SongId)
        .Distinct()
        .CountAsync();

    var customPlaylists = await dbContext.Playlists
        .AsNoTracking()
        .Include(x => x.Songs)
        .Where(x => x.UserId == userId)
        .OrderByDescending(x => x.IsSystemPlaylist)
        .ThenBy(x => x.Name)
        .ToListAsync();

    var playlists = customPlaylists
        .Select(x => new PlaylistSummaryDto(
            x.Id,
            x.Name,
            x.IsSystemPlaylist,
            x.IsPublic,
            x.IsSystemPlaylist ? likedCount : x.Songs.Count,
            x.Description))
        .ToList();

    return Results.Ok(playlists);
});

catalogGroup.MapGet("/playlists/{playlistId:guid}/details", async (Guid playlistId, Guid userId, HttpContext httpContext, CatalogDbContext dbContext) =>
{
    var authorizationResult = EnsureAuthorizedUser(httpContext.User, userId);
    if (authorizationResult is not null)
    {
        return authorizationResult;
    }

    await EnsureLikesPlaylistAsync(dbContext, userId);

    var playlist = await dbContext.Playlists
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == playlistId && x.UserId == userId);

    if (playlist is null)
    {
        return Results.NotFound("Playlist was not found.");
    }

    if (playlist.IsSystemPlaylist)
    {
        var likedSongs = await dbContext.UserSongPreferences
            .AsNoTracking()
            .Include(x => x.Song)
            .ThenInclude(x => x!.Genre)
            .Where(x => x.UserId == userId && x.FeedbackType == "like")
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        return Results.Ok(new PlaylistDetailsDto(
            playlist.Id,
            playlist.Name,
            true,
            false,
            playlist.Description,
            likedSongs.Select(x => x.Song)
                .DistinctBy(x => x!.Id)
                .Select(x => MapSongCard(x!))
                .ToList()));
    }

    var playlistSongs = await dbContext.PlaylistSongs
        .AsNoTracking()
        .Include(x => x.Song)
        .ThenInclude(x => x.Genre)
        .Where(x => x.PlaylistId == playlistId)
        .OrderByDescending(x => x.AddedAtUtc)
        .ToListAsync();

    return Results.Ok(new PlaylistDetailsDto(
        playlist.Id,
        playlist.Name,
        false,
        playlist.IsPublic,
        playlist.Description,
        playlistSongs.Select(x => MapSongCard(x.Song)).ToList()));
});

catalogGroup.MapPost("/playlists", async (CreatePlaylistRequest request, HttpContext httpContext, CatalogDbContext dbContext) =>
{
    var authorizationResult = EnsureAuthorizedUser(httpContext.User, request.UserId);
    if (authorizationResult is not null)
    {
        return authorizationResult;
    }

    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest("Playlist name is required.");
    }

    await EnsureLikesPlaylistAsync(dbContext, request.UserId);

    var exists = await dbContext.Playlists.AnyAsync(x =>
        x.UserId == request.UserId &&
        x.Name == request.Name.Trim());

    if (exists)
    {
        return Results.Conflict("A playlist with the same name already exists.");
    }

    var playlist = new Playlist
    {
        Id = Guid.NewGuid(),
        UserId = request.UserId,
        Name = request.Name.Trim(),
        Description = request.Description?.Trim() ?? string.Empty,
        IsSystemPlaylist = false,
        IsPublic = false,
        CreatedAtUtc = DateTimeOffset.UtcNow
    };

    dbContext.Playlists.Add(playlist);
    await dbContext.SaveChangesAsync();

    return Results.Ok(new PlaylistSummaryDto(
        playlist.Id,
        playlist.Name,
        playlist.IsSystemPlaylist,
        playlist.IsPublic,
        0,
        playlist.Description));
});

catalogGroup.MapPut("/playlists/{playlistId:guid}", async (
    Guid playlistId,
    UpdatePlaylistRequest request,
    HttpContext httpContext,
    CatalogDbContext dbContext) =>
{
    var authorizationResult = EnsureAuthorizedUser(httpContext.User, request.UserId);
    if (authorizationResult is not null)
    {
        return authorizationResult;
    }

    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest("Playlist name is required.");
    }

    var playlist = await dbContext.Playlists
        .FirstOrDefaultAsync(x => x.Id == playlistId && x.UserId == request.UserId);

    if (playlist is null)
    {
        return Results.NotFound("Playlist was not found.");
    }

    if (playlist.IsSystemPlaylist)
    {
        return Results.BadRequest("The system likes playlist cannot be edited.");
    }

    var normalizedName = request.Name.Trim();
    var exists = await dbContext.Playlists.AnyAsync(x =>
        x.UserId == request.UserId &&
        x.Id != playlistId &&
        x.Name == normalizedName);

    if (exists)
    {
        return Results.Conflict("A playlist with the same name already exists.");
    }

    playlist.Name = normalizedName;
    playlist.Description = request.Description?.Trim() ?? string.Empty;
    playlist.IsPublic = request.IsPublic;

    await dbContext.SaveChangesAsync();

    var songCount = await dbContext.PlaylistSongs.CountAsync(x => x.PlaylistId == playlistId);

    return Results.Ok(new PlaylistSummaryDto(
        playlist.Id,
        playlist.Name,
        playlist.IsSystemPlaylist,
        playlist.IsPublic,
        songCount,
        playlist.Description));
});

catalogGroup.MapDelete("/playlists/{playlistId:guid}", async (
    Guid playlistId,
    Guid userId,
    HttpContext httpContext,
    CatalogDbContext dbContext) =>
{
    var authorizationResult = EnsureAuthorizedUser(httpContext.User, userId);
    if (authorizationResult is not null)
    {
        return authorizationResult;
    }

    var playlist = await dbContext.Playlists
        .FirstOrDefaultAsync(x => x.Id == playlistId && x.UserId == userId);

    if (playlist is null)
    {
        return Results.NotFound("Playlist was not found.");
    }

    if (playlist.IsSystemPlaylist)
    {
        return Results.BadRequest("The system likes playlist cannot be deleted.");
    }

    dbContext.Playlists.Remove(playlist);
    await dbContext.SaveChangesAsync();
    return Results.Ok();
});

catalogGroup.MapPost("/playlists/song", async (AddSongToPlaylistRequest request, HttpContext httpContext, CatalogDbContext dbContext) =>
{
    var authorizationResult = EnsureAuthorizedUser(httpContext.User, request.UserId);
    if (authorizationResult is not null)
    {
        return authorizationResult;
    }

    var playlist = await dbContext.Playlists
        .Include(x => x.Songs)
        .FirstOrDefaultAsync(x => x.Id == request.PlaylistId && x.UserId == request.UserId);

    if (playlist is null)
    {
        return Results.NotFound("Playlist was not found.");
    }

    if (playlist.IsSystemPlaylist)
    {
        return Results.BadRequest("The system likes playlist is managed automatically.");
    }

    var song = await dbContext.Songs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.SongId);
    if (song is null)
    {
        return Results.NotFound("Song was not found.");
    }

    var exists = playlist.Songs.Any(x => x.SongId == request.SongId);
    if (!exists)
    {
        playlist.Songs.Add(new PlaylistSong
        {
            PlaylistId = playlist.Id,
            SongId = request.SongId,
            AddedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }

    return Results.Ok();
});

catalogGroup.MapPost("/playlists/song/remove", async (RemoveSongFromPlaylistRequest request, HttpContext httpContext, CatalogDbContext dbContext) =>
{
    var authorizationResult = EnsureAuthorizedUser(httpContext.User, request.UserId);
    if (authorizationResult is not null)
    {
        return authorizationResult;
    }

    var playlist = await dbContext.Playlists
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == request.PlaylistId && x.UserId == request.UserId);

    if (playlist is null)
    {
        return Results.NotFound("Playlist was not found.");
    }

    if (playlist.IsSystemPlaylist)
    {
        return Results.BadRequest("Songs in the system likes playlist are removed by changing your preference.");
    }

    var playlistSong = await dbContext.PlaylistSongs
        .FirstOrDefaultAsync(x => x.PlaylistId == request.PlaylistId && x.SongId == request.SongId);

    if (playlistSong is null)
    {
        return Results.NotFound("Song is not in the playlist.");
    }

    dbContext.PlaylistSongs.Remove(playlistSong);
    await dbContext.SaveChangesAsync();
    return Results.Ok();
});

catalogGroup.MapGet("/preferences/{userId:guid}", async (Guid userId, HttpContext httpContext, CatalogDbContext dbContext) =>
{
    var authorizationResult = EnsureAuthorizedUser(httpContext.User, userId);
    if (authorizationResult is not null)
    {
        return authorizationResult;
    }

    var preferences = await dbContext.UserSongPreferences
        .AsNoTracking()
        .Where(x => x.UserId == userId)
        .OrderByDescending(x => x.CreatedAtUtc)
        .Select(x => new SongPreferenceResultDto(
            x.UserId,
            x.SongId,
            x.FeedbackType,
            x.Source,
            x.CreatedAtUtc))
        .ToListAsync();

    return Results.Ok(preferences);
});

catalogGroup.MapPost("/behavior/events", async (TrackBehaviorEventRequest request, HttpContext httpContext, CatalogDbContext dbContext) =>
{
    var authorizationResult = EnsureAuthorizedUser(httpContext.User, request.UserId);
    if (authorizationResult is not null)
    {
        return authorizationResult;
    }

    var eventType = request.EventType.Trim().ToLowerInvariant();
    var supportedEvents = new[] { "impression", "play", "pause", "complete", "skip", "details-open", "playlist-add", "search-click" };

    if (!supportedEvents.Contains(eventType))
    {
        return Results.BadRequest("Unsupported event type.");
    }

    var songExists = await dbContext.Songs.AnyAsync(x => x.Id == request.SongId && x.IsActive);
    if (!songExists)
    {
        return Results.NotFound("Song was not found.");
    }

    dbContext.UserBehaviorEvents.Add(new UserBehaviorEvent
    {
        Id = Guid.NewGuid(),
        UserId = request.UserId,
        SongId = request.SongId,
        EventType = eventType,
        ContextType = string.IsNullOrWhiteSpace(request.ContextType) ? "unknown" : request.ContextType.Trim().ToLowerInvariant(),
        Source = string.IsNullOrWhiteSpace(request.Source) ? "web" : request.Source.Trim().ToLowerInvariant(),
        CompletionRate = request.CompletionRate,
        PlaybackPositionSeconds = request.PlaybackPositionSeconds,
        PlaybackDurationSeconds = request.PlaybackDurationSeconds,
        OccurredAtUtc = request.OccurredAtUtc == default ? DateTimeOffset.UtcNow : request.OccurredAtUtc
    });

    await dbContext.SaveChangesAsync();
    return Results.Ok();
});

catalogGroup.MapGet("/behavior/summary/{userId:guid}", async (Guid userId, HttpContext httpContext, CatalogDbContext dbContext) =>
{
    var authorizationResult = EnsureAuthorizedUser(httpContext.User, userId);
    if (authorizationResult is not null)
    {
        return authorizationResult;
    }

    var events = await dbContext.UserBehaviorEvents
        .AsNoTracking()
        .Include(x => x.Song)
        .ThenInclude(x => x.Genre)
        .Where(x => x.UserId == userId)
        .OrderByDescending(x => x.OccurredAtUtc)
        .Take(1000)
        .ToListAsync();

    var likeCount = await dbContext.UserSongPreferences.CountAsync(x => x.UserId == userId && x.FeedbackType == "like");
    var dislikeCount = await dbContext.UserSongPreferences.CountAsync(x => x.UserId == userId && x.FeedbackType == "dislike");

    var summary = new BehaviorSummaryDto(
        userId,
        events.Count,
        events.Count(x => x.EventType == "play"),
        events.Count(x => x.EventType == "complete" || (x.CompletionRate ?? 0) >= 0.8),
        events.Count(x => x.EventType == "skip"),
        likeCount,
        dislikeCount,
        events
            .GroupBy(x => x.Song.Genre.Name)
            .OrderByDescending(x => x.Count())
            .Take(5)
            .Select(x => new BehaviorTopGenreDto(x.Key, x.Count()))
            .ToList());

    return Results.Ok(summary);
});

catalogGroup.MapPost("/search/history", async (TrackSearchHistoryRequest request, HttpContext httpContext, CatalogDbContext dbContext) =>
{
    var authorizationResult = EnsureAuthorizedUser(httpContext.User, request.UserId);
    if (authorizationResult is not null)
    {
        return authorizationResult;
    }

    var keyword = request.Keyword.Trim();
    if (string.IsNullOrWhiteSpace(keyword))
    {
        return Results.BadRequest("Search keyword is required.");
    }

    var normalizedKeyword = keyword.ToUpperInvariant();
    var searchType = string.IsNullOrWhiteSpace(request.SearchType) ? "all" : request.SearchType.Trim().ToLowerInvariant();
    var source = string.IsNullOrWhiteSpace(request.Source) ? "web" : request.Source.Trim().ToLowerInvariant();

    dbContext.UserSearchHistories.Add(new UserSearchHistory
    {
        Id = Guid.NewGuid(),
        UserId = request.UserId,
        Keyword = keyword,
        NormalizedKeyword = normalizedKeyword,
        SearchType = searchType,
        Source = source,
        ResultCount = Math.Max(request.ResultCount, 0),
        SearchedAtUtc = request.SearchedAtUtc == default ? DateTimeOffset.UtcNow : request.SearchedAtUtc
    });

    await dbContext.SaveChangesAsync();
    return Results.Ok();
});

catalogGroup.MapGet("/search/history/{userId:guid}", async (Guid userId, HttpContext httpContext, CatalogDbContext dbContext) =>
{
    var authorizationResult = EnsureAuthorizedUser(httpContext.User, userId);
    if (authorizationResult is not null)
    {
        return authorizationResult;
    }

    var history = await dbContext.UserSearchHistories
        .AsNoTracking()
        .Where(x => x.UserId == userId)
        .OrderByDescending(x => x.SearchedAtUtc)
        .Take(50)
        .Select(x => new SearchHistoryItemDto(
            x.Id,
            x.UserId,
            x.Keyword,
            x.SearchType,
            x.Source,
            x.ResultCount,
            x.SearchedAtUtc))
        .ToListAsync();

    return Results.Ok(history);
});

var adminGroup = catalogGroup.MapGroup("/admin").WithTags("CatalogAdmin");

adminGroup.MapGet("/spotify/status", (SpotifyCatalogService spotifyService) =>
    Results.Ok(new SpotifyCatalogStatusDto(
        spotifyService.IsConfigured,
        spotifyService.IsConfigured
            ? "Spotify Web API credentials are configured."
            : "Configure Spotify:ClientId and Spotify:ClientSecret before using sync APIs.")));

adminGroup.MapGet("/spotify/search", async (
    string q,
    string? type,
    SpotifyCatalogService spotifyService,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(q))
    {
        return Results.Ok(new SpotifyCatalogSearchResponseDto([], [], []));
    }

    var searchType = string.IsNullOrWhiteSpace(type) ? "track,album,artist" : type.Trim().ToLowerInvariant();
    var result = await spotifyService.SearchCatalogAsync(q.Trim(), searchType, cancellationToken);
    return Results.Ok(result);
});

adminGroup.MapPost("/spotify/ensure-track", async (
    EnsureSpotifyTrackRequest request,
    CatalogDbContext dbContext,
    SpotifyCatalogService spotifyService,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.SpotifyTrackId))
    {
        return Results.BadRequest("SpotifyTrackId is required.");
    }

    SpotifyTrackSearchItemDto? track = null;
    if (!string.IsNullOrWhiteSpace(request.Title))
    {
        track = new SpotifyTrackSearchItemDto(
            request.SpotifyTrackId.Trim(),
            request.SpotifyUri ?? string.Empty,
            request.Title.Trim(),
            request.Artist?.Trim() ?? string.Empty,
            request.Album?.Trim() ?? string.Empty,
            request.PreviewUrl,
            request.CoverUrl ?? string.Empty,
            request.ExternalUrl ?? string.Empty,
            request.DurationSeconds ?? 0,
            request.Popularity ?? 0,
            request.ReleaseDate,
            false);
    }
    else
    {
        track = await spotifyService.GetTrackAsync(request.SpotifyTrackId.Trim(), cancellationToken);
    }

    if (track is null)
    {
        return Results.NotFound("Spotify track was not found.");
    }

    var genreName = string.IsNullOrWhiteSpace(request.GenreName)
        ? InferGenreFromTrack(track)
        : request.GenreName.Trim();
    var genre = await EnsureGenreAsync(dbContext, genreName, cancellationToken);

    var song = await dbContext.Songs
        .Include(x => x.Genre)
        .FirstOrDefaultAsync(x => x.SpotifyTrackId == track.SpotifyTrackId, cancellationToken);

    if (song is null)
    {
        song = new Song
        {
            Id = Guid.NewGuid(),
            Source = "spotify",
            SpotifyTrackId = track.SpotifyTrackId,
            Genre = genre
        };
        dbContext.Songs.Add(song);
    }

    song.Source = "spotify";
    song.SpotifyTrackId = track.SpotifyTrackId;
    song.SpotifyUri = track.SpotifyUri;
    song.ExternalUrl = track.ExternalUrl;
    song.Title = track.Title;
    song.Artist = track.Artist;
    song.Album = track.Album;
    song.GenreId = genre.Id;
    song.Genre = genre;
    song.CoverUrl = track.CoverUrl;
    song.AudioUrl = track.PreviewUrl ?? string.Empty;
    song.ReleaseDate = ParseSpotifyReleaseDate(track.ReleaseDate);
    song.Description = $"Imported from Spotify catalog. External playback: {track.ExternalUrl}";
    song.DurationSeconds = track.DurationSeconds;
    song.PopularityScore = Math.Round(track.Popularity / 100d, 4);
    song.IsColdStartCandidate = false;
    song.IsActive = true;
    SongAttributeProfileBuilder.Apply(song, genre.Name);

    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok(MapSongCard(song));
});

adminGroup.MapPost("/spotify/import", async (
    ImportSpotifyTracksRequest request,
    CatalogDbContext dbContext,
    SpotifyCatalogService spotifyService,
    CancellationToken cancellationToken) =>
{
    if (request.SpotifyTrackIds.Count == 0)
    {
        return Results.BadRequest("At least one Spotify track id is required.");
    }

    if (string.IsNullOrWhiteSpace(request.GenreName))
    {
        return Results.BadRequest("GenreName is required.");
    }

    var genre = await EnsureGenreAsync(dbContext, request.GenreName.Trim(), cancellationToken);
    var tracks = await spotifyService.GetTracksAsync(request.SpotifyTrackIds, cancellationToken);
    var importedSongIds = new List<Guid>();

    foreach (var track in tracks)
    {
        var existingSong = await dbContext.Songs.FirstOrDefaultAsync(
            x => x.SpotifyTrackId == track.SpotifyTrackId,
            cancellationToken);

        if (existingSong is null)
        {
            existingSong = new Song
            {
                Id = Guid.NewGuid(),
                Source = "spotify",
                SpotifyTrackId = track.SpotifyTrackId
            };
            dbContext.Songs.Add(existingSong);
        }

        existingSong.Source = "spotify";
        existingSong.ExternalUrl = track.ExternalUrl;
        existingSong.SpotifyUri = track.SpotifyUri;
        existingSong.Title = track.Title;
        existingSong.Artist = track.Artist;
        existingSong.Album = track.Album;
        existingSong.GenreId = genre.Id;
        existingSong.CoverUrl = track.CoverUrl;
        existingSong.AudioUrl = track.PreviewUrl ?? string.Empty;
        existingSong.ReleaseDate = ParseSpotifyReleaseDate(track.ReleaseDate);
        existingSong.Description = $"Imported from Spotify catalog. External playback: {track.ExternalUrl}";
        existingSong.DurationSeconds = track.DurationSeconds;
        existingSong.PopularityScore = Math.Round(track.Popularity / 100d, 4);
        existingSong.IsColdStartCandidate = request.MarkAsColdStartCandidate;
        existingSong.IsActive = true;
        SongAttributeProfileBuilder.Apply(existingSong, genre.Name);

        importedSongIds.Add(existingSong.Id);
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(new
    {
        importedCount = importedSongIds.Count,
        songIds = importedSongIds
    });
});

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await dbContext.Database.MigrateAsync();
    await CatalogSeedData.SeedAsync(dbContext);
}

app.Run();

static IResult? EnsureAuthorizedUser(ClaimsPrincipal user, Guid requestedUserId)
{
    if (user.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    var subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
    if (!Guid.TryParse(subject, out var authenticatedUserId))
    {
        return Results.Unauthorized();
    }

    return authenticatedUserId == requestedUserId
        ? null
        : Results.Forbid();
}

static SongCardDto MapSongCard(Song song) =>
    new(
        song.Id,
        song.Title,
        song.Artist,
        song.Album,
        song.Genre.Name,
        song.TempoTag,
        song.MoodTag,
        song.EnergyLevel,
        song.SpotifyUri,
        song.ExternalUrl,
        song.CoverUrl,
        song.AudioUrl,
        song.ReleaseDate,
        song.DurationSeconds,
        song.PopularityScore);

static SongDetailsDto MapSongDetails(Song song) =>
    new(
        song.Id,
        song.Title,
        song.Artist,
        song.Album,
        song.Genre.Name,
        song.TempoTag,
        song.MoodTag,
        song.EnergyLevel,
        song.SpotifyUri,
        song.ExternalUrl,
        song.CoverUrl,
        song.AudioUrl,
        song.ReleaseDate,
        song.DurationSeconds,
        song.PopularityScore,
        song.Description);

static async Task EnsureLikesPlaylistAsync(CatalogDbContext dbContext, Guid userId)
{
    var exists = await dbContext.Playlists.AnyAsync(x => x.UserId == userId && x.IsSystemPlaylist);
    if (exists)
    {
        return;
    }

    dbContext.Playlists.Add(new Playlist
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Name = "我喜欢的音乐",
        Description = "系统根据你的喜欢行为自动维护的默认歌单。",
        IsSystemPlaylist = true,
        CreatedAtUtc = DateTimeOffset.UtcNow
    });

    await dbContext.SaveChangesAsync();
}

static async Task<Genre> EnsureGenreAsync(
    CatalogDbContext dbContext,
    string genreName,
    CancellationToken cancellationToken)
{
    var genre = await dbContext.Genres.FirstOrDefaultAsync(
        x => x.Name == genreName,
        cancellationToken);

    if (genre is not null)
    {
        return genre;
    }

    genre = new Genre
    {
        Id = Guid.NewGuid(),
        Name = genreName
    };

    dbContext.Genres.Add(genre);
    await dbContext.SaveChangesAsync(cancellationToken);
    return genre;
}

static DateOnly? ParseSpotifyReleaseDate(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return null;
    }

    if (DateOnly.TryParse(value, out var exactDate))
    {
        return exactDate;
    }

    if (value.Length == 4 && int.TryParse(value, out var year))
    {
        return new DateOnly(year, 1, 1);
    }

    if (value.Length == 7 &&
        int.TryParse(value[..4], out year) &&
        int.TryParse(value[5..7], out var month))
    {
        return new DateOnly(year, month, 1);
    }

    return null;
}

static string InferGenreFromTrack(SpotifyTrackSearchItemDto track)
{
    var text = string.Join(' ', new[] { track.Title, track.Artist, track.Album }).ToLowerInvariant();

    if (text.Contains("rock") || text.Contains("metal") || text.Contains("punk"))
    {
        return "Rock";
    }

    if (text.Contains("jazz") || text.Contains("blues") || text.Contains("swing"))
    {
        return "Jazz";
    }

    if (text.Contains("hip hop") || text.Contains("rap") || text.Contains("trap"))
    {
        return "Hip-Hop";
    }

    if (text.Contains("classical") || text.Contains("orchestra") || text.Contains("piano"))
    {
        return "Classical";
    }

    if (text.Contains("edm") || text.Contains("dance") || text.Contains("house") || text.Contains("electro"))
    {
        return "Electronic";
    }

    if (text.Contains("folk") || text.Contains("acoustic"))
    {
        return "Folk";
    }

    return "Pop";
}

string CapitalizeFirstLetter(string input)
{
    if (string.IsNullOrWhiteSpace(input))
    {
        return input;
    }
    
    if (input.Length == 1)
    {
        return input.ToUpper();
    }
    
    return char.ToUpper(input[0]) + input.Substring(1);
}
