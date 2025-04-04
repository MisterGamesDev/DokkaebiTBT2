using System.Collections.Generic;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Common
{
    /// <summary>
    /// Interface for the pathfinding system to break cyclic dependencies
    /// </summary>
    public interface IPathfinding
    {
        /// <summary>
        /// Find a path between two grid positions
        /// </summary>
        /// <param name="startPosition">Starting grid position</param>
        /// <param name="endPosition">Target grid position</param>
        /// <param name="includeDiagonals">Whether diagonal movement is allowed</param>
        /// <returns>List of grid positions forming the path (including start, excluding end)</returns>
        List<GridPosition> FindPath(GridPosition startPosition, GridPosition endPosition, bool includeDiagonals = false);
        
        /// <summary>
        /// Get all reachable grid positions within a movement range
        /// </summary>
        /// <param name="startPosition">Starting grid position</param>
        /// <param name="movementRange">Maximum movement range in grid cells</param>
        /// <param name="includeDiagonals">Whether diagonal movement is allowed</param>
        /// <returns>Dictionary mapping reachable positions to their movement cost</returns>
        Dictionary<GridPosition, int> GetReachablePositions(GridPosition startPosition, int movementRange, bool includeDiagonals = false);
        
        /// <summary>
        /// Calculate the movement cost between adjacent grid positions
        /// </summary>
        int CalculateMovementCost(GridPosition fromPosition, GridPosition toPosition);
        
        /// <summary>
        /// Get the movement cost to reach a target position from a start position
        /// </summary>
        int GetPathCost(GridPosition startPosition, GridPosition targetPosition);
    }
} 