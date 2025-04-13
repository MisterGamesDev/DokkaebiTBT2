using UnityEngine; // Using Vector2Int assumes Unity context
using System.Collections.Generic;

namespace Dokkaebi.Interfaces
{
    /// <summary>
    /// Provides the necessary information about the grid topology
    /// and state for pathfinding algorithms, without exposing the
    /// full Grid implementation.
    /// </summary>
    public interface IPathfindingGridInfo
    {
        /// <summary>
        /// Checks if the grid cell at the given coordinates is considered walkable/traversable.
        /// </summary>
        /// <param name="coordinates">The grid coordinates (e.g., Vector2Int).</param>
        /// <returns>True if the cell is walkable, false otherwise.</returns>
        bool IsWalkable(Vector2Int coordinates);

        /// <summary>
        /// Checks if the grid cell at the given coordinates is considered walkable/traversable,
        /// optionally ignoring a specific unit when checking occupancy.
        /// </summary>
        /// <param name="coordinates">The grid coordinates.</param>
        /// <param name="requestingUnit">The unit requesting the check (will be ignored for occupancy).</param>
        /// <returns>True if the cell is walkable, false otherwise.</returns>
        bool IsWalkable(GridPosition coordinates, IDokkaebiUnit requestingUnit);

        /// <summary>
        /// Gets the movement cost associated with traversing the cell at the given coordinates.
        /// </summary>
        /// <param name="coordinates">The grid coordinates.</param>
        /// <returns>An integer representing the cost (e.g., 1 for normal, higher for difficult terrain).</returns>
        int GetNodeCost(Vector2Int coordinates);

        /// <summary>
        /// Gets the valid, walkable neighbouring grid coordinates for a given cell.
        /// </summary>
        /// <param name="coordinates">The coordinates of the cell to find neighbours for.</param>
        /// <returns>An enumerable collection of walkable neighbour coordinates.</returns>
        IEnumerable<Vector2Int> GetWalkableNeighbours(Vector2Int coordinates);
        
        /// <summary>
        /// Gets the size of a single grid cell.
        /// </summary>
        /// <returns>The size of one grid cell as a float.</returns>
        float GetGridCellSize();

        // --- Optional additions based on Pathfinding needs ---

        // /// <summary>
        // /// Gets the total width of the grid, if required by the algorithm setup.
        // /// </summary>
        // int GridWidth { get; }

        // /// <summary>
        // /// Gets the total height of the grid, if required by the algorithm setup.
        // /// </summary>
        // int GridHeight { get; }

        // /// <summary>
        // /// Converts grid coordinates to world position, if pathfinding needs it.
        // /// </summary>
        // Vector3 GetWorldPosition(Vector2Int coordinates);
    }
}