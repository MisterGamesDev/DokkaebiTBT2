using UnityEngine;
using System.Collections.Generic;
using Dokkaebi.Common;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Grid
{
    /// <summary>
    /// Provides grid-based node utilities without depending directly on pathfinding
    /// </summary>
    public static class GridNodeUtility
    {
        // Cached reference to node provider (injected by dependency)
        private static INodeProvider _nodeProvider;
        
        /// <summary>
        /// Initialize with a node provider (called from higher-level assembly)
        /// </summary>
        public static void Initialize(INodeProvider nodeProvider)
        {
            _nodeProvider = nodeProvider;
        }
        
        /// <summary>
        /// Get a node from a grid position
        /// </summary>
        public static IGraphNode GetNode(GridPosition gridPosition)
        {
            if (_nodeProvider == null)
            {
                Debug.LogError("NodeProvider not initialized in GridNodeUtility");
                return null;
            }
            
            return _nodeProvider.GetNodeFromGridPosition(
                new Interfaces.GridPosition(gridPosition.x, gridPosition.z));
        }
        
        /// <summary>
        /// Check if a grid position is walkable
        /// </summary>
        public static bool IsWalkable(GridPosition gridPosition)
        {
            if (_nodeProvider == null)
            {
                Debug.LogError("NodeProvider not initialized in GridNodeUtility");
                return false;
            }
            
            return _nodeProvider.IsWalkable(
                new Interfaces.GridPosition(gridPosition.x, gridPosition.z));
        }
        
        /// <summary>
        /// Convert from Grid.GridPosition to Interfaces.GridPosition
        /// </summary>
        public static Interfaces.GridPosition ToInterfaceGridPosition(GridPosition gridPosition)
        {
            return new Interfaces.GridPosition(gridPosition.x, gridPosition.z);
        }
        
        /// <summary>
        /// Convert from Interfaces.GridPosition to Grid.GridPosition
        /// </summary>
        public static GridPosition FromInterfaceGridPosition(Interfaces.GridPosition gridPosition)
        {
            return new GridPosition(gridPosition.x, gridPosition.z);
        }
    }
} 
