namespace Dokkaebi.Interfaces
{
    /// <summary>
    /// Provides access to register/unregister for custom update calls.
    /// </summary>
    public interface ICoreUpdateService
    {
        void RegisterUpdateObserver(IUpdateObserver observer);
        void UnregisterUpdateObserver(IUpdateObserver observer);
    }
}