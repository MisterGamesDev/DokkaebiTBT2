using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Common;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Grid
{
    /// <summary>
    /// Manages the 10x10 grid for Dokkaebi, tracking tile occupancy, zones, and providing pathfinding services.
    /// </summary>
    public class GridManager : MonoBehaviour, IGridSystem, IPathfindingGridInfo
    {
        [Header("Grid Settings")]
        [SerializeField] private int gridWidth = 10;
        [SerializeField] private int gridHeight = 10;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector3 gridOrigin = Vector3.zero;
        
        [Header("Integration")]
        [SerializeField] private bool initializeGridConverter = true;
        
        // Grid data storage
        private Dictionary<GridPosition, GridCell> gridCells = new Dictionary<GridPosition, GridCell>();
        private Dictionary<GridPosition, List<IZoneInstance>> gridZones = new Dictionary<GridPosition, List<IZoneInstance>>();
        private Dictionary<GridPosition, bool> walkablePositions = new Dictionary<GridPosition, bool>();
        private Dictionary<GridPosition, int> voidSpaces = new Dictionary<GridPosition, int>();
        
        // Singleton instance
        private static GridManager instance;
        public static GridManager Instance { get { return instance; } }
        
        [Header("Visualization")]
        [SerializeField] private bool showGridInGameView = true;
        [SerializeField] private Color gridLineColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
        [SerializeField] private float gridLineWidth = 0.02f;
        [SerializeField] private bool showGridInSceneView = true;
        [SerializeField] private Color sceneViewGridColor = new Color(0.7f, 0.7f, 0.7f, 0.8f);
        [SerializeField] private bool showDebugVisuals = true;
        [SerializeField] private GameObject gridCellPrefab;
        [SerializeField] private GameObject highlightCellPrefab;
        
        // Visualization objects
        private Dictionary<GridPosition, GameObject> visualGridCells = new Dictionary<GridPosition, GameObject>();
        private List<GameObject> highlightCells = new List<GameObject>();

        // Change pathfinder reference to interface
        private IPathfinder pathfinder;
        // Move this method inside the GridManager class:
public float GetGridCellSize()
{
    return this.cellSize;
}
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("More than one GridManager detected. This instance will be destroyed.");
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize shared grid settings
            if (initializeGridConverter)
            {
                Common.GridConverter.CellSize = cellSize;
                Common.GridConverter.GridOrigin = gridOrigin;
            }
            
            // Find pathfinder - deferred to Start to avoid circular reference
            
            // Initialize grid
            InitializeGrid();
        }
        
        private void Start()
        {
            // Create runtime grid visualization if enabled
            if (showGridInGameView)
            {
                VisualizeGrid(gridLineColor, gridLineWidth);
            }
        }
        
        private void InitializeGrid()
        {
            // Reset collections
            gridCells.Clear();
            gridZones.Clear();
            walkablePositions.Clear();
            
            // Initialize grid cells
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    GridPosition pos = new GridPosition(x, z);
                    gridCells[pos] = new GridCell(pos);
                    walkablePositions[pos] = true;
                    
                    // Initialize empty zone lists for each position
                    gridZones[pos] = new List<IZoneInstance>();
                }
            }
            
            // Create visual grid if debug visuals are enabled
            if (showDebugVisuals && gridCellPrefab != null)
            {
                CreateVisualGrid();
            }
            
            Debug.Log($"GridManager initialized with grid size {gridWidth}x{gridHeight}");
        }
        
        private void CreateVisualGrid()
        {
            // Clean up any existing visuals
            ClearVisualGrid();
            
            // Create new grid cells
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    GridPosition pos = new GridPosition(x, z);
                    Vector3 worldPos = GridToWorld(pos);
                    
                    GameObject cell = Instantiate(gridCellPrefab, worldPos, Quaternion.identity, transform);
                    cell.name = $"GridCell_{x}_{z}";
                    
                    visualGridCells[pos] = cell;
                }
            }
        }
        
        private void ClearVisualGrid()
        {
            foreach (GameObject cell in visualGridCells.Values)
            {
                if (cell != null)
                {
                    DestroyImmediate(cell);
                }
            }
            
            visualGridCells.Clear();
        }
        
        /// <summary>
        /// Validates if a given grid position is within the grid bounds
        /// </summary>
        public bool IsPositionValid(GridPosition position)
        {
            return position.x >= 0 && position.x < gridWidth && 
                   position.z >= 0 && position.z < gridHeight;
        }

        /// <summary>
        /// Validates if a given Vector2Int position is within the grid bounds
        /// </summary>
        public bool IsValidGridPosition(Vector2Int position)
        {
            return position.x >= 0 && position.x < gridWidth && 
                   position.y >= 0 && position.y < gridHeight;
        }

        /// <summary>
        /// Places a unit at the specified grid position and updates its world position
        /// </summary>
        public void PlaceUnitAtPosition(IDokkaebiUnit unit, GridPosition gridPosition)
        {
            if (!IsPositionValid(gridPosition))
            {
                Debug.LogError($"Attempted to place unit at invalid grid position: {gridPosition}");
                return;
            }

            // Convert grid position to world position
            Vector3 worldPosition = GridToWorld(gridPosition);
            
            // Update the unit's position using the GameObject property from IDokkaebiUnit
            if (unit.GameObject != null)
            {
                unit.GameObject.transform.position = worldPosition;
            }
            
            // Update grid state
            UpdateGridState(gridPosition, unit);
            
            Debug.Log($"Placed unit with ID {unit.UnitId} at grid position {gridPosition}");
        }

        private void UpdateGridState(GridPosition position, IDokkaebiUnit unit)
        {
            // Update the grid cell state
            if (gridCells.TryGetValue(position, out var cell))
            {
                cell.OccupyingUnit = unit;
                cell.IsOccupied = true;
            }
            else
            {
                Debug.LogError($"Grid cell not found at position {position}");
            }
        }

        /// <summary>
        /// Converts a grid position to world position
        /// </summary>
        public Vector3 GridToWorld(GridPosition gridPos)
        {
            return Common.GridConverter.GridToWorld(gridPos);
        }

        /// <summary>
        /// Converts a world position to grid position
        /// </summary>
        public GridPosition WorldToGrid(Vector3 worldPos)
        {
            return Common.GridConverter.WorldToGrid(worldPos);
        }

        /// <summary>
        /// Gets the grid cell at the specified position
        /// </summary>
        public GridCell GetCellAtPosition(GridPosition position)
        {
            return gridCells.TryGetValue(position, out var cell) ? cell : null;
        }

        /// <summary>
        /// Gets the grid cell at the specified Vector2Int position
        /// </summary>
        public GridCell GetCellAtPosition(Vector2Int position)
        {
            return GetCellAtPosition(new GridPosition(position.x, position.y));
        }

        /// <summary>
        /// Clears all grid cells
        /// </summary>
        public void ClearGrid()
        {
            foreach (var cell in gridCells.Values)
            {
                cell.Clear();
            }
        }
        
        /// <summary>
        /// Check if a position is walkable (not blocked)
        /// </summary>
        public bool IsWalkable(GridPosition position)
        {
            if (!IsPositionValid(position)) return false;
            
            // Check if the position is blocked by terrain
            if (pathfinder != null && !pathfinder.IsWalkable(position)) return false;
            
            // Check if the position is occupied by a unit
            if (gridCells.TryGetValue(position, out var cell) && cell.IsOccupied) return false;

            return true;
        }
        
        /// <summary>
        /// Check if a position is occupied by a unit
        /// </summary>
        public bool IsTileOccupied(GridPosition gridPos)
        {
            return gridCells.ContainsKey(gridPos);
        }
        
        /// <summary>
        /// Find a path from start to target position
        /// </summary>
        public List<GridPosition> FindPath(GridPosition startPos, GridPosition targetPos, int maxDistance = int.MaxValue)
        {
            // For now, just return a direct path - this should be replaced with A* pathfinding
            List<GridPosition> path = new List<GridPosition>();
            
            if (!IsPositionValid(startPos) || !IsPositionValid(targetPos))
            {
                return path;
            }
            
            // Calculate Manhattan distance
            int distance = GetManhattanDistance(startPos, targetPos);
            
            if (distance > maxDistance)
            {
                // Target is too far
                return path;
            }
            
            // Add target position to path
            path.Add(targetPos);
            
            return path;
        }
        
        /// <summary>
        /// Get all positions that can be reached from a starting position with a given movement range
        /// </summary>
        public List<GridPosition> GetReachablePositions(GridPosition startPos, int movementRange)
        {
            List<GridPosition> reachablePositions = new List<GridPosition>();
            
            // Handle invalid starting position
            if (!IsPositionValid(startPos)) return reachablePositions;
            
            // Check all positions within movement range
            for (int x = -movementRange; x <= movementRange; x++)
            {
                for (int z = -movementRange; z <= movementRange; z++)
                {
                    // Calculate Manhattan distance
                    int distance = Mathf.Abs(x) + Mathf.Abs(z);
                    
                    if (distance <= movementRange)
                    {
                        GridPosition checkPos = new GridPosition(startPos.x + x, startPos.z + z);
                        
                        // Skip invalid or unwalkable positions
                        if (!IsPositionValid(checkPos) || !IsWalkable(checkPos))
                            continue;
                            
                        // Skip the starting position
                        if (checkPos.Equals(startPos))
                            continue;
                            
                        reachablePositions.Add(checkPos);
                    }
                }
            }
            
            return reachablePositions;
        }
        
        /// <summary>
        /// Calculate Manhattan distance between two grid positions
        /// </summary>
        public int GetManhattanDistance(GridPosition a, GridPosition b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
        }
        
        /// <summary>
        /// Adds a zone instance to a grid tile
        /// </summary>
        public void AddZoneToTile(GridPosition gridPos, IZoneInstance zone)
        {
            if (!IsPositionValid(gridPos))
            {
                Debug.LogError($"Attempted to add zone to invalid grid position: {gridPos}");
                return;
            }

            if (!gridZones.ContainsKey(gridPos))
            {
                gridZones[gridPos] = new List<IZoneInstance>();
            }

            if (!gridZones[gridPos].Contains(zone))
            {
                gridZones[gridPos].Add(zone);
            }
        }
        
        /// <summary>
        /// Removes a zone instance from a grid tile
        /// </summary>
        public void RemoveZoneFromTile(GridPosition gridPos, IZoneInstance zone)
        {
            if (!IsPositionValid(gridPos) || !gridZones.ContainsKey(gridPos))
            {
                return;
            }

            gridZones[gridPos].Remove(zone);
        }
        
        /// <summary>
        /// Gets all zone instances at a given grid position
        /// </summary>
        public List<IZoneInstance> GetZonesAtPosition(GridPosition gridPos)
        {
            if (!IsPositionValid(gridPos) || !gridZones.ContainsKey(gridPos))
            {
                return new List<IZoneInstance>();
            }

            return new List<IZoneInstance>(gridZones[gridPos]);
        }
        
        /// <summary>
        /// Highlight a list of grid positions
        /// </summary>
        public void HighlightPositions(List<GridPosition> positions, Color color)
        {
            ClearHighlights();
            
            if (highlightCellPrefab == null) return;
            
            foreach (GridPosition pos in positions)
            {
                if (IsPositionValid(pos))
                {
                    Vector3 worldPos = GridToWorld(pos);
                    GameObject highlight = Instantiate(highlightCellPrefab, worldPos, Quaternion.identity, transform);
                    
                    // Set color
                    Renderer renderer = highlight.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = color;
                    }
                    
                    highlightCells.Add(highlight);
                }
            }
        }
        
        /// <summary>
        /// Clear all grid position highlights
        /// </summary>
        public void ClearHighlights()
        {
            foreach (GameObject highlight in highlightCells)
            {
                if (highlight != null)
                {
                    Destroy(highlight);
                }
            }
            
            highlightCells.Clear();
        }
        
        /// <summary>
        /// Update void space durations
        /// </summary>
        public void UpdateVoidSpaceDurations()
        {
            List<GridPosition> expiredVoidSpaces = new List<GridPosition>();
            
            foreach (var kvp in voidSpaces)
            {
                GridPosition position = kvp.Key;
                int remainingDuration = kvp.Value - 1;
                
                if (remainingDuration <= 0)
                {
                    // Void space has expired
                    expiredVoidSpaces.Add(position);
                }
                else
                {
                    // Update the duration
                    voidSpaces[position] = remainingDuration;
                }
            }
            
            // Remove expired void spaces
            foreach (var position in expiredVoidSpaces)
            {
                voidSpaces.Remove(position);
            }
        }
        
        /// <summary>
        /// Convert a Vector2Int to a GridPosition
        /// </summary>
        public static GridPosition Vector2IntToGrid(Vector2Int vec)
        {
            return new GridPosition(vec.x, vec.y);
        }

        /// <summary>
        /// Convert a GridPosition to a Vector2Int
        /// </summary>
        public static Vector2Int GridToVector2Int(GridPosition gridPos)
        {
            return new Vector2Int(gridPos.x, gridPos.z);
        }
        
        /// <summary>
        /// Visualize the grid at runtime (creates visible line renderers)
        /// </summary>
        public void VisualizeGrid(Color lineColor, float lineWidth = 0.02f)
        {
            string name = "GridVisualization";
            
            // Remove any existing visualization
            Transform existing = transform.Find(name);
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }
            
            // Create container
            GameObject container = new GameObject(name);
            container.transform.SetParent(transform, false);
            
            // Create material
            Material lineMaterial = new Material(Shader.Find("Sprites/Default"));
            lineMaterial.color = lineColor;
            
            float yPos = gridOrigin.y + 0.01f; // Small offset to prevent Z-fighting
            
            // Create horizontal lines
            for (int z = 0; z <= gridHeight; z++)
            {
                GameObject lineObj = new GameObject($"HLine_{z}");
                lineObj.transform.SetParent(container.transform, false);
                
                LineRenderer line = lineObj.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.startWidth = lineWidth;
                line.endWidth = lineWidth;
                line.positionCount = 2;
                
                Vector3 start = new Vector3(gridOrigin.x, yPos, gridOrigin.z + z * cellSize);
                Vector3 end = new Vector3(gridOrigin.x + gridWidth * cellSize, yPos, gridOrigin.z + z * cellSize);
                
                line.SetPosition(0, start);
                line.SetPosition(1, end);
                line.material = lineMaterial;
            }
            
            // Create vertical lines
            for (int x = 0; x <= gridWidth; x++)
            {
                GameObject lineObj = new GameObject($"VLine_{x}");
                lineObj.transform.SetParent(container.transform, false);
                
                LineRenderer line = lineObj.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.startWidth = lineWidth;
                line.endWidth = lineWidth;
                line.positionCount = 2;
                
                Vector3 start = new Vector3(gridOrigin.x + x * cellSize, yPos, gridOrigin.z);
                Vector3 end = new Vector3(gridOrigin.x + x * cellSize, yPos, gridOrigin.z + gridHeight * cellSize);
                
                line.SetPosition(0, start);
                line.SetPosition(1, end);
                line.material = lineMaterial;
            }
            
            Debug.Log($"Grid visualization created with {gridWidth+1 + gridHeight+1} lines");
        }
        
        private void OnDrawGizmos()
        {
            if (showGridInSceneView)
            {
                float yPos = gridOrigin.y + 0.01f; // Small offset to prevent Z-fighting
                
                Gizmos.color = sceneViewGridColor;
                
                // Draw horizontal lines (Z axis)
                for (int z = 0; z <= gridHeight; z++)
                {
                    Vector3 start = new Vector3(gridOrigin.x, yPos, gridOrigin.z + z * cellSize);
                    Vector3 end = new Vector3(gridOrigin.x + gridWidth * cellSize, yPos, gridOrigin.z + z * cellSize);
                    Gizmos.DrawLine(start, end);
                }
                
                // Draw vertical lines (X axis)
                for (int x = 0; x <= gridWidth; x++)
                {
                    Vector3 start = new Vector3(gridOrigin.x + x * cellSize, yPos, gridOrigin.z);
                    Vector3 end = new Vector3(gridOrigin.x + x * cellSize, yPos, gridOrigin.z + gridHeight * cellSize);
                    Gizmos.DrawLine(start, end);
                }
                
                // Draw a boundary box around the grid to make it more visible
                Vector3 bottomLeft = new Vector3(gridOrigin.x, yPos, gridOrigin.z);
                Vector3 bottomRight = new Vector3(gridOrigin.x + gridWidth * cellSize, yPos, gridOrigin.z);
                Vector3 topLeft = new Vector3(gridOrigin.x, yPos, gridOrigin.z + gridHeight * cellSize);
                Vector3 topRight = new Vector3(gridOrigin.x + gridWidth * cellSize, yPos, gridOrigin.z + gridHeight * cellSize);
                
                // Draw boundary in a slightly more visible color
                Gizmos.color = new Color(sceneViewGridColor.r, sceneViewGridColor.g, sceneViewGridColor.b, 1f);
                Gizmos.DrawLine(bottomLeft, bottomRight);
                Gizmos.DrawLine(bottomRight, topRight);
                Gizmos.DrawLine(topRight, topLeft);
                Gizmos.DrawLine(topLeft, bottomLeft);
            }
        }

        /// <summary>
        /// Check if a tile is in void space
        /// </summary>
        public bool IsVoidSpace(GridPosition position)
        {
            return voidSpaces.ContainsKey(position);
        }

        /// <summary>
        /// Create a void space at the specified position with a duration
        /// </summary>
        public void CreateVoidSpace(GridPosition position, int duration)
        {
            if (!IsPositionValid(position)) return;
            
            // Set or update void space duration
            voidSpaces[position] = duration;
        }

        /// <summary>
        /// Remove a void space from the specified position
        /// </summary>
        public void RemoveVoidSpace(GridPosition position)
        {
            if (voidSpaces.ContainsKey(position))
            {
                voidSpaces.Remove(position);
            }
        }

        /// <summary>
        /// Get the width of the grid
        /// </summary>
        public int GetGridWidth() => gridWidth;
        
        /// <summary>
        /// Get the height of the grid
        /// </summary>
        public int GetGridHeight() => gridHeight;

        /// <summary>
        /// Sets a unit as the occupant of a grid position
        /// </summary>
        public void SetTileOccupant(GridPosition gridPos, IDokkaebiUnit unit)
        {
            if (!IsPositionValid(gridPos))
            {
                Debug.LogError($"Attempted to set occupant at invalid grid position: {gridPos}");
                return;
            }

            // Get the grid cell
            if (gridCells.TryGetValue(gridPos, out GridCell cell))
            {
                // Clear previous tile if the unit is already on the grid
                ClearUnitFromPreviousTile(unit);

                // Set the new occupant
                cell.OccupyingUnit = unit;
                cell.IsOccupied = unit != null;
            }
        }
        
        /// <summary>
        /// Clears a unit from its previous tile. Used when moving units to new positions.
        /// </summary>
        public void ClearUnitFromPreviousTile(IDokkaebiUnit unit)
        {
            foreach (var gridPosition in gridCells.Keys)
            {
                var cell = gridCells[gridPosition];
                if (cell.OccupyingUnit != null && cell.OccupyingUnit.UnitId == unit.UnitId)
                {
                    cell.Clear();
                    return;
                }
            }
        }
        
        /// <summary>
        /// Gets the unit occupying a given grid position
        /// </summary>
        public IDokkaebiUnit GetTileOccupant(GridPosition gridPos)
        {
            if (!IsPositionValid(gridPos))
            {
                return null;
            }

            if (gridCells.TryGetValue(gridPos, out GridCell cell))
            {
                return cell.OccupyingUnit;
            }

            return null;
        }

        /// <summary>
        /// Get an IPathfinder instance for pathfinding operations
        /// </summary>
        public IPathfinder GetPathfinder()
        {
            return pathfinder;
        }

        /// <summary>
        /// Get all walkable positions within range of a starting position
        /// </summary>
        public List<GridPosition> GetWalkablePositionsInRange(GridPosition start, int range)
        {
            if (pathfinder != null)
            {
                return pathfinder.GetWalkablePositionsInRange(start, range);
            }
            
            // Fallback if no pathfinder
            var result = new List<GridPosition>();
            for (int x = Mathf.Max(0, start.x - range); x <= Mathf.Min(gridWidth - 1, start.x + range); x++)
            {
                for (int z = Mathf.Max(0, start.z - range); z <= Mathf.Min(gridHeight - 1, start.z + range); z++)
                {
                    GridPosition pos = new GridPosition(x, z);
                    if (Common.GridConverter.GetGridDistance(start, pos) <= range && IsWalkable(pos))
                    {
                        result.Add(pos);
                    }
                }
            }
            return result;
        }

        #region IGridSystem Implementation
        
        /// <summary>
        /// Width of the grid in cells
        /// </summary>
        int IGridSystem.Width => gridWidth;
        
        /// <summary>
        /// Height of the grid in cells
        /// </summary>
        int IGridSystem.Height => gridHeight;
        
        /// <summary>
        /// Size of a cell in world units
        /// </summary>
        float IGridSystem.CellSize => cellSize;
        
        /// <summary>
        /// Convert a grid position to a world position
        /// </summary>
        public Vector3 GridToWorldPosition(Interfaces.GridPosition gridPosition)
        {
            var internalPosition = new GridPosition(gridPosition.x, gridPosition.z);
            return GridToWorld(internalPosition);
        }
        
        /// <summary>
        /// Convert a world position to a grid position
        /// </summary>
        public Interfaces.GridPosition WorldToGridPosition(Vector3 worldPosition)
        {
            var internalPosition = WorldToGrid(worldPosition);
            return new Interfaces.GridPosition(internalPosition.x, internalPosition.z);
        }
        
        /// <summary>
        /// Check if a grid position is valid (within bounds)
        /// </summary>
        public bool IsValidGridPosition(Interfaces.GridPosition gridPosition)
        {
            return gridPosition.x >= 0 && gridPosition.x < gridWidth &&
                   gridPosition.z >= 0 && gridPosition.z < gridHeight;
        }
        
        /// <summary>
        /// Get neighboring grid positions (orthogonal)
        /// </summary>
        public List<Interfaces.GridPosition> GetNeighborPositions(Interfaces.GridPosition gridPosition, bool includeDiagonals = false)
        {
            List<Interfaces.GridPosition> neighbors = new List<Interfaces.GridPosition>();
            
            // Orthogonal directions
            int[] dx = { 0, 1, 0, -1 };
            int[] dz = { 1, 0, -1, 0 };
            
            // Add orthogonal neighbors
            for (int i = 0; i < 4; i++)
            {
                Interfaces.GridPosition neighbor = new Interfaces.GridPosition(
                    gridPosition.x + dx[i],
                    gridPosition.z + dz[i]
                );
                
                if (IsValidGridPosition(neighbor))
                {
                    neighbors.Add(neighbor);
                }
            }
            
            // Add diagonal neighbors if requested
            if (includeDiagonals)
            {
                int[] dxDiag = { 1, 1, -1, -1 };
                int[] dzDiag = { 1, -1, 1, -1 };
                
                for (int i = 0; i < 4; i++)
                {
                    Interfaces.GridPosition neighbor = new Interfaces.GridPosition(
                        gridPosition.x + dxDiag[i],
                        gridPosition.z + dzDiag[i]
                    );
                    
                    if (IsValidGridPosition(neighbor))
                    {
                        neighbors.Add(neighbor);
                    }
                }
            }
            
            return neighbors;
        }
        
        /// <summary>
        /// Get all grid positions within a specific range
        /// </summary>
        public List<Interfaces.GridPosition> GetGridPositionsInRange(Interfaces.GridPosition centerPosition, int range)
        {
            List<Interfaces.GridPosition> positions = new List<Interfaces.GridPosition>();
            
            for (int x = centerPosition.x - range; x <= centerPosition.x + range; x++)
            {
                for (int z = centerPosition.z - range; z <= centerPosition.z + range; z++)
                {
                    Interfaces.GridPosition pos = new Interfaces.GridPosition(x, z);
                    
                    // Calculate Manhattan distance
                    int distance = Mathf.Abs(x - centerPosition.x) + Mathf.Abs(z - centerPosition.z);
                    
                    if (distance <= range && IsValidGridPosition(pos))
                    {
                        positions.Add(pos);
                    }
                }
            }
            
            return positions;
        }
        
        /// <summary>
        /// Check if a grid position is occupied by a unit or obstacle
        /// </summary>
        public bool IsPositionOccupied(Interfaces.GridPosition gridPosition)
        {
            var internalPosition = new GridPosition(gridPosition.x, gridPosition.z);
            
            if (!gridCells.TryGetValue(internalPosition, out var cell))
            {
                return false;
            }
            
            return cell.IsOccupied;
        }
        
        #endregion

        // Implementation of IPathfindingGridInfo interface methods
        public bool IsWalkable(Vector2Int coordinates)
        {
            GridPosition gridPos = new GridPosition(coordinates.x, coordinates.y);
            return IsPositionValid(gridPos) && walkablePositions.TryGetValue(gridPos, out bool isWalkable) ? isWalkable : false;
        }

        public int GetNodeCost(Vector2Int coordinates)
        {
            // For now, just return a basic cost of 1 for all walkable tiles
            // This can be enhanced to consider terrain types, zones, etc.
            return IsWalkable(coordinates) ? 1 : int.MaxValue;
        }

        public IEnumerable<Vector2Int> GetWalkableNeighbours(Vector2Int coordinates)
        {
            List<Vector2Int> neighbours = new List<Vector2Int>();
            
            // Check the four cardinal directions
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),  // North
                new Vector2Int(1, 0),  // East
                new Vector2Int(0, -1), // South
                new Vector2Int(-1, 0)  // West
            };
            
            foreach (var dir in directions)
            {
                Vector2Int neighbourPos = coordinates + dir;
                if (IsValidGridPosition(neighbourPos) && IsWalkable(neighbourPos))
                {
                    neighbours.Add(neighbourPos);
                }
            }
            
            return neighbours;
        }

        
    }
    
    /// <summary>
    /// Contains data for a single grid tile
    /// </summary>
    public class GridTileData
    {
        public GridPosition Position;
        public IDokkaebiUnit OccupyingUnit;
        public bool IsOccupied => OccupyingUnit != null;
        public List<IZoneInstance> Zones = new List<IZoneInstance>();
        public bool IsWalkable = true;
        public TerrainType TerrainType = TerrainType.Normal;
        public float Height = 0f;
        public bool IsVoidSpace = false;
        public int VoidSpaceDuration = 0;
        
        // Used for movement resolution
        public int ReservedForUnitId = -1;
        
    }
    
    /// <summary>
    /// Placeholder enum for future Terrain System
    /// </summary>
    public enum TerrainType
    {
        Normal,
        Elevated,
        Water,
        Obstacle
    }

    
} 