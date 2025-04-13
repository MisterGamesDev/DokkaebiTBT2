using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using Dokkaebi.Utilities;
using Dokkaebi.Interfaces;
using Dokkaebi.Grid;
using Dokkaebi.Core;
using Dokkaebi.Common;
using Dokkaebi.Units;
using System.Linq;

namespace Dokkaebi.Pathfinding
{
    /// <summary>
    /// Handles pathfinding and movement for Dokkaebi units
    /// </summary>
    [RequireComponent(typeof(Seeker))]
    public class DokkaebiMovementHandler : MonoBehaviour, DokkaebiUpdateManager.IUpdateObserver
    {
        [Header("Dependencies")]
        private IDokkaebiUnit _unit;
        private ICoreUpdateService _coreUpdateService;
        
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float nextWaypointDistance = 0.1f;
        [SerializeField] private float pathUpdateInterval = 0.5f;
        
        // A* components
        private Seeker seeker;
        private Path currentPath;
        private IPathfindingGridInfo _gridInfo;
        private IGridSystem _gridSystem;
        
        // Path following variables
        private bool isMoving = false;
        private int currentWaypoint = 0;
        private float lastPathUpdateTime = 0f;
        
        // Events
        public event Action OnMoveComplete;
        public event Action<GridPosition> OnPositionChanged;
        
        // Movement target
        private GridPosition targetGridPosition;

        /// <summary>
        /// Calculates the cost of a path using Manhattan distance between consecutive points
        /// </summary>
        private int CalculatePathCost(List<Vector3> path)
        {
            int totalCost = 0;
            
            // Need at least 2 points to calculate movement cost
            if (path == null || path.Count < 2)
                return 0;
                
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 current = path[i];
                Vector3 next = path[i + 1];
                
                // Convert to grid positions
                GridPosition currentGrid = Common.GridConverter.WorldToGrid(current);
                GridPosition nextGrid = Common.GridConverter.WorldToGrid(next);
                
                // Add Manhattan distance between points
                totalCost += GridPosition.GetManhattanDistance(currentGrid, nextGrid);
            }
            
            return totalCost;
        }

        /// <summary>
        /// Truncates a path to only include waypoints that fit within the movement range budget using Manhattan distance
        /// </summary>
        private List<Vector3> TruncatePathToCost(List<Vector3> path, int maxCost)
        {
            List<Vector3> truncatedPath = new List<Vector3>();
            int currentCost = 0;
            
            // Always include the starting point
            if (path != null && path.Count > 0)
                truncatedPath.Add(path[0]);
                
            // Need at least 2 points to calculate movement
            if (path == null || path.Count < 2)
                return truncatedPath;
                
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 current = path[i];
                Vector3 next = path[i + 1];
                
                // Convert to grid positions
                GridPosition currentGrid = Common.GridConverter.WorldToGrid(current);
                GridPosition nextGrid = Common.GridConverter.WorldToGrid(next);
                
                // Calculate Manhattan distance for this segment
                int segmentCost = GridPosition.GetManhattanDistance(currentGrid, nextGrid);
                
                // Check if adding this segment would exceed the budget
                if (currentCost + segmentCost <= maxCost)
                {
                    truncatedPath.Add(next);
                    currentCost += segmentCost;
                }
                else
                {
                    // We've hit our cost limit
                    break;
                }
            }
            
            return truncatedPath;
        }

        /// <summary>
        /// Called when the path has been calculated
        /// </summary>
        private void OnPathComplete(Path p)
        {
            SmartLogger.Log($"[DokkaebiMovementHandler] OnPathComplete called for unit {gameObject.name}", LogCategory.Pathfinding, this);
            
            if (p.error)
            {
                SmartLogger.LogError($"[DokkaebiMovementHandler] Path calculation failed: {p.errorLog}", LogCategory.Pathfinding, this);
                return;
            }

            SmartLogger.Log($"[DokkaebiMovementHandler] Path calculation successful. Waypoints: {p.vectorPath.Count}", LogCategory.Pathfinding, this);
            
            // Store the path
            currentPath = p;
            currentWaypoint = 0;
            isMoving = true;
            
            // Start following the path
            SmartLogger.Log("[DokkaebiMovementHandler] Starting to follow path", LogCategory.Pathfinding, this);
            FollowPath(Time.deltaTime);
        }

        /// <summary>
        /// Get all valid grid positions that the unit can move to, using Manhattan distance
        /// </summary>
        public List<GridPosition> GetValidMovePositions()
        {
            List<GridPosition> validPositions = new List<GridPosition>();
            
            SmartLogger.Log($"[MovementHandler.GetValidMovePositions START] Checking dependencies for {gameObject.name}. _unit: {(_unit != null ? $"Name={_unit.GetUnitName()}, ID={_unit.UnitId}" : "NULL")}, _gridInfo: {(_gridInfo != null ? _gridInfo.GetType().Name : "NULL")}, _gridSystem: {(_gridSystem != null ? _gridSystem.GetType().Name : "NULL")}", LogCategory.Movement, this);
            
            // Ensure dependencies are available
            if (_gridInfo == null || _unit == null || _gridSystem == null)
            {
                SmartLogger.LogError($"[MovementHandler.GetValidMovePositions] Missing dependencies for {gameObject.name}! _unit: {(_unit != null)}, _gridInfo: {(_gridInfo != null)}, _gridSystem: {(_gridSystem != null)}. Component enabled: {enabled}", LogCategory.Movement, this);
                return validPositions;
            }

            GridPosition startPosition = _unit.CurrentGridPosition;
            int moveRange = _unit.MovementRange;

            // Calculate the bounding box for potential positions
            for (int x = startPosition.x - moveRange; x <= startPosition.x + moveRange; x++)
            {
                for (int z = startPosition.z - moveRange; z <= startPosition.z + moveRange; z++)
                {
                    GridPosition testPos = new GridPosition(x, z);

                    // Skip if it's the starting position
                    if (testPos == startPosition) continue;

                    // Skip if position is invalid
                    if (!_gridSystem.IsValidGridPosition(testPos)) continue;

                    // Calculate Manhattan distance
                    int distance = GridPosition.GetManhattanDistance(startPosition, testPos);

                    // Check if within range and walkable FOR THIS UNIT
                    if (distance <= moveRange && _gridInfo.IsWalkable(testPos, _unit))
                    {
                        validPositions.Add(testPos);
                    }
                }
            }

            return validPositions;
        }

        private void Awake()
        {
            SmartLogger.Log($"[MovementHandler AWAKE START] Initializing {gameObject.name}. Component enabled: {enabled}", LogCategory.Pathfinding, this);

            // Get unit reference
            _unit = GetComponent<IDokkaebiUnit>();
            SmartLogger.Log($"[MovementHandler AWAKE] Found IDokkaebiUnit: {(_unit != null)}. Unit details: {(_unit != null ? $"Name={_unit.GetUnitName()}, ID={_unit.UnitId}" : "NULL")}", LogCategory.Pathfinding, this);
            
            // Get GridManager instance and required interfaces
            var gridManager = GridManager.Instance;
            if (gridManager == null)
            {
                SmartLogger.LogError("[MovementHandler AWAKE] GridManager.Instance is null! Disabling component.", LogCategory.Pathfinding, this);
                enabled = false;
                return;
            }

            // Cast to required interfaces
            _gridSystem = gridManager;
            _gridInfo = gridManager;

            // Validate all dependencies
            if (_unit == null || _gridSystem == null || _gridInfo == null)
            {
                SmartLogger.LogError($"[MovementHandler AWAKE] Missing critical dependencies! Unit: {(_unit != null)}, GridSystem: {(_gridSystem != null)}, GridInfo: {(_gridInfo != null)}. Disabling component.", LogCategory.Pathfinding, this);
                enabled = false;
                return;
            }

            // Get seeker component
            seeker = GetComponent<Seeker>();
            if (seeker == null)
            {
                SmartLogger.LogError("[MovementHandler AWAKE] Required Seeker component not found! Disabling component.", LogCategory.Pathfinding, this);
                enabled = false;
                return;
            }

            SmartLogger.Log($"[MovementHandler AWAKE] Successfully initialized with Unit={_unit.GetUnitName()}, GridSystem={_gridSystem.GetType().Name}, GridInfo={_gridInfo.GetType().Name}", LogCategory.Pathfinding, this);
        }

        private string GetComponentHierarchyString()
        {
            var components = GetComponents<Component>();
            return string.Join(", ", System.Array.ConvertAll(components, c => c.GetType().Name));
        }

        private void Start()
        {
            SmartLogger.Log($"[MovementHandler START] Running for {gameObject.name}.", LogCategory.Pathfinding, this);

            _coreUpdateService = DokkaebiUpdateManager.Instance;
            if (_coreUpdateService != null)
            {
                // _coreUpdateService.RegisterUpdateObserver(this); // Commented out as requested
                SmartLogger.Log($"[MovementHandler START] Registration with UpdateManager skipped.", LogCategory.Pathfinding, this);
            }

            if (_unit != null)
            {
                GridPosition initialPos = Common.GridConverter.WorldToGrid(transform.position);
                _unit.SetGridPosition(initialPos);
            }

            // Force enable the component
            this.enabled = true;
            SmartLogger.Log($"[MovementHandler START] Explicitly ensuring component is enabled (this.enabled = {this.enabled}) at end of Start.", LogCategory.Pathfinding, this);
        }

        private void OnEnable()
        {
            SmartLogger.Log($"[MovementHandler.OnEnable] Component ENABLED on {gameObject.name}", LogCategory.Pathfinding, this);
        }

        private void OnDisable()
        {
            SmartLogger.Log($"[MovementHandler.OnDisable] Component DISABLED on {gameObject.name}. Reason: {GetDisableReason()}", LogCategory.Pathfinding, this);
        }

        private string GetDisableReason()
        {
            if (gameObject == null) return "GameObject is null/destroyed";
            if (!gameObject.activeSelf) return "GameObject set inactive";
            if (!enabled) return "Component disabled";
            return "Unknown";
        }

        private void Update()
        {
            if (!isMoving || currentPath == null || currentPath.vectorPath == null || currentPath.vectorPath.Count == 0 || currentWaypoint >= currentPath.vectorPath.Count)
            {
                return;
            }

            FollowPath(Time.deltaTime);
        }

        public void RequestPath(GridPosition targetPosition)
        {
            SmartLogger.Log($"[MovementHandler] RequestPath called for unit {gameObject.name} to position {targetPosition}", LogCategory.Pathfinding, this);
            
            if (_unit == null || _gridSystem == null)
            {
                SmartLogger.LogError("[MovementHandler] Cannot request path: Missing dependencies", LogCategory.Pathfinding, this);
                return;
            }

            if (seeker == null)
            {
                SmartLogger.LogError("[MovementHandler] Cannot request path: seeker is null", LogCategory.Pathfinding, this);
                return;
            }

            targetGridPosition = targetPosition;
            
            // Convert grid positions to world positions
            Vector3 startPos = _gridSystem.GridToWorldPosition(_unit.CurrentGridPosition);
            Vector3 endPos = _gridSystem.GridToWorldPosition(targetPosition);
            
            SmartLogger.Log($"[MovementHandler] Requesting path from {startPos} to {endPos}", LogCategory.Pathfinding, this);
            
            // Request the path
            seeker.StartPath(startPos, endPos, OnPathComplete);
        }

        private void FollowPath(float deltaTime)
        {
            SmartLogger.Log($"[MovementHandler.FollowPath] Tick. Current Waypoint: {currentWaypoint}/{currentPath?.vectorPath?.Count ?? 0}. isMoving={isMoving}", LogCategory.Pathfinding, this);
            
            if (currentPath == null || currentPath.vectorPath == null || currentPath.vectorPath.Count == 0)
            {
                SmartLogger.LogError("[DokkaebiMovementHandler] Cannot follow path: Path is null or empty", LogCategory.Pathfinding, this);
                return;
            }

            if (_unit == null)
            {
                SmartLogger.LogError("[DokkaebiMovementHandler] Cannot follow path: _unit is null", LogCategory.Pathfinding, this);
                return;
            }

            // Start movement coroutine
            SmartLogger.Log("[DokkaebiMovementHandler] Starting movement coroutine", LogCategory.Pathfinding, this);
            StartCoroutine(MoveAlongPath(deltaTime));
        }

        private IEnumerator MoveAlongPath(float deltaTime)
        {
            if (gameObject == null)
            {
                SmartLogger.LogError("[DokkaebiMovementHandler] Cannot move along path: GameObject is null or destroyed", LogCategory.Pathfinding, this);
                yield break;
            }

            SmartLogger.Log($"[DokkaebiMovementHandler] MoveAlongPath started for unit {gameObject.name}. Component enabled: {enabled}", LogCategory.Pathfinding, this);
            
            // Validate all critical dependencies
            if (_unit == null)
            {
                SmartLogger.LogError("[DokkaebiMovementHandler] Cannot move along path: _unit is null", LogCategory.Pathfinding, this);
                isMoving = false;
                yield break;
            }

            if (currentPath == null || currentPath.vectorPath == null || currentPath.vectorPath.Count == 0)
            {
                SmartLogger.LogError("[DokkaebiMovementHandler] Cannot move along path: Path is null or empty", LogCategory.Pathfinding, this);
                isMoving = false;
                yield break;
            }

            Transform unitTransform = transform;
            if (unitTransform == null)
            {
                SmartLogger.LogError("[DokkaebiMovementHandler] Cannot move along path: transform is null", LogCategory.Pathfinding, this);
                isMoving = false;
                yield break;
            }

            // Skip the first waypoint (current position)
            for (int i = 1; i < currentPath.vectorPath.Count; i++)
            {
                bool shouldBreak = false;
                string errorMessage = null;

                // Validate state at start of each waypoint
                if (!isMoving || currentPath == null || currentPath.vectorPath == null)
                {
                    SmartLogger.LogError($"[DokkaebiMovementHandler] Movement interrupted at waypoint {i}: isMoving={isMoving}, path validity changed", LogCategory.Pathfinding, this);
                    isMoving = false;
                    yield break;
                }

                if (gameObject == null || !gameObject.activeInHierarchy)
                {
                    SmartLogger.LogError($"[DokkaebiMovementHandler] GameObject destroyed or disabled during movement at waypoint {i}", LogCategory.Pathfinding, this);
                    isMoving = false;
                    yield break;
                }

                // Additional validation for unitTransform
                if (unitTransform == null)
                {
                    SmartLogger.LogError($"[DokkaebiMovementHandler] Transform became null during movement at waypoint {i}", LogCategory.Pathfinding, this);
                    isMoving = false;
                    yield break;
                }

                // Validate path index before accessing
                if (i >= currentPath.vectorPath.Count)
                {
                    SmartLogger.LogError($"[DokkaebiMovementHandler] Path index {i} out of bounds (Count: {currentPath.vectorPath.Count})", LogCategory.Pathfinding, this);
                    isMoving = false;
                    yield break;
                }

                Vector3 targetPosition;
                string unitName;
                try
                {
                    targetPosition = currentPath.vectorPath[i];
                    
                    // Add explicit null check for _unit before accessing it
                    if (_unit == null)
                    {
                        SmartLogger.LogError($"[DokkaebiMovementHandler] _unit became null unexpectedly before getting name at waypoint {i}. Aborting coroutine.", LogCategory.Pathfinding, this);
                        isMoving = false;
                        yield break;
                    }
                    
                    // Now we know _unit is not null, we can safely call GetUnitName
                    unitName = _unit.GetUnitName();
                }
                catch (Exception e)
                {
                    SmartLogger.LogError($"[DokkaebiMovementHandler] Error accessing path or unit data at waypoint {i}: {e.Message}\nStack: {e.StackTrace}", LogCategory.Pathfinding, this);
                    isMoving = false;
                    yield break;
                }

                SmartLogger.Log($"[DokkaebiMovementHandler] Moving to waypoint {i}/{currentPath.vectorPath.Count}: {targetPosition}. Unit: {unitName}", LogCategory.Pathfinding, this);

                // Move to the waypoint
                float elapsedTime = 0f;
                Vector3 startPosition = unitTransform.position;

                while (elapsedTime < pathUpdateInterval && !shouldBreak)
                {
                    // Add detailed diagnostic logging
                    SmartLogger.Log($"[DokkaebiMovementHandler] Movement Status Check - isMoving: {isMoving}, gameObject null: {gameObject == null}, gameObject active: {(gameObject != null ? gameObject.activeInHierarchy : false)}, Unit: {(_unit != null ? _unit.GetUnitName() : "null")}", LogCategory.Pathfinding, this);

                    // Check for critical failures first (null checks)
                    if (gameObject == null || !gameObject.activeInHierarchy)
                    {
                        SmartLogger.LogError($"[DokkaebiMovementHandler] Movement interrupted - GameObject destroyed or inactive during interpolation at waypoint {i}", LogCategory.Pathfinding, this);
                        isMoving = false;
                        yield break;
                    }

                    // Check for intentional movement stops
                    if (!isMoving)
                    {
                        SmartLogger.Log($"[DokkaebiMovementHandler] Movement stopped intentionally during interpolation at waypoint {i}", LogCategory.Pathfinding, this);
                        yield break;
                    }

                    // Check if unit still exists and is valid
                    if (_unit == null)
                    {
                        SmartLogger.LogError($"[DokkaebiMovementHandler] Movement interrupted - Unit reference lost during interpolation at waypoint {i}", LogCategory.Pathfinding, this);
                        isMoving = false;
                        yield break;
                    }

                    // Additional validation for unitTransform during interpolation
                    if (unitTransform == null)
                    {
                        SmartLogger.LogError($"[DokkaebiMovementHandler] Transform became null during interpolation at waypoint {i}", LogCategory.Pathfinding, this);
                        isMoving = false;
                        yield break;
                    }

                    try
                    {
                        elapsedTime += Time.deltaTime;
                        float t = elapsedTime / pathUpdateInterval;
                        unitTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
                    }
                    catch (Exception e)
                    {
                        errorMessage = $"[DokkaebiMovementHandler] Error during position interpolation at waypoint {i}: {e.Message}\nStack: {e.StackTrace}";
                        shouldBreak = true;
                        break;
                    }

                    yield return null;
                }

                if (shouldBreak)
                {
                    SmartLogger.LogError(errorMessage, LogCategory.Pathfinding, this);
                    isMoving = false;
                    yield break;
                }

                // Ensure we reach the exact position
                if (unitTransform != null)
                {
                    try
                    {
                        unitTransform.position = targetPosition;
                        SmartLogger.Log($"[DokkaebiMovementHandler] Reached waypoint {i}: {targetPosition}", LogCategory.Pathfinding, this);
                    }
                    catch (Exception e)
                    {
                        SmartLogger.LogError($"[DokkaebiMovementHandler] Error setting final position at waypoint {i}: {e.Message}\nStack: {e.StackTrace}", LogCategory.Pathfinding, this);
                        isMoving = false;
                        yield break;
                    }
                }
                else
                {
                    SmartLogger.LogError($"[DokkaebiMovementHandler] Transform became null while finalizing waypoint {i}", LogCategory.Pathfinding, this);
                    isMoving = false;
                    yield break;
                }
            }

            // Movement complete
            if (gameObject != null && gameObject.activeInHierarchy)
            {
                SmartLogger.Log($"[DokkaebiMovementHandler] Movement complete for unit {gameObject.name}", LogCategory.Pathfinding, this);
                CompleteMovement();
            }
            else
            {
                SmartLogger.LogError("[DokkaebiMovementHandler] Cannot complete movement: GameObject is null or inactive", LogCategory.Pathfinding, this);
                isMoving = false;
            }
        }

        private void CompleteMovement()
        {
            SmartLogger.Log($"[MovementHandler] CompleteMovement ENTRY for {gameObject.name}", LogCategory.Pathfinding, this);
            
            if (!isMoving || _gridSystem == null)
            {
                SmartLogger.Log($"[MovementHandler] CompleteMovement EXIT - Not moving or missing dependencies", LogCategory.Pathfinding, this);
                return;
            }
                
            isMoving = false;
            currentPath = null;
            
            // Ensure unit position is exactly at target grid position
            Vector3 finalWorldPos = _gridSystem.GridToWorldPosition(targetGridPosition);

            SmartLogger.Log($"[MovementHandler] CompleteMovement: Snapping to targetGridPosition={targetGridPosition}, Calculated World Pos={finalWorldPos}. Current Transform Pos={transform.position}", LogCategory.Pathfinding, this);
            transform.position = finalWorldPos;
            _unit.SetGridPosition(targetGridPosition);
            
            // Set the unit's hasMovedThisTurn flag
            if (_unit is DokkaebiUnit dokkaebiUnit)
            {
                dokkaebiUnit.SetHasMoved(true);
            }
            
            // Notify listeners that movement is complete
            OnMoveComplete?.Invoke();
            
            SmartLogger.Log($"Movement complete for {_unit.GetUnitName()} to {targetGridPosition}", LogCategory.Movement);
            
            SmartLogger.Log($"[MovementHandler] CompleteMovement EXIT for {gameObject.name}", LogCategory.Pathfinding, this);
        }

        public void StopMovement()
        {
            SmartLogger.Log($"[MovementHandler] StopMovement ENTRY for {gameObject.name}", LogCategory.Pathfinding, this);
            
            isMoving = false;
            currentPath = null;
            currentWaypoint = 0;

            // Update the unit's grid position to match current world position
            GridPosition currentGridPos = Common.GridConverter.WorldToGrid(transform.position);
            _unit.SetGridPosition(currentGridPos);

            SmartLogger.Log($"Movement stopped for {_unit?.GetUnitName()}", LogCategory.Movement);
            
            SmartLogger.Log($"[MovementHandler] StopMovement EXIT for {gameObject.name}", LogCategory.Pathfinding, this);
        }

        /// <summary>
        /// Directly updates the unit's position without pathfinding
        /// </summary>
        public void UpdatePosition(GridPosition position)
        {
            if (_gridSystem == null)
            {
                SmartLogger.LogError("UpdatePosition: Missing GridSystem reference!", LogCategory.Movement);
                return;
            }

            // Convert position to world coordinates
            Vector3 worldPosition = _gridSystem.GridToWorldPosition(position);
            worldPosition.y = transform.position.y; // Maintain current height
            
            // Update transform position
            transform.position = worldPosition;
            
            // Notify listeners of position change
            OnPositionChanged?.Invoke(position);
            
            SmartLogger.Log($"Unit position directly updated to {position}", LogCategory.Movement);
        }

        public void SetGridInfoProvider(IPathfindingGridInfo gridInfoProvider)
        {
            this._gridInfo = gridInfoProvider;
            if (this._gridInfo == null) {
                SmartLogger.LogError($"[{gameObject.name}] SetGridInfoProvider was called with NULL!", LogCategory.Pathfinding, this);
            } else {
                SmartLogger.Log($"[{gameObject.name}] SetGridInfoProvider successfully assigned Grid Info.", LogCategory.Pathfinding, this);
            }
        }

        public void CustomUpdate(float deltaTime)
        {
            // Empty for now since we're using standard Update
        }

        /// <summary>
        /// Moves the unit directly to a world position without pathfinding
        /// </summary>
        public void StartSimpleMove(Vector3 worldTargetPosition)
        {
            SmartLogger.Log($"[MovementHandler.StartSimpleMove] ENTRY on {gameObject.name}. Target: {worldTargetPosition}. Current Position: {transform.position}", LogCategory.Pathfinding, this);
            
            if (isMoving)
            {
                SmartLogger.Log($"[MovementHandler.StartSimpleMove] Unit {gameObject.name} is already moving. Stopping current movement first.", LogCategory.Pathfinding, this);
                StopMovement();
            }
            
            this.isMoving = true;
            this.currentPath = null;
            this.currentWaypoint = 0;
            
            SmartLogger.Log($"[MovementHandler.StartSimpleMove] Starting SimpleMovementCoroutine for {gameObject.name}", LogCategory.Pathfinding, this);
            StartCoroutine(SimpleMovementCoroutine(worldTargetPosition));
        }

        private IEnumerator SimpleMovementCoroutine(Vector3 targetPosition)
        {
            SmartLogger.Log($"[MovementHandler.SimpleMovementCoroutine] ENTRY on {gameObject.name}. Target: {targetPosition}, Start Position: {transform.position}", LogCategory.Pathfinding, this);
            float startTime = Time.time;
            Vector3 startPosition = transform.position;
            float journeyLength = Vector3.Distance(startPosition, targetPosition);
            
            while (Vector3.Distance(transform.position, targetPosition) > nextWaypointDistance)
            {
                float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
                SmartLogger.Log($"[MovementHandler.SimpleMovementCoroutine] Moving {gameObject.name}. Distance to target: {distanceToTarget:F2}. Current Pos: {transform.position}", LogCategory.Pathfinding, this);
                
                // Calculate movement
                Vector3 direction = (targetPosition - transform.position).normalized;
                Vector3 newPosition = transform.position + direction * moveSpeed * Time.deltaTime;
                
                // Update position
                transform.position = newPosition;
                
                // Update rotation to face movement direction
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                }
                
                yield return null;
            }
            
            // Snap to final position
            transform.position = targetPosition;
            float totalTime = Time.time - startTime;
            
            SmartLogger.Log($"[MovementHandler.SimpleMovementCoroutine] Movement complete for {gameObject.name}. Journey Length: {journeyLength:F2}, Time Taken: {totalTime:F2}s", LogCategory.Pathfinding, this);
            SmartLogger.Log($"[MovementHandler.SimpleMovementCoroutine] Final Position: {transform.position}, Target Was: {targetPosition}", LogCategory.Pathfinding, this);
            
            // Update grid position
            GridPosition newGridPos = Common.GridConverter.WorldToGrid(targetPosition);
            if (_unit != null)
            {
                SmartLogger.Log($"[MovementHandler.SimpleMovementCoroutine] Updating grid position for {gameObject.name} to {newGridPos}", LogCategory.Pathfinding, this);
                _unit.SetGridPosition(newGridPos);
                OnPositionChanged?.Invoke(newGridPos);
            }
            else
            {
                SmartLogger.LogError($"[MovementHandler.SimpleMovementCoroutine] No IDokkaebiUnit found on {gameObject.name}, cannot update grid position!", LogCategory.Pathfinding, this);
            }
            
            isMoving = false;
            OnMoveComplete?.Invoke();
            SmartLogger.Log($"[MovementHandler.SimpleMovementCoroutine] Movement sequence completed for {gameObject.name}. IsMoving set to false.", LogCategory.Pathfinding, this);
        }
    }
} 