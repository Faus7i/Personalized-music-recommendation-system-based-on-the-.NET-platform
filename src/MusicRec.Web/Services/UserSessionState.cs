using MusicRec.BuildingBlocks.Contracts.Auth;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace MusicRec.Web.Services;

public sealed class UserSessionState(ProtectedLocalStorage storage)
{
    private const string StorageKey = "musicrec.auth";
    private Task? initializationTask;

    public AuthResponse? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;
    public bool IsReady { get; private set; }

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
            var result = await storage.GetAsync<AuthResponse>(StorageKey);
            CurrentUser = result.Success ? result.Value : null;
            IsReady = true;
            Changed?.Invoke();
        }
        catch
        {
            initializationTask = null;
        }
    }

    public async Task SetUserAsync(AuthResponse user)
    {
        CurrentUser = user;
        IsReady = true;
        await storage.SetAsync(StorageKey, user);
        Changed?.Invoke();
    }

    public async Task ClearAsync()
    {
        CurrentUser = null;
        IsReady = true;
        await storage.DeleteAsync(StorageKey);
        Changed?.Invoke();
    }
}
