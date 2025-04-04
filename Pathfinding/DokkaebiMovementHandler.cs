using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using Dokkaebi.Utilities;
using Dokkaebi.Interfaces;
using Dokkaebi.Grid;
using Dokkaebi.Core;

namespace Dokkaebi.Pathfinding
{
    /// <summary>
    /// Handles pathfinding and movement for Dokkaebi units
    /// </summary>
    [RequireComponent(typeof(Seeker))]
    public class DokkaebiMovementHandler : MonoBehaviour, DokkaebiUpdateManager.IUpdateObserver
    {
        [Header("References")]
        private IDokkaebiUnit _unit;
        private ICoreUpdateService _coreUpdateService;
        [SerializeField] private GridManager gridInfoProvider; // Reference to the GridManager in inspector
        
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float nextWaypointDistance = 0.1f;
        [SerializeField] private float pathUpdateInterval = 0.5f;
        
        // A* components
        private Seeker seeker;
        private Path currentPath;
        private IPathfindingGridInfo _gridInfo;
        
        // Path following variables
        private bool isMoving = false;
        private int currentWaypoint = 0;
        private float lastPathUpdateTime = 0f;
        
        // Events
        public event Action OnMoveComplete;
        public event Action<GridPosition> OnPositionChanged;
        
        // Movement target
        private GridPosition targetGridPosition;
        
        private void Awake()
        {
            // Get required components
            seeker = GetComponent<Seeker>();
            
            _unit = GetComponent<IDokkaebiUnit>();
            if (_unit == null)
            {
                SmartLogger.LogError($"DokkaebiMovementHandler requires a component implementing IDokkaebiUnit", LogCategory.Movement);
            }
            
            // Get grid info from the assigned provider or find it
            if (gridInfoProvider != null)
            {
                _gridInfo = gridInfoProvider as IPathfindingGridInfo;
            }
            
            // If not assigned or not valid, try to find GridManager
            if (_gridInfo == null)
            {
                var gridManager = FindObjectOfType<GridManager>();
                if (gridManager != null)
                {
                    _gridInfo = gridManager;
                    gridInfoProvider = gridManager;
                    SmartLogger.Log("Found and assigned GridManager automatically.", LogCategory.Movement);
                }
                else
                {
                    SmartLogger.LogError("IPathfindingGridInfo implementation not found! Assign the GridManager in inspector or ensure it exists in the scene.", LogCategory.Movement);
                }
            }
        }
        
        private void Start()
        {
            Debug.Log($"[MovementHandler] Start() called for {gameObject.name} (InstanceID: {gameObject.GetInstanceID()})."); // Keep this log

            // --- Initialize Core Update Service ---
            _coreUpdateService = DokkaebiUpdateManager.Instance; // Use the Singleton Instance

            // Final check and registration
            if (_coreUpdateService != null)
            {
                Debug.Log($"[MovementHandler] Attempting to register {gameObject.name} (InstanceID: {gameObject.GetInstanceID()}) with {_coreUpdateService.GetType().Name} (Instance ID: {(_coreUpdateService as MonoBehaviour)?.gameObject.GetInstanceID()}).");
                _coreUpdateService.RegisterUpdateObserver(this);
                Debug.Log($"[MovementHandler] Registration call completed for {gameObject.name}.");
            }
            else
            {
                // Log error only if Singleton Instance is null (shouldn't happen if manager exists)
                Debug.LogError($"[MovementHandler] FAILED to find DokkaebiUpdateManager.Instance for {gameObject.name}! CustomUpdate will not be called.");
            }
            // --- End Initialize Core Update Service ---


            // --- Initialize Unit Position ---
            if (_unit != null)
            {
                 // Ensure correct GridPositionConverter is used if moved
                GridPosition initialPos = Dokkaebi.Utilities.GridPositionConverter.WorldToGrid(transform.position); // Or Common.GridConverter
                _unit.SetGridPosition(initialPos);
            }
        }
        
        private void OnDestroy()
        {
            // Unregister from update manager
            if (_coreUpdateService != null)
            {
                _coreUpdateService?.UnregisterUpdateObserver(this);
            }
        }
        
        public void CustomUpdate(float deltaTime)
        {
            // Keep the existing CustomUpdate logic here
            //Debug.Log($"[MovementHandler] CustomUpdate TICK for {gameObject.name}. isMoving={isMoving}, currentPath?={(currentPath != null)}");
            if (!isMoving || currentPath == null)
                return;
            FollowPath(deltaTime);
        }

        /// <summary>
        /// Request a path to the target grid position
        /// </summary>
        public void RequestPath(GridPosition targetPosition)
{
    // ADD THIS: If already moving, stop the current movement first
    if (isMoving)
    {
        SmartLogger.Log($"Unit {_unit?.GetUnitName()} is already moving. Cancelling previous path to start new request to {targetPosition}.", LogCategory.Movement);
        StopMovement(); // Call your existing StopMovement method
        // Ensure StopMovement properly resets isMoving, currentPath etc.
    }

    targetGridPosition = targetPosition;
    Vector3 startPos = transform.position;
    // Use the corrected GridConverter call from the previous step
    Vector3 endPos = Dokkaebi.Common.GridConverter.GridToWorld(targetPosition);
    Debug.Log($"[MovementHandler] RequestPath: Received targetPosition (GridPosition)={targetPosition}, Calculated A* Target World Pos (endPos)={endPos}");

    SmartLogger.Log($"Requesting path from {GridPositionConverter.WorldToGrid(startPos)} to {targetPosition}", LogCategory.Movement);

    // Important: Make sure StopMovement runs *before* seeker.StartPath if StartPath is immediate.
    // If StartPath takes time or StopMovement needs a frame, you might need a small delay or coroutine.
    // For simplicity, assuming StopMovement is immediate for now:
    seeker.StartPath(startPos, endPos, OnPathComplete);
}
        
        /// <summary>
        /// Called when the path has been calculated
        /// </summary>
        private void OnPathComplete(Path p)
        {
            if (p.error)
            {
                SmartLogger.LogError($"Path error: {p.errorLog}", LogCategory.Movement);
                return;
            }
            
            // Store the path
            currentPath = p;
            currentWaypoint = 0;
            isMoving = true;
            
            // Log path info
            SmartLogger.Log($"Path found with {p.vectorPath.Count} waypoints", LogCategory.Movement);
        }
        
        /// <summary>
        /// Follow the current path each frame
        /// </summary>
        private void FollowPath(float deltaTime)
        {
            if (currentPath == null || currentWaypoint >= currentPath.vectorPath.Count)
            {
                CompleteMovement();
                return;
            }
            
            // Get direction to next waypoint
            Vector3 targetPoint = currentPath.vectorPath[currentWaypoint];
            Vector3 direction = (targetPoint - transform.position).normalized;
            
            // Move towards waypoint
            transform.position += direction * moveSpeed * deltaTime;
            
            // Rotate towards movement direction
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * deltaTime);
            }
            
            // Check if we reached the waypoint
            float distanceToWaypoint = Vector3.Distance(transform.position, targetPoint);
            if (distanceToWaypoint < nextWaypointDistance)
            {
                currentWaypoint++;
                
                // Update unit's grid position
                GridPosition newGridPos = GridPositionConverter.WorldToGrid(transform.position);
                if (!_unit.CurrentGridPosition.Equals(newGridPos))
                {
                    _unit.SetGridPosition(newGridPos);
                    OnPositionChanged?.Invoke(newGridPos);
                }
            }
        }
        
        /// <summary>
        /// Called when movement is complete
        /// </summary>
        private void CompleteMovement()
        {
            if (!isMoving)
                return;
                
            isMoving = false;
            currentPath = null;
            
            // Ensure unit position is exactly at target grid position
            Vector3 finalWorldPos = Dokkaebi.Common.GridConverter.GridToWorld(targetGridPosition);

// Update the Debug.Log to use the new variable name if you copied that part
Debug.Log($"[MovementHandler] CompleteMovement: Snapping to targetGridPosition={targetGridPosition}, Calculated World Pos={finalWorldPos}. Current Transform Pos={transform.position}");
transform.position = finalWorldPos;
            _unit.SetGridPosition(targetGridPosition);
            
            // Notify listeners that movement is complete
            OnMoveComplete?.Invoke();
            
            SmartLogger.Log($"Movement complete for {_unit.GetUnitName()} to {targetGridPosition}", LogCategory.Movement);
        }
        
        /// <summary>
        /// Stop the current movement
        /// </summary>
        public void StopMovement()
{
    isMoving = false; // Make sure this is reset
    currentPath = null; // Make sure this is reset
    currentWaypoint = 0; // Reset waypoint

    // Optional: Maybe stop the Seeker if it's still calculating? (More advanced)
    // seeker.CancelCurrentPathRequest();

    // Update the unit's grid position to match current world position (already in your code)
    GridPosition currentGridPos = GridPositionConverter.WorldToGrid(transform.position);
    _unit.SetGridPosition(currentGridPos);

    SmartLogger.Log($"Movement stopped for {_unit?.GetUnitName()}", LogCategory.Movement);
}
        
        /// <summary>
        /// Check if the unit can reach a specific grid position
        /// </summary>
        public bool CanReachPosition(GridPosition targetPosition, int maxDistance)
        {
            if (_gridInfo == null)
                return false;
                
            // Get current position
            GridPosition currentPosition = _unit.CurrentGridPosition;
            
            // First check basic distance using Manhattan distance
            int manhattanDistance = Math.Abs(currentPosition.x - targetPosition.x) + Math.Abs(currentPosition.z - targetPosition.z);
            if (manhattanDistance > maxDistance)
                return false;
            
            // Then check if the tile is walkable
            if (!_gridInfo.IsWalkable(targetPosition.ToVector2Int()))
                return false;
            
            // Use A* for path existence check
            Vector3 worldTarget = GridPositionConverter.GridToWorld(targetPosition);
            Path testPath = ABPath.Construct(transform.position, worldTarget);
            AstarPath.StartPath(testPath);
            AstarPath.BlockUntilCalculated(testPath);
            
            // Check if path exists and is within range
            if (testPath.error || testPath.vectorPath.Count == 0)
                return false;
                
            // Estimate the path length in grid steps
            float pathLength = 0;
            for (int i = 0; i < testPath.vectorPath.Count - 1; i++)
            {
                pathLength += Vector3.Distance(testPath.vectorPath[i], testPath.vectorPath[i+1]);
            }
            
            // Convert path length to approximate grid distance (assuming grid cell size)
            float gridCellSize = _gridInfo.GetGridCellSize();
            int approximateGridDistance = Mathf.CeilToInt(pathLength / gridCellSize);
            
            return approximateGridDistance <= maxDistance;
        }
        
        /// <summary>
        /// Get all valid grid positions that the unit can move to
        /// </summary>
        public List<GridPosition> GetValidMovePositions()
        {
            if (_gridInfo == null)
                return new List<GridPosition>();
                
            List<GridPosition> validPositions = new List<GridPosition>();
            GridPosition startPosition = _unit.CurrentGridPosition;
            int moveRange = _unit.MovementRange;
            
            // Using Breadth-First Search to find all reachable positions
            Queue<GridPosition> nodesToVisit = new Queue<GridPosition>();
            Dictionary<GridPosition, int> costSoFar = new Dictionary<GridPosition, int>();
            
            // Add start position
            nodesToVisit.Enqueue(startPosition);
            costSoFar[startPosition] = 0;
            
            while (nodesToVisit.Count > 0)
            {
                GridPosition currentPos = nodesToVisit.Dequeue();
                
                // Skip if we're already at max range
                if (costSoFar[currentPos] >= moveRange)
                    continue;
                    
                // Get walkable neighbours
                IEnumerable<Vector2Int> neighbourCoords = _gridInfo.GetWalkableNeighbours(currentPos.ToVector2Int());
                
                foreach (Vector2Int neighbor in neighbourCoords)
                {
                    // Convert Vector2Int to GridPosition
                    GridPosition neighborGridPos = GridPosition.FromVector2Int(neighbor);
                    
                    // Calculate cost to move to this neighbor
                    int newCost = costSoFar[currentPos] + _gridInfo.GetNodeCost(neighbor);
                    
                    // Check if this position is valid and within range
                    if (newCost <= moveRange && (!costSoFar.ContainsKey(neighborGridPos) || newCost < costSoFar[neighborGridPos]))
                    {
                        costSoFar[neighborGridPos] = newCost;
                        nodesToVisit.Enqueue(neighborGridPos);
                        
                        // Add to valid positions if not start position
                        if (!neighborGridPos.Equals(startPosition))
                        {
                            validPositions.Add(neighborGridPos);
                        }
                    }
                }
            }
            
            return validPositions;
        }
        
        /// <summary>
        /// Check if the unit is currently moving
        /// </summary>
        public bool IsMoving()
        {
            return isMoving;
        }
    }
}