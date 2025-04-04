using UnityEngine;
using Pathfinding;
using Dokkaebi.Common;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Pathfinding
{
    /// <summary>
    /// Adapter that wraps A* GraphNode to implement the IGraphNode interface.
    /// This allows higher-level code to work with the abstraction rather than
    /// the concrete A* implementation.
    /// </summary>
    public class GraphNodeAdapter : IGraphNode
    {
        private readonly GraphNode _node;
        
        public GraphNodeAdapter(GraphNode node)
        {
            _node = node;
        }
        
        /// <summary>
        /// Whether this node is walkable
        /// </summary>
        public bool Walkable => _node != null && _node.Walkable;
        
        /// <summary>
        /// Position of the node in world space
        /// </summary>
        public Vector3 Position => (Vector3)_node.position;
        
        /// <summary>
        /// Get the underlying A* GraphNode
        /// </summary>
        public GraphNode UnderlyingNode => _node;
        
        /// <summary>
        /// Create an adapter from a GraphNode, or null if the input is null
        /// </summary>
        public static GraphNodeAdapter FromGraphNode(GraphNode node)
        {
            return node != null ? new GraphNodeAdapter(node) : null;
        }

        /// <summary>
        /// Get the penalty for traversing this node
        /// </summary>
        public int Penalty => _node != null ? (int)_node.Penalty : 0;

        /// <summary>
        /// Check if this node has a connection to another node
        /// </summary>
        public bool HasConnectionTo(IGraphNode other)
        {
            if (_node == null || other == null || !(other is GraphNodeAdapter adapter))
                return false;

            return _node.ContainsConnection(adapter.UnderlyingNode);
        }
    }
} 