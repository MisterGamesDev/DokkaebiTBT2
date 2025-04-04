using UnityEngine;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Utilities
{
    /// <summary>
    /// Utility class to convert between different GridPosition types
    /// </summary>
    public static class GridPositionConverter
    {
        /// <summary>
        /// Convert from Vector2Int to Interfaces.GridPosition
        /// </summary>
        public static Interfaces.GridPosition ToInterfaces(Vector2Int position)
        {
            return new Interfaces.GridPosition(position.x, position.y);
        }
        
        /// <summary>
        /// Convert from Interfaces.GridPosition to Vector2Int
        /// </summary>
        public static Vector2Int ToVector2Int(Interfaces.GridPosition position)
        {
            return new Vector2Int(position.x, position.z);
        }
        
        /// <summary>
        /// Convert from world position to Interfaces.GridPosition
        /// </summary>
        public static Interfaces.GridPosition WorldToGrid(Vector3 worldPosition)
        {
            // This assumes a grid with 1 unit cell size
            // For more complex conversions, use GridManager
            return new Interfaces.GridPosition(
                Mathf.RoundToInt(worldPosition.x),
                Mathf.RoundToInt(worldPosition.z)
            );
        }
        
        /// <summary>
        /// Convert from Interfaces.GridPosition to world position
        /// </summary>
        public static Vector3 GridToWorld(Interfaces.GridPosition gridPosition, float y = 0)
        {
            // This assumes a grid with 1 unit cell size
            // For more complex conversions, use GridManager
            return new Vector3(gridPosition.x, y, gridPosition.z);
        }
    }
} 