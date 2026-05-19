using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace MusicRec.Web.Services;

public sealed class SpotifySessionState(ProtectedLocalStorage storage)
{
    private const string StorageKey = "musicrec.spotify";
    private Task? initializationTask;

    public SpotifyAuthSession? Current { get; private set; }
    public bool IsReady { get; private set; }
    public bool IsConnected => Current is not null;

    public event Action? Changed;

    public Task InitializeAsync()
    {
        if (initializationTask is null || initializationTask.IsFaulted || initializationTask.IsCanceled)
        {
            initializationTask = InitializeCoreAsync();
        }

        return initializationTask;
    }

    private async Task InitializeCoreAsync()
    {
        try
        {
            await ReloadCoreAsync();
            IsReady = true;
            Changed?.Invoke();
        }
        catch
        {
            initializationTask = null;
        }
    }

    public async Task ReloadAsync()
    {
        await ReloadCoreAsync();
        IsReady = true;
        initializationTask = Task.CompletedTask;
        Changed?.Invoke();
    }

    private async Task ReloadCoreAsync()
    {
        var result = await storage.GetAsync<SpotifyAuthSession>(StorageKey);
        Current = result.Success ? result.Value : null;
    }

    public async Task SetAsync(SpotifyAuthSession session)
    {
        Current = session;
        IsReady = true;
        initializationTask = Task.CompletedTask;
        await storage.SetAsync(StorageKey, session);
        Changed?.Invoke();
    }

    public async Task ClearAsync()
    {
        Current = null;
        IsReady = true;
        initializationTask = Task.CompletedTask;
        await storage.DeleteAsync(StorageKey);
        Changed?.Invoke();
    }
}
