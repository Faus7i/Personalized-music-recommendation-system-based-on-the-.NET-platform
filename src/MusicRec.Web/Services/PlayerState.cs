using MusicRec.BuildingBlocks.Contracts.Catalog;

namespace MusicRec.Web.Services;

public sealed class PlayerState
{
    public SongDetailsDto? CurrentSong { get; private set; }
    public SongDetailsDto? SelectedSong { get; private set; }
    public bool IsDetailsOpen { get; private set; }

    public event Action? Changed;

    public void OpenDetails(SongDetailsDto song)
    {
        SelectedSong = song;
        IsDetailsOpen = true;
        Changed?.Invoke();
    }

    public void CloseDetails()
    {
        IsDetailsOpen = false;
        Changed?.Invoke();
    }

    public void Play(SongDetailsDto song)
    {
        CurrentSong = song;
        SelectedSong = song;
        IsDetailsOpen = true;
        Changed?.Invoke();
    }
}
