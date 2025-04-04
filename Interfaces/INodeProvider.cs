using UnityEngine;

namespace Dokkaebi.Interfaces
{
    /// <summary>
    /// Interface for providing graph nodes based on positions.
    /// This abstraction allows Grid code to get nodes from a pathfinding system
    /// without directly depending on a specific implementation.
    /// </summary>
    public interface INodeProvider
    {
        /// <summary>
        /// Gets a node from a world position
        /// </summary>
        IGraphNode GetNodeFromWorldPosition(Vector3 worldPosition);
        
        /// <summary>
        /// Gets a node from a grid position
        /// </summary>
        IGraphNode GetNodeFromGridPosition(GridPosition gridPosition);
        
        /// <summary>
        /// Checks if a grid position is walkable
        /// </summary>
        bool IsWalkable(GridPosition gridPosition);
    }
} 