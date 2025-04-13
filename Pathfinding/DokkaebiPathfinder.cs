using UnityEngine;
using System.Collections.Generic;
using Pathfinding;
using Dokkaebi.Grid;
using Dokkaebi.Interfaces;
using Dokkaebi.Common;
using Dokkaebi.Utilities;

namespace Dokkaebi.Pathfinding
{
    /// <summary>
    /// Handles pathfinding operations using the A* Pathfinding Project
    /// </summary>
    [RequireComponent(typeof(Seeker))]
    public class DokkaebiPathfinder : MonoBehaviour
    {
        private Seeker seeker;
        private IGridSystem gridSystem;
        private IPathfindingGridInfo gridInfo;
        private IDokkaebiUnit unit;

        private void Awake()
        {
            // Get required components
            seeker = GetComponent<Seeker>();
            if (seeker == null)
            {
                SmartLogger.LogError("[DokkaebiPathfinder] No Seeker component found. Pathfinding will not work properly.", LogCategory.Pathfinding, this);
                enabled = false;
                return;
            }

            // Get the unit component
            unit = GetComponent<IDokkaebiUnit>();
            if (unit == null)
            {
                SmartLogger.LogError("[DokkaebiPathfinder] No IDokkaebiUnit implementation found on this GameObject.", LogCategory.Pathfinding, this);
                enabled = false;
                return;
            }

            // Get GridManager singleton instance
            GridManager gmInstance = GridManager.Instance;
            if (gmInstance == null)
            {
                SmartLogger.LogError("[DokkaebiPathfinder] Could not find GridManager.Instance in the scene. Ensure GridManager exists and is properly initialized.", LogCategory.Pathfinding, this);
                enabled = false;
                return;
            }

            // Cast singleton to required interfaces
            gridSystem = gmInstance as IGridSystem;
            gridInfo = gmInstance as IPathfindingGridInfo;

            if (gridSystem == null)
            {
                SmartLogger.LogError("[DokkaebiPathfinder] GridManager.Instance does not implement IGridSystem.", LogCategory.Pathfinding, this);
                enabled = false;
                return;
            }

            if (gridInfo == null)
            {
                SmartLogger.LogError("[DokkaebiPathfinder] GridManager.Instance does not implement IPathfindingGridInfo.", LogCategory.Pathfinding, this);
                enabled = false;
                return;
            }

            SmartLogger.Log("[DokkaebiPathfinder] Successfully initialized with all dependencies.", LogCategory.Pathfinding, this);
        }

        /// <summary>
        /// Get all walkable positions within the unit's movement range
        /// </summary>
        public List<GridPosition> GetWalkablePositionsInRange()
        {
            if (gridSystem == null || gridInfo == null || unit == null)
            {
                SmartLogger.LogError("GetWalkablePositionsInRange: Missing dependencies", LogCategory.Pathfinding, this);
                return new List<GridPosition>();
            }

            var validPositions = new List<GridPosition>();
            var startPos = unit.CurrentGridPosition;
            var movementRange = unit.MovementRange;

            // Get all positions within range
            for (int x = -movementRange; x <= movementRange; x++)
            {
                for (int z = -movementRange; z <= movementRange; z++)
                {
                    var checkPos = new GridPosition(startPos.x + x, startPos.z + z);

                    // Skip if position is out of grid bounds
                    if (!gridSystem.IsValidGridPosition(checkPos))
                        continue;

                    // Skip if position is not walkable
                    if (!gridInfo.IsWalkable(checkPos, unit))
                        continue;

                    // Skip if position is too far (using Manhattan distance for now)
                    int distance = Mathf.Abs(x) + Mathf.Abs(z);
                    if (distance > movementRange)
                        continue;

                    // Check if we can actually reach this position
                    if (CanReachPosition(checkPos))
                    {
                        validPositions.Add(checkPos);
                    }
                }
            }

            return validPositions;
        }

        /// <summary>
        /// Check if a position can be reached within the unit's movement range
        /// </summary>
        private bool CanReachPosition(GridPosition targetPos)
        {
            if (gridSystem == null || gridInfo == null)
                return false;

            // For now, just check if the position is walkable and within range
            // In the future, we might want to do actual pathfinding here
            return gridInfo.IsWalkable(targetPos, unit);
        }

        /// <summary>
        /// Get a path to the target position
        /// </summary>
        public void GetPath(GridPosition targetPos, System.Action<Path> callback)
        {
            if (seeker == null || gridSystem == null || gridInfo == null)
            {
                SmartLogger.LogError("GetPath: Missing dependencies", LogCategory.Pathfinding, this);
                return;
            }

            Vector3 startPos = transform.position;
            Vector3 endPos = gridSystem.GridToWorldPosition(targetPos);

            // Start pathfinding
            seeker.StartPath(startPos, endPos, (Path p) =>
            {
                if (p.error)
                {
                    SmartLogger.LogError($"Path calculation failed: {p.errorLog}", LogCategory.Pathfinding, this);
                }
                callback?.Invoke(p);
            });
        }
    }
} 
