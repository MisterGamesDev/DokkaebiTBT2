using UnityEngine;

namespace Dokkaebi.Common
{
    public interface IMoveable
    {
        bool CanReachPosition(Vector2Int targetPosition);
        void SetTargetPosition(Vector2Int targetPosition);
        bool HasPendingMovement();
        void StopMovement();
    }
} 