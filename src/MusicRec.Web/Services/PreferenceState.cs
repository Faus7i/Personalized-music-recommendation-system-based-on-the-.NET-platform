using MusicRec.BuildingBlocks.Contracts.Catalog;

namespace MusicRec.Web.Services;

public sealed class PreferenceState : IDisposable
{
    private readonly CatalogApiClient catalogApiClient;
    private readonly UserSessionState session;
    private readonly LibraryState libraryState;
    private readonly SemaphoreSlim refreshLock = new(1, 1);

    private HashSet<Guid> likedSongIds = [];
    private HashSet<Guid> dislikedSongIds = [];

    public PreferenceState(CatalogApiClient catalogApiClient, UserSessionState session, LibraryState libraryState)
    {
        this.catalogApiClient = catalogApiClient;
        this.session = session;
        this.libraryState = libraryState;

        session.Changed += OnStateChanged;
        libraryState.Changed += OnStateChanged;

        _ = RefreshAsync();
    }

    public bool IsReady { get; private set; }

    public event Action? Changed;

    public bool IsLiked(Guid songId) => likedSongIds.Contains(songId);

    public bool IsDisliked(Guid songId) => dislikedSongIds.Contains(songId);

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await refreshLock.WaitAsync(cancellationToken);

        try
        {
            if (!session.IsReady || !session.IsAuthenticated)
            {
                likedSongIds = [];
                dislikedSongIds = [];
                IsReady = session.IsReady;
                return;
            }

            var preferences = await catalogApiClient.GetPreferencesAsync(session.CurrentUser!.UserId, cancellationToken);
            likedSongIds = preferences
                .Where(x => x.FeedbackType == "like")
                .Select(x => x.SongId)
                .ToHashSet();
            dislikedSongIds = preferences
                .Where(x => x.FeedbackType == "dislike")
                .Select(x => x.SongId)
                .ToHashSet();
            IsReady = true;
        }
        finally
        {
            refreshLock.Release();
            Changed?.Invoke();
        }
    }

    public void Apply(SongPreferenceResultDto preference)
    {
        if (preference.FeedbackType == "like")
        {
            likedSongIds.Add(preference.SongId);
            dislikedSongIds.Remove(preference.SongId);
        }
        else if (preference.FeedbackType == "dislike")
        {
            dislikedSongIds.Add(preference.SongId);
            likedSongIds.Remove(preference.SongId);
        }

        IsReady = true;
        Changed?.Invoke();
    }

    private void OnStateChanged() => _ = RefreshAsync();

    public void Dispose()
    {
        session.Changed -= OnStateChanged;
        libraryState.Changed -= OnStateChanged;
        refreshLock.Dispose();
    }
}
