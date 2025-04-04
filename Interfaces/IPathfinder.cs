using System.Collections.Generic;

namespace Dokkaebi.Interfaces
{
    /// <summary>
    /// Interface for pathfinding services.
    /// This abstraction allows Grid code to use pathfinding
    /// without directly depending on a specific implementation.
    /// </summary>
    public interface IPathfinder
    {
        /// <summary>
        /// Check if a path exists between two grid positions
        /// </summary>
        bool PathExists(GridPosition start, GridPosition end);
        
        /// <summary>
        /// Check if a position is walkable
        /// </summary>
        bool IsWalkable(GridPosition position);
        
        /// <summary>
        /// Find all walkable positions within a certain range
        /// </summary>
        List<GridPosition> GetWalkablePositionsInRange(GridPosition start, int range);
        
        /// <summary>
        /// Get the path between two grid positions
        /// </summary>
        List<GridPosition> GetPath(GridPosition start, GridPosition end);
        
        /// <summary>
        /// Get the distance between two grid positions considering pathfinding
        /// </summary>
        int GetPathDistance(GridPosition start, GridPosition end);
    }
    
    /// <summary>
    /// Delegate for path completion callbacks
    /// </summary>
    public delegate void PathCompleteCallback(List<GridPosition> path);
} 