using UnityEngine;
using System;
using Dokkaebi.Grid;
using Dokkaebi.Units;
using Dokkaebi.Common;
using Dokkaebi.Interfaces;
using Dokkaebi.Utilities;

namespace Dokkaebi.Core
{
    /// <summary>
    /// Manages input handling and coordinates with other managers
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        // Singleton reference
        public static InputManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private UnitManager unitManager;
        [SerializeField] private PlayerActionManager playerActionManager;

        [Header("Input Settings")]
        [SerializeField] private LayerMask unitLayer;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float raycastDistance = 100f;

        // Events
        public event Action<Vector2Int?> OnGridCoordHovered;
        public event Action<DokkaebiUnit> OnUnitHovered;
        public event Action<DokkaebiUnit> OnUnitSelected;
        public event Action OnUnitDeselected;

        private void Awake()
        {
            // Singleton setup
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Find required managers
            if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
            if (unitManager == null) unitManager = FindObjectOfType<UnitManager>();
            if (playerActionManager == null) playerActionManager = FindObjectOfType<PlayerActionManager>();

            if (gridManager == null || unitManager == null || playerActionManager == null)
            {
                Debug.LogError("Required managers not found in scene!");
                return;
            }
        }

        private void Update()
        {
            // Update hover position
            UpdateHoverPosition();

            // Handle input
            HandleInput();
        }

        

        private void HandleInput()
        {
            // Process left click for unit selection
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                SmartLogger.Log("Left click detected", LogCategory.General);
                Ray ray = UnityEngine.Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
                RaycastHit hit;
                
                // Check for unit hits first
                if (Physics.Raycast(ray, out hit, raycastDistance, unitLayer))
                {
                    SmartLogger.Log($"Hit something on unit layer at {hit.point}", LogCategory.General);
                    DokkaebiUnit unit = hit.collider.GetComponent<DokkaebiUnit>();
                    if (unit != null)
                    {
                        Debug.Log($"[InputManager] Unit layer hit! Found unit: {unit.GetUnitName()}. Invoking OnUnitSelected.");
                        OnUnitSelected?.Invoke(unit);
                        return;
                    }
                    else
                    {
                        SmartLogger.Log("Hit object on unit layer but no DokkaebiUnit component found", LogCategory.General);
                    }
                }
                else
                {
                    SmartLogger.Log($"No hit on unit layer. Layer mask: {unitLayer.value}", LogCategory.General);
                }
                
                // If no unit was hit, check for ground hit
                if (Physics.Raycast(ray, out hit, raycastDistance, groundLayer))
                {
                    GridPosition gridPos = gridManager.WorldToGrid(hit.point);
                    Vector2Int vectorPos = gridPos.ToVector2Int();
                    Debug.Log($"[InputManager] Ground Click: Hit World Pos={hit.point}, Calculated GridPos={gridPos}, Calculated Vector2Int={vectorPos}");
                    playerActionManager.HandleGroundClick(vectorPos);
                }
            }
            
            // Check for right click to deselect
            if (UnityEngine.Input.GetMouseButtonDown(1))
            {
                OnUnitDeselected?.Invoke();
            }
        }

        private void UpdateHoverPosition()
        {
            Ray ray = UnityEngine.Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
            RaycastHit hit;

            // Check for unit hover
            if (Physics.Raycast(ray, out hit, raycastDistance, unitLayer))
            {
                DokkaebiUnit unit = hit.collider.GetComponent<DokkaebiUnit>();
                if (unit != null)
                {
                    OnUnitHovered?.Invoke(unit);
                    return;
                }
            }

            // Check for ground hover
            if (Physics.Raycast(ray, out hit, raycastDistance, groundLayer))
            {
                GridPosition gridPos = gridManager.WorldToGrid(hit.point);
                // Convert GridPosition to Vector2Int using z instead of y
                Vector2Int vectorPos = new Vector2Int(gridPos.x, gridPos.z);
                OnGridCoordHovered?.Invoke(vectorPos);
            }
            else
            {
                OnGridCoordHovered?.Invoke(null);
            }
        }
    }
} 