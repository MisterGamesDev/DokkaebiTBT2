using System.Collections.Generic;
using UnityEngine;

namespace Dokkaebi.Interfaces
{
    /// <summary>
    /// Interface for the grid system to break cyclic dependencies between modules
    /// </summary>
    public interface IGridSystem
    {
        /// <summary>
        /// Width of the grid in cells
        /// </summary>
        int Width { get; }
        
        /// <summary>
        /// Height of the grid in cells
        /// </summary>
        int Height { get; }
        
        /// <summary>
        /// Size of a cell in world units
        /// </summary>
        float CellSize { get; }

        /// <summary>
        /// The world-space position corresponding to grid position (0, 0)
        /// </summary>
        Vector3 GridOrigin { get; }
        
        /// <summary>
        /// Convert a grid position to a world position
        /// </summary>
        Vector3 GridToWorldPosition(GridPosition gridPosition);
        
        /// <summary>
        /// Convert a world position to a grid position
        /// </summary>
        GridPosition WorldToGridPosition(Vector3 worldPosition);
        
        /// <summary>
        /// Check if a grid position is valid (within bounds)
        /// </summary>
        bool IsValidGridPosition(GridPosition gridPosition);
        
        /// <summary>
        /// Get neighboring grid positions (orthogonal)
        /// </summary>
        List<GridPosition> GetNeighborPositions(GridPosition gridPosition, bool includeDiagonals = false);
        
        /// <summary>
        /// Get all grid positions within a specific range
        /// </summary>
        List<GridPosition> GetGridPositionsInRange(GridPosition centerPosition, int range);
        
        /// <summary>
        /// Check if a grid position is occupied by a unit or obstacle
        /// </summary>
        bool IsPositionOccupied(GridPosition gridPosition);
    }
} 