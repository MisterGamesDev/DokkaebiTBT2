using UnityEngine;

namespace Dokkaebi.Interfaces
{
    /// <summary>
    /// Interface for graph nodes in pathfinding systems.
    /// This abstraction allows Grid code to reference pathfinding concepts
    /// without directly depending on the Pathfinding namespace.
    /// </summary>
    public interface IGraphNode
    {
        /// <summary>
        /// Whether this node is walkable
        /// </summary>
        bool Walkable { get; }
        
        /// <summary>
        /// Position of the node in world space
        /// </summary>
        Vector3 Position { get; }
        
        /// <summary>
        /// Get the penalty for traversing this node
        /// </summary>
        int Penalty { get; }
        
        /// <summary>
        /// Check if this node has a connection to another node
        /// </summary>
        bool HasConnectionTo(IGraphNode other);
        
    }
} 