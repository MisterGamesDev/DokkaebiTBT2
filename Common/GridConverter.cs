using UnityEngine;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Common
{
    /// <summary>
    /// Handles basic conversions between different coordinate systems:
    /// - Grid positions (GridPosition)
    /// - World positions (Vector3)
    /// - Vector2Int positions
    /// </summary>
    public static class GridConverter
    {
        // Grid cell size in world units
        public static float CellSize = 1.0f;
        
        // Grid origin in world space
        public static Vector3 GridOrigin = Vector3.zero;
        
        // Default grid height in world space
        public static float DefaultGridHeight = 0f;

        /// <summary>
        /// Convert a grid position to a world position.
        /// </summary>
        public static Vector3 GridToWorld(GridPosition gridPosition, float height = -1f)
        {
            // Use provided height or default
            float yPos = height >= 0 ? height : DefaultGridHeight;
            
            return new Vector3(
                GridOrigin.x + (gridPosition.x * CellSize) + (CellSize / 2f),
                yPos,
                GridOrigin.z + (gridPosition.z * CellSize) + (CellSize / 2f)
            );
        }

        /// <summary>
        /// Convert a world position to a grid position.
        /// </summary>
        public static GridPosition WorldToGrid(Vector3 worldPosition)
        {
            // Subtract the centering offset before converting to grid coordinates
            float centeredX = worldPosition.x - (CellSize / 2f);
            float centeredZ = worldPosition.z - (CellSize / 2f);
            
            // Convert to grid coordinates
            int x = Mathf.FloorToInt((centeredX - GridOrigin.x) / CellSize);
            int z = Mathf.FloorToInt((centeredZ - GridOrigin.z) / CellSize);
            
            // Clamp to valid grid range (assuming 10x10 grid)
            x = Mathf.Clamp(x, 0, 9);
            z = Mathf.Clamp(z, 0, 9);
            
            return new GridPosition(x, z);
        }

        /// <summary>
        /// Convert a Vector2Int (X,Y) to a GridPosition (X,Z).
        /// Note: The Y component of Vector2Int becomes the Z component of GridPosition
        /// since we use the XZ plane for our grid.
        /// </summary>
        public static GridPosition Vector2IntToGrid(Vector2Int vector)
        {
            return new GridPosition(vector.x, vector.y);
        }

        /// <summary>
        /// Convert a GridPosition (X,Z) to a Vector2Int (X,Y).
        /// Note: The Z component of GridPosition becomes the Y component of Vector2Int
        /// since we use the XZ plane for our grid.
        /// </summary>
        public static Vector2Int GridToVector2Int(GridPosition gridPosition)
        {
            return new Vector2Int(gridPosition.x, gridPosition.z);
        }

        /// <summary>
        /// Gets the distance between two grid positions in grid units (Manhattan distance)
        /// </summary>
        public static int GetGridDistance(GridPosition a, GridPosition b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
        }
    }
} 