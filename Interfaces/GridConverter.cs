// Inside Scripts/Dokkaebi/Common/GridConverter.cs

using UnityEngine;
using Dokkaebi.Interfaces; // Make sure this using statement exists

public static class GridConverter
{
    // Grid cell size in world units
    public static float CellSize = 1.0f;

    // Grid origin in world space
    public static Vector3 GridOrigin = Vector3.zero;

    // Default grid height in world space
    public static float DefaultGridHeight = 0f;

    /// <summary>
    /// Convert a grid position to a world position (center of the tile).
    /// </summary>
    public static Vector3 GridToWorld(GridPosition gridPosition, float height = -1f)
    {
        // Use provided height or default
        float yPos = height >= 0 ? height : DefaultGridHeight;

        // --- THIS IS THE CRITICAL PART ---
        // Add half the cell size to X and Z to center the position within the tile
        float worldX = GridOrigin.x + (gridPosition.x * CellSize) + (CellSize / 2f);
        float worldZ = GridOrigin.z + (gridPosition.z * CellSize) + (CellSize / 2f);
        // --- END CRITICAL PART ---

        return new Vector3(worldX, yPos, worldZ);
    }

    // (Keep the rest of the methods as they are or apply the suggested WorldToGrid change from the previous response if you prefer)
    // ... other methods like WorldToGrid, Vector2IntToGrid, etc. ...
     /// <summary>
    /// Convert a world position to a grid position.
    /// </summary>
    public static GridPosition WorldToGrid(Vector3 worldPosition)
    {
        // Calculate grid coordinates relative to the origin
        float relativeX = worldPosition.x - GridOrigin.x;
        float relativeZ = worldPosition.z - GridOrigin.z;

        // Convert to grid coordinates using FloorToInt for consistency
        int x = Mathf.FloorToInt(relativeX / CellSize);
        int z = Mathf.FloorToInt(relativeZ / CellSize);

        // Clamp to valid grid range (assuming 10x10) - Get these dynamically if possible
        int gridWidth = 10;
        int gridHeight = 10;
        x = Mathf.Clamp(x, 0, gridWidth - 1);
        z = Mathf.Clamp(z, 0, gridHeight - 1);

        return new GridPosition(x, z);
    }

     public static GridPosition Vector2IntToGrid(Vector2Int vector)
    {
        return new GridPosition(vector.x, vector.y);
    }

    public static Vector2Int GridToVector2Int(GridPosition gridPosition)
    {
        return new Vector2Int(gridPosition.x, gridPosition.z);
    }
     public static int GetGridDistance(GridPosition a, GridPosition b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
    }
}