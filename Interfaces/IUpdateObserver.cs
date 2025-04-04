namespace Dokkaebi.Interfaces
{
    /// <summary>
    /// Interface for objects that want to receive custom update calls.
    /// </summary>
    public interface IUpdateObserver
    {
        void CustomUpdate(float deltaTime);
    }
}