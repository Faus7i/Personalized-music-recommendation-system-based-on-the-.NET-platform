using Microsoft.EntityFrameworkCore;
using MusicRec.Services.Catalog.Api.Data.Entities;
using MusicRec.Services.Catalog.Api.Services;

namespace MusicRec.Services.Catalog.Api.Data;

public static class CatalogSeedData
{
    public static async Task SeedAsync(CatalogDbContext dbContext)
    {
        var genreMap = await EnsureGenresAsync(dbContext);
        var seedSongs = BuildSeedSongs(genreMap);
        var genreById = genreMap.Values.ToDictionary(x => x.Id);
        var curatedKeys = seedSongs
            .Select(x => ToSongKey(x.Title, x.Artist))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existingSongs = await dbContext.Songs
            .Include(x => x.Genre)
            .ToListAsync();
        var existingSongMap = existingSongs
            .GroupBy(x => ToSongKey(x.Title, x.Artist), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.First(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var seedSong in seedSongs)
        {
            if (existingSongMap.TryGetValue(ToSongKey(seedSong.Title, seedSong.Artist), out var existingSong))
            {
                existingSong.Album = seedSong.Album;
                existingSong.GenreId = seedSong.GenreId;
                existingSong.CoverUrl = seedSong.CoverUrl;
                existingSong.AudioUrl = seedSong.AudioUrl;
                existingSong.ReleaseDate = seedSong.ReleaseDate;
                existingSong.Description = seedSong.Description;
                existingSong.DurationSeconds = seedSong.DurationSeconds;
                existingSong.PopularityScore = seedSong.PopularityScore;
                existingSong.IsColdStartCandidate = seedSong.IsColdStartCandidate;
                existingSong.IsActive = true;
                SongAttributeProfileBuilder.Apply(existingSong, genreById[seedSong.GenreId].Name);
                continue;
            }

            SongAttributeProfileBuilder.Apply(seedSong, genreById[seedSong.GenreId].Name);
            dbContext.Songs.Add(seedSong);
        }

        foreach (var extraSong in existingSongs.Where(x => !curatedKeys.Contains(ToSongKey(x.Title, x.Artist))))
        {
            if (!string.IsNullOrWhiteSpace(extraSong.AudioUrl) &&
                !extraSong.CoverUrl.Contains("placehold.co", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            extraSong.IsActive = false;
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task<Dictionary<string, Genre>> EnsureGenresAsync(CatalogDbContext dbContext)
    {
        var requiredGenres = new[]
        {
            "Pop",
            "Dance-pop",
            "Alternative",
            "Synth-pop",
            "Electropop"
        };

        var existingGenres = await dbContext.Genres.ToDictionaryAsync(x => x.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var genreName in requiredGenres.Where(x => !existingGenres.ContainsKey(x)))
        {
            var genre = new Genre { Id = Guid.NewGuid(), Name = genreName };
            existingGenres[genreName] = genre;
            dbContext.Genres.Add(genre);
        }

        await dbContext.SaveChangesAsync();
        return await dbContext.Genres.ToDictionaryAsync(x => x.Name, StringComparer.OrdinalIgnoreCase);
    }

    private static List<Song> BuildSeedSongs(IReadOnlyDictionary<string, Genre> genres)
    {
        const string shortPreview = "https://samplelib.com/lib/preview/mp3/sample-12s.mp3";
        const string altPreview = "https://samplelib.com/lib/preview/mp3/sample-15s.mp3";
        const string mellowPreview = "https://samplelib.com/lib/preview/mp3/sample-9s.mp3";

        return
        [
            BuildSong("Blinding Lights", "The Weeknd", "After Hours", genres["Synth-pop"].Id, "https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=cinematic%20album%20cover%2C%20neon%20city%20street%20at%20night%2C%20retro%20sports%20car%20tail%20lights%2C%20moody%20blue%20and%20amber%20glow%2C%20premium%20music%20art%2C%20square%20composition&image_size=square_hd", shortPreview, new DateOnly(2020, 3, 20), "A neon-soaked synth anthem built for late-night drives and bright city horizons.", 200, 0.98, true),
            BuildSong("Levitating", "Dua Lipa", "Future Nostalgia", genres["Dance-pop"].Id, "https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=futuristic%20disco%20album%20cover%2C%20female%20silhouette%20floating%20in%20pink%20and%20violet%20lights%2C%20chrome%20stars%2C%20stylish%20pop%20art%2C%20premium%20square%20cover&image_size=square_hd", altPreview, new DateOnly(2020, 10, 1), "Glossy disco-pop energy with buoyant hooks, glittering percussion, and retro-future flair.", 203, 0.95, true),
            BuildSong("Believer", "Imagine Dragons", "Evolve", genres["Alternative"].Id, "https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=dramatic%20alternative%20rock%20album%20cover%2C%20storm%20clouds%20over%20desert%20monument%2C%20crimson%20lightning%2C%20bold%20high-contrast%20composition%2C%20square%20music%20art&image_size=square_hd", shortPreview, new DateOnly(2017, 2, 1), "Percussive rock intensity driven by resilience, pain, and explosive self-belief.", 204, 0.91, true),
            BuildSong("Someone You Loved", "Lewis Capaldi", "Divinely Uninspired", genres["Pop"].Id, "https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=emotional%20ballad%20album%20cover%2C%20rainy%20window%20portrait%2C%20soft%20gray%20and%20blue%20tones%2C%20minimal%20cinematic%20lighting%2C%20premium%20square%20cover&image_size=square_hd", mellowPreview, new DateOnly(2018, 11, 8), "A stark piano-led pop ballad centered on heartbreak, absence, and fragile healing.", 182, 0.87, true),
            BuildSong("Bad Guy", "Billie Eilish", "When We All Fall Asleep", genres["Electropop"].Id, "https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=avant-garde%20electropop%20album%20cover%2C%20minimal%20studio%20portrait%2C%20acid%20yellow%20background%2C%20playful%20dark%20fashion%2C%20editorial%20square%20art&image_size=square_hd", altPreview, new DateOnly(2019, 3, 29), "Playful and off-kilter electropop with whispered hooks and sharply stylized menace.", 194, 0.92, true),
            BuildSong("Shape of You", "Ed Sheeran", "Divide", genres["Pop"].Id, "https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=bright%20modern%20pop%20album%20cover%2C%20abstract%20blue%20brush%20texture%2C%20clean%20graphic%20composition%2C%20commercial%20music%20art%2C%20premium%20square&image_size=square_hd", shortPreview, new DateOnly(2017, 1, 6), "An upbeat global pop crossover built around acoustic rhythm and infectious melody.", 233, 0.97, true),
            BuildSong("Starboy", "The Weeknd", "Starboy", genres["Synth-pop"].Id, "https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=dark%20futuristic%20album%20cover%2C%20red%20laser%20cross%20lighting%2C%20moody%20male%20silhouette%2C%20luxury%20pop%20editorial%20look%2C%20square&image_size=square_hd", altPreview, new DateOnly(2016, 9, 22), "A sleek synth-pop cut balancing swagger, chrome textures, and night-drive drama.", 230, 0.94, true),
            BuildSong("As It Was", "Harry Styles", "Harry's House", genres["Pop"].Id, "https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=soft%20vintage%20pop%20album%20cover%2C%20warm%20interior%20room%2C%20tilted%20perspective%2C%20sunlit%20minimalist%20scene%2C%20premium%20square%20art&image_size=square_hd", mellowPreview, new DateOnly(2022, 4, 1), "A bittersweet pop single with breezy rhythm, nostalgic synths, and intimate tension.", 167, 0.93, true),
            BuildSong("Midnight Metro", "Aurora Lane", "Signals", genres["Synth-pop"].Id, "https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=synthwave%20album%20cover%2C%20empty%20midnight%20subway%20platform%2C%20blue%20neon%20reflections%2C%20cinematic%20square%20music%20art&image_size=square_hd", shortPreview, new DateOnly(2023, 8, 15), "A polished synth-pop track that mixes urban solitude with shimmering electronic textures.", 215, 0.82, false),
            BuildSong("Paper Planets", "Nova Harbor", "Orbit Room", genres["Dance-pop"].Id, "https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=dreamy%20dance-pop%20album%20cover%2C%20paper%20planets%20floating%20over%20pastel%20sky%2C%20editorial%20music%20design%2C%20premium%20square&image_size=square_hd", altPreview, new DateOnly(2023, 5, 12), "A bright dance-pop single with airy vocals, glossy rhythm, and feel-good lift.", 221, 0.80, false),
            BuildSong("Anti-Hero", "Taylor Swift", "Midnights", genres["Synth-pop"].Id, "https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=moody%20midnight%20pop%20album%20cover%2C%20lavender%20bedroom%20glow%2C%20mirror%20reflection%2C%20editorial%20square%20music%20art&image_size=square_hd", mellowPreview, new DateOnly(2022, 10, 21), "A confessional synth-pop single balancing self-awareness, bite, and polished late-night production.", 200, 0.96, false),
            BuildSong("Flowers", "Miley Cyrus", "Endless Summer Vacation", genres["Pop"].Id, "https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=sunlit%20pop%20album%20cover%2C%20golden%20outdoor%20garden%2C%20confident%20female%20silhouette%2C%20luxury%20editorial%20square%20art&image_size=square_hd", shortPreview, new DateOnly(2023, 1, 13), "A resilient pop anthem with warm groove, open-air brightness, and self-assured energy.", 200, 0.90, false),
            BuildSong("Heat Waves", "Glass Animals", "Dreamland", genres["Alternative"].Id, "https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=indie%20alternative%20album%20cover%2C%20summer%20night%20street%20lights%2C%20hazy%20orange%20glow%2C%20dreamy%20square%20composition&image_size=square_hd", altPreview, new DateOnly(2020, 6, 29), "A hazy alternative-pop track with nostalgic warmth, soft grooves, and nocturnal longing.", 239, 0.89, false),
            BuildSong("Unstoppable", "Sia", "This Is Acting", genres["Electropop"].Id, "https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=bold%20electropop%20album%20cover%2C%20powerful%20spotlight%20stage%2C%20high-contrast%20black%20and%20gold%2C%20editorial%20square%20art&image_size=square_hd", shortPreview, new DateOnly(2016, 1, 21), "A high-energy empowerment track designed around soaring choruses and dramatic electronic punch.", 217, 0.88, false),
            BuildSong("Easy On Me", "Adele", "30", genres["Pop"].Id, "https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=classic%20ballad%20album%20cover%2C%20soft%20sepia%20portrait%2C%20windblown%20hair%2C%20minimal%20cinematic%20square%20art&image_size=square_hd", mellowPreview, new DateOnly(2021, 10, 15), "A reflective piano-led ballad with intimate delivery and restrained emotional force.", 224, 0.91, false),
            BuildSong("Golden Hour", "JVKE", "Golden Hour", genres["Synth-pop"].Id, "https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=golden%20sunset%20pop%20album%20cover%2C%20piano%20silhouette%2C%20warm%20orange%20sky%2C%20romantic%20square%20music%20art&image_size=square_hd", mellowPreview, new DateOnly(2022, 7, 15), "A lush modern pop ballad centered on cinematic piano, warm light, and romantic sweep.", 209, 0.86, false),
            BuildSong("Save Your Tears", "The Weeknd", "After Hours", genres["Synth-pop"].Id, "https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=glossy%20synth-pop%20album%20cover%2C%20vintage%20spotlight%20stage%2C%20red%20curtain%20and%20midnight%20blue%2C%20premium%20square%20art&image_size=square_hd", altPreview, new DateOnly(2020, 8, 9), "A sleek synth-pop breakup anthem with glossy drama and polished retro hooks.", 215, 0.92, false),
            BuildSong("Calm Down", "Rema", "Rave & Roses", genres["Dance-pop"].Id, "https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=afropop%20dance%20album%20cover%2C%20vibrant%20city%20colors%2C%20emerald%20and%20orange%20lights%2C%20premium%20square%20music%20art&image_size=square_hd", shortPreview, new DateOnly(2022, 2, 11), "A buoyant crossover hit driven by airy rhythm, warm melody, and relaxed club energy.", 239, 0.90, false),
            BuildSong("Neon Tides", "Skyline Echo", "Afterglow Arcade", genres["Electropop"].Id, "https://coresg-normal.trae.ai/api/ide/v1/text_to_image?prompt=neon%20electropop%20album%20cover%2C%20glowing%20ocean%20waves%20at%20night%2C%20cyan%20and%20magenta%20palette%2C%20square%20cover&image_size=square_hd", altPreview, new DateOnly(2024, 2, 2), "An upbeat electropop cut mixing coastal night-drive atmosphere with bright hook-driven momentum.", 214, 0.78, false)
        ];
    }

    private static Song BuildSong(
        string title,
        string artist,
        string album,
        Guid genreId,
        string coverUrl,
        string audioUrl,
        DateOnly releaseDate,
        string description,
        int durationSeconds,
        double popularityScore,
        bool isColdStartCandidate) =>
        new()
        {
            Id = Guid.NewGuid(),
            Source = "seed",
            Title = title,
            Artist = artist,
            Album = album,
            GenreId = genreId,
            CoverUrl = coverUrl,
            AudioUrl = audioUrl,
            ReleaseDate = releaseDate,
            Description = description,
            DurationSeconds = durationSeconds,
            PopularityScore = popularityScore,
            IsColdStartCandidate = isColdStartCandidate,
            IsActive = true
        };

    private static string ToSongKey(string title, string artist) =>
        $"{title.Trim()}::{artist.Trim()}";
}
