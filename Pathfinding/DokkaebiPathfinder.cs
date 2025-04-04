using UnityEngine;
using System.Collections.Generic;
using Pathfinding;
using Dokkaebi.Common;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Pathfinding
{
    /// <summary>
    /// Implementation of IPathfinder using the A* Pathfinding Project
    /// </summary>
    public class DokkaebiPathfinder : MonoBehaviour, IPathfinder
    {
        private static DokkaebiPathfinder instance;
        public static DokkaebiPathfinder Instance => instance;

        private AstarPath astarPath;
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
            DontDestroyOnLoad(gameObject);

            // Get or create the AstarPath component
            astarPath = GetComponent<AstarPath>();
            if (astarPath == null)
            {
                astarPath = FindObjectOfType<AstarPath>();
                if (astarPath == null)
                {
                    Debug.LogWarning("No AstarPath component found. Pathfinding will not work properly.");
                }
            }
        }

        private void Start()
        {
            // Find and cache the IGridSystem using MonoBehaviours
            MonoBehaviour[] allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
            
            // Find IGridSystem implementation
            gridSystem = null;
            foreach (var behaviour in allMonoBehaviours)
            {
                gridSystem = behaviour as IGridSystem;
                if (gridSystem != null)
                    break;
            }
            
            if (gridSystem == null)
            {
                Debug.LogError("DokkaebiPathfinder: No IGridSystem implementation found in the scene.");
            }
            
            // Find IPathfindingGridInfo implementation
            gridInfo = null;
            foreach (var behaviour in allMonoBehaviours)
            {
                gridInfo = behaviour as IPathfindingGridInfo;
                if (gridInfo != null)
                    break;
            }
            
            if (gridInfo == null)
            {
                Debug.LogError("DokkaebiPathfinder: No IPathfindingGridInfo implementation found in the scene.");
            }
        }

        /// <summary>
        /// Check if a path exists between two grid positions
        /// </summary>
        public bool PathExists(Interfaces.GridPosition start, Interfaces.GridPosition end)
        {
            // Convert to Grid.GridPosition for internal use
            Vector2Int startPos = new Vector2Int(start.x, start.z);
            Vector2Int endPos = new Vector2Int(end.x, end.z);

            // Check if both positions are walkable using IPathfindingGridInfo
            if (!gridInfo.IsWalkable(startPos) || !gridInfo.IsWalkable(endPos))
            {
                return false;
            }

            // Convert to A* nodes
            GraphNode startNode = NodeFromGridPosition(startPos);
            GraphNode endNode = NodeFromGridPosition(endPos);

            if (startNode == null || endNode == null)
                return false;

            // Check path existence
            return PathUtilities.IsPathPossible(startNode, endNode);
        }

        // Helper method to get A* GraphNode from Vector2Int grid position
        private GraphNode NodeFromGridPosition(Vector2Int gridPos)
        {
            // Convert grid position to world position
            Vector3 worldPos = gridSystem.GridToWorldPosition(new Interfaces.GridPosition(gridPos.x, gridPos.y));
            
            // Get the nearest node from the graph
            NNInfo info = AstarPath.active.GetNearest(worldPos);
            return info.node;
        }

        /// <summary>
        /// Check if a position is walkable
        /// </summary>
        public bool IsWalkable(Interfaces.GridPosition position)
        {
            return gridInfo.IsWalkable(new Vector2Int(position.x, position.z));
        }

        /// <summary>
        /// Find all walkable positions within a certain range
        /// </summary>
        public List<Interfaces.GridPosition> GetWalkablePositionsInRange(Interfaces.GridPosition start, int range)
        {
            List<Interfaces.GridPosition> result = new List<Interfaces.GridPosition>();
            
            if (gridSystem == null || gridInfo == null)
            {
                Debug.LogError("GetWalkablePositionsInRange: Missing dependencies");
                return result;
            }
            
            // Get initial walkable position
            Vector2Int startPosV2 = new Vector2Int(start.x, start.z);
            
            // Create a queue for breadth-first search
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            
            queue.Enqueue(startPosV2);
            visited.Add(startPosV2);
            
            // Dictionary to track distance from start
            Dictionary<Vector2Int, int> distances = new Dictionary<Vector2Int, int>();
            distances[startPosV2] = 0;
            
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                int currentDistance = distances[current];
                
                // Add to result
                result.Add(new Interfaces.GridPosition(current.x, current.y));
                
                // Stop expanding if we've reached our range
                if (currentDistance >= range)
                    continue;
                
                // Get neighbors using IPathfindingGridInfo
                foreach (Vector2Int neighbor in gridInfo.GetWalkableNeighbours(current))
                {
                    if (!visited.Contains(neighbor))
                    {
                        queue.Enqueue(neighbor);
                        visited.Add(neighbor);
                        distances[neighbor] = currentDistance + 1;
                    }
                }
            }
            
            return result;
        }

        /// <summary>
        /// Get the path between two grid positions
        /// </summary>
        public List<Interfaces.GridPosition> GetPath(Interfaces.GridPosition start, Interfaces.GridPosition end)
        {
            List<Interfaces.GridPosition> path = new List<Interfaces.GridPosition>();
            
            if (gridSystem == null || gridInfo == null)
            {
                Debug.LogError("GetPath: Missing dependencies");
                return path;
            }

            // Convert start/end positions to Vector2Int for IPathfindingGridInfo
            Vector2Int startPosV2 = new Vector2Int(start.x, start.z);
            Vector2Int endPosV2 = new Vector2Int(end.x, end.z);
            
            // Check walkability
            if (!gridInfo.IsWalkable(startPosV2) || !gridInfo.IsWalkable(endPosV2))
            {
                return path;
            }

            // Convert to world positions using IGridSystem for A* integration
            Vector3 startPos = gridSystem.GridToWorldPosition(start);
            Vector3 endPos = gridSystem.GridToWorldPosition(end);

            // Use A* to get path
            ABPath abPath = ABPath.Construct(startPos, endPos);
            AstarPath.StartPath(abPath);
            abPath.BlockUntilCalculated();

            // Convert waypoints to grid positions
            if (abPath.error || abPath.vectorPath.Count == 0)
                return path;

            foreach (var point in abPath.vectorPath)
            {
                Interfaces.GridPosition gridPos = gridSystem.WorldToGridPosition(point);
                
                // Avoid duplicates
                if (path.Count == 0 || !path[path.Count - 1].Equals(gridPos))
                {
                    path.Add(gridPos);
                }
            }

            return path;
        }

        /// <summary>
        /// Get the distance between two grid positions considering pathfinding
        /// </summary>
        public int GetPathDistance(Interfaces.GridPosition start, Interfaces.GridPosition end)
        {
            var path = GetPath(start, end);
            // Return -1 if no path exists, otherwise return path length - 1 (number of moves needed)
            return path.Count == 0 ? -1 : path.Count - 1;
        }
    }
} 