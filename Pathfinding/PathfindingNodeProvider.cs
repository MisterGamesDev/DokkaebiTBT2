using UnityEngine;
using Pathfinding;
using Dokkaebi.Common;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Pathfinding
{
    /// <summary>
    /// Implementation of INodeProvider that uses A* Pathfinding
    /// </summary>
    public class PathfindingNodeProvider : MonoBehaviour, INodeProvider
    {
        private static PathfindingNodeProvider instance;
        public static PathfindingNodeProvider Instance => instance;
        
        private IGridSystem gridSystem;
        private IPathfindingGridInfo gridInfo;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }
        
        private void Start()
{
    // Find MonoBehaviours to check for interface implementations
    MonoBehaviour[] allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
    
    // Find and cache the IGridSystem
    gridSystem = null;
    foreach (var behaviour in allMonoBehaviours)
    {
        gridSystem = behaviour as IGridSystem;
        if (gridSystem != null)
            break;
    }
    
    if (gridSystem == null)
    {
        Debug.LogError("PathfindingNodeProvider: No IGridSystem implementation found in the scene.");
    }
    
    // Find and cache the IPathfindingGridInfo
    gridInfo = null;
    foreach (var behaviour in allMonoBehaviours)
    {
        gridInfo = behaviour as IPathfindingGridInfo;
        if (gridInfo != null)
            break;
    }
    
    if (gridInfo == null)
    {
        Debug.LogError("PathfindingNodeProvider: No IPathfindingGridInfo implementation found in the scene.");
    }
}

        /// <summary>
        /// Gets a node from a world position
        /// </summary>
        public IGraphNode GetNodeFromWorldPosition(Vector3 worldPosition)
        {
            // Get the nearest node from the active A* path instance
            NNInfo info = AstarPath.active.GetNearest(worldPosition);
            return GraphNodeAdapter.FromGraphNode(info.node);
        }
        
        /// <summary>
        /// Gets a node from a grid position
        /// </summary>
        public IGraphNode GetNodeFromGridPosition(Interfaces.GridPosition gridPosition)
        {
            // Convert grid position to world position
            Vector3 worldPosition = gridSystem.GridToWorldPosition(gridPosition);
            return GetNodeFromWorldPosition(worldPosition);
        }
        
        /// <summary>
        /// Checks if a grid position is walkable
        /// </summary>
        public bool IsWalkable(Interfaces.GridPosition gridPosition)
        {
            // Use IPathfindingGridInfo to check walkability
            return gridInfo.IsWalkable(new Vector2Int(gridPosition.x, gridPosition.z));
        }
    }
} 