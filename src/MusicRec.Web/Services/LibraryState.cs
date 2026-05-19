namespace MusicRec.Web.Services;

public sealed class LibraryState
{
    public event Action? Changed;

    public void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
