using UnityEngine;
using Pathfinding;
using Dokkaebi.Interfaces;
using Dokkaebi.Utilities;

namespace Dokkaebi.Grid
{
    /// <summary>
    /// Handles conversions between different coordinate systems, particularly A* pathfinding related.
    /// Basic conversions have been moved to Dokkaebi.Utilities.GridPositionConverter.
    /// </summary>
    [System.Obsolete("This class creates direct dependencies between Grid and Pathfinding. Use GridNodeUtility and interfaces instead.")]
    public static class DokkaebiGridConverter
    {
        /// <summary>
        /// Gets the closest graph node to a world position
        /// </summary>
        [System.Obsolete("Use PathfindingNodeProvider.GetNodeFromWorldPosition instead")]
        public static GraphNode WorldToNode(Vector3 worldPosition)
        {
            // Get the nearest node from the active A* path instance
            NNInfo info = AstarPath.active.GetNearest(worldPosition);
            return info.node;
        }

        /// <summary>
        /// Gets the closest graph node to a grid position
        /// </summary>
        [System.Obsolete("Use PathfindingNodeProvider.GetNodeFromGridPosition instead")]
        public static GraphNode GridToNode(GridPosition gridPosition)
        {
            // Convert from Grid.GridPosition to world position
            Vector3 worldPosition = GridToWorld(gridPosition);
            return WorldToNode(worldPosition);
        }

        /// <summary>
        /// Gets the world position of a graph node
        /// </summary>
        [System.Obsolete("Use IGraphNode.Position instead")]
        public static Vector3 NodeToWorld(GraphNode node)
        {
            return (Vector3)node.position;
        }

        /// <summary>
        /// Converts a graph node to the nearest grid position
        /// </summary>
        [System.Obsolete("Use GridConverter.WorldToGrid with IGraphNode.Position instead")]
        public static GridPosition NodeToGrid(GraphNode node)
        {
            Vector3 worldPos = (Vector3)node.position;
            return WorldToGrid(worldPos);
        }

        /// <summary>
        /// Checks if a GridPosition is walkable according to the pathfinding graph
        /// </summary>
        [System.Obsolete("Use GridNodeUtility.IsWalkable instead")]
        public static bool IsWalkable(GridPosition gridPosition)
        {
            GraphNode node = GridToNode(gridPosition);
            return node != null && node.Walkable;
        }

        #region Convenience wrapper methods to maintain compatibility

        /// <summary>
        /// Convert a grid position to a world position - wrapper around Utilities.GridPositionConverter
        /// </summary>
        [System.Obsolete("Use Utilities.GridPositionConverter.GridToWorld instead")]
        public static Vector3 GridToWorld(GridPosition gridPosition, float height = -1f)
        {
            // Convert grid position to world position
            return Utilities.GridPositionConverter.GridToWorld(gridPosition, height);
        }

        /// <summary>
        /// Convert a world position to a grid position - wrapper around Utilities.GridPositionConverter
        /// </summary>
        [System.Obsolete("Use Utilities.GridPositionConverter.WorldToGrid instead")]
        public static GridPosition WorldToGrid(Vector3 worldPosition)
        {
            return Utilities.GridPositionConverter.WorldToGrid(worldPosition);
        }

        /// <summary>
        /// Convert a Vector2Int to a GridPosition - wrapper around GridPosition.FromVector2Int
        /// </summary>
        [System.Obsolete("Use GridPosition.FromVector2Int instead")]
        public static GridPosition Vector2IntToGrid(Vector2Int vector)
        {
            return GridPosition.FromVector2Int(vector);
        }

        /// <summary>
        /// Convert a GridPosition to a Vector2Int - wrapper around GridPosition.ToVector2Int
        /// </summary>
        [System.Obsolete("Use GridPosition.ToVector2Int instead")]
        public static Vector2Int GridToVector2Int(GridPosition gridPosition)
        {
            // Create a Vector2Int directly, with Z mapped to Y
            return gridPosition.ToVector2Int();
        }

        /// <summary>
        /// Gets the distance between two grid positions - wrapper around GridPosition.GetManhattanDistance
        /// </summary>
        [System.Obsolete("Use GridPosition.GetManhattanDistance instead")]
        public static int GetGridDistance(GridPosition a, GridPosition b)
        {
            // Calculate locally to avoid unnecessary conversion
            return GridPosition.GetManhattanDistance(a, b);
        }
        
        /// <summary>
        /// Get the world height at a given world position.
        /// In a terrain-based game, this would query the terrain height.
        /// </summary>
        [System.Obsolete("Move this functionality to a TerrainManager in a higher-level namespace")]
        public static float GetWorldHeight(Vector3 worldPosition)
        {
            // For now, we'll use a simple implementation
            // In a real implementation, this might raycast or query terrain
            return 0f; // Default grid height
        }
        
        /// <summary>
        /// Get the appropriate cell height for a grid position.
        /// </summary>
        [System.Obsolete("Move this functionality to a TerrainManager in a higher-level namespace")]
        public static float GetCellHeight(GridPosition gridPosition)
        {
            // For future terrain integration - could query terrain height system
            return 0f; // Default grid height
        }

        #endregion
    }
} 