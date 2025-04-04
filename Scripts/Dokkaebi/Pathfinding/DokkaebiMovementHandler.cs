using UnityEngine;

public class DokkaebiMovementHandler : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float stoppingDistance = 0.1f;

    private Vector3 targetGridPosition;
    private bool isMoving = false;
    private Path path;
    private Seeker seeker;
    private Current current;
    private GridPositionConverter gridPositionConverter;
    private Unit _unit;

    public event System.Action OnMoveComplete;

    private void Awake()
    {
        seeker = GetComponent<Seeker>();
        current = GetComponent<Current>();
        gridPositionConverter = GetComponent<GridPositionConverter>();
        _unit = GetComponent<Unit>();
    }

    public void RequestPath(GridPosition targetPosition)
    {
        targetGridPosition = targetPosition;
        Vector3 startPos = transform.position;
        Vector3 endPos = GridPositionConverter.GridToWorld(targetPosition);
        Debug.Log($"[MovementHandler] RequestPath: Received targetPosition (GridPosition)={targetPosition}, Current World Pos={startPos}, Calculated A* Target World Pos={endPos}");

        SmartLogger.Log($"Requesting path from {GridPositionConverter.WorldToGrid(startPos)} to {targetPosition}", LogCategory.Movement);

        // Request a path using A* Pathfinding
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
        path = p;
        current.target = path.vectorPath;
        isMoving = true;
        
        // Log path info
        SmartLogger.Log($"Path found with {p.vectorPath.Count} waypoints", LogCategory.Movement);
    }

    private void Update()
    {
        if (isMoving)
        {
            MoveAlongPath();
        }
    }

    private void MoveAlongPath()
    {
        if (path == null || path.vectorPath.Count == 0)
        {
            Debug.LogError("Path is not valid!");
            return;
        }

        Vector3 targetPos = path.vectorPath[0];
        Vector3 moveDirection = (targetPos - transform.position).normalized;
        Vector3 desiredVelocity = moveDirection * moveSpeed;

        // Apply rotation
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // Apply movement
        Vector3 velocity = desiredVelocity;
        current.desiredVelocity = velocity;

        // Check if we've reached the target
        float distance = Vector3.Distance(transform.position, targetPos);
        if (distance < stoppingDistance)
        {
            path.vectorPath.RemoveAt(0);
            if (path.vectorPath.Count == 0)
            {
                CompleteMovement();
            }
        }
    }

    private void CompleteMovement()
    {
        if (!isMoving)
            return;
                
        isMoving = false;
        path = null;
        
        // Log before snapping
        Debug.Log($"[MovementHandler] CompleteMovement: Before Snap - Current Transform Pos={transform.position}, Target GridPos={targetGridPosition}");
        
        // Ensure unit position is exactly at target grid position
        Vector3 finalWorldPos = GridPositionConverter.GridToWorld(targetGridPosition);
        Debug.Log($"[MovementHandler] CompleteMovement: Snapping to World Pos={finalWorldPos} from GridPos={targetGridPosition}");
        transform.position = finalWorldPos;
        _unit.SetGridPosition(targetGridPosition);
        
        // Notify listeners that movement is complete
        OnMoveComplete?.Invoke();
        
        SmartLogger.Log($"Movement complete for {_unit.GetUnitName()} to {targetGridPosition}", LogCategory.Movement);
    }
} 