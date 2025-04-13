using UnityEngine;
using System;
using System.Collections;
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

        [Header("Debug Visualizers")]
        [SerializeField] private GameObject clickMarkerPrefab;
        [SerializeField] private float clickMarkerDuration = 1.0f;
        [SerializeField] private Color raycastColor = Color.yellow;

        private GameObject currentClickMarker;
        private Coroutine clickMarkerCoroutine;

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
            //SmartLogger.Log($"[InputManager.Update] Frame {Time.frameCount}", LogCategory.Input);

            // Debug log to check time scale
            //Debug.Log($"[InputManager] Time.timeScale: {Time.timeScale}");

            // Update hover position
            UpdateHoverPosition();

            // Handle input
            HandleInput();
        }

        private void HandleInput()
        {
            Ray ray = UnityEngine.Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
            RaycastHit hit;

            // Draw Ray Visualization (Scene View Only)
            Debug.DrawRay(ray.origin, ray.direction * raycastDistance, raycastColor);

            // Process left click for unit selection or ability targeting
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                var currentState = PlayerActionManager.Instance?.GetCurrentActionState() ?? PlayerActionManager.ActionState.Idle;
                
                // If PAM is expecting an ability target, let other systems (like UnitSelectionController) handle it
                if (currentState == PlayerActionManager.ActionState.SelectingAbilityTarget)
                {
                    SmartLogger.Log("[InputManager.HandleInput] Ignoring left click because PAM state is SelectingAbilityTarget.", LogCategory.Input);
                    return; // Exit early, let UnitSelectionController handle the targeting click
                }

                SmartLogger.Log($"[InputManager.HandleInput] Left Click DETECTED Inside Check! Current PAM State: {currentState}", LogCategory.Input);
                SmartLogger.Log("Left click detected", LogCategory.General);
                
                // Log raycast setup
                SmartLogger.Log($"[InputManager] Raycast Setup - Origin: {ray.origin}, Direction: {ray.direction}, Distance: {raycastDistance}, UnitLayer: {LayerMask.LayerToName(Mathf.RoundToInt(Mathf.Log(unitLayer.value, 2)))}", LogCategory.General);
                
                // Check for unit hits first
                if (Physics.Raycast(ray, out hit, raycastDistance, unitLayer))
                {
                    // Debug logging for ability targeting state
                    SmartLogger.Log($"[InputManager] Raycast hit during state: {currentState}", LogCategory.Ability);
                    SmartLogger.Log($"[InputManager] Hit object - Name: {hit.collider.gameObject.name}, Tag: {hit.collider.gameObject.tag}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}", LogCategory.Ability);
                    
                    SmartLogger.Log($"Hit something on unit layer at {hit.point}", LogCategory.General);
                    
                    DokkaebiUnit unit = hit.collider.GetComponentInParent<DokkaebiUnit>();

                    if (unit != null)
                    {
                        SmartLogger.Log($"[InputManager] Found unit: {unit.GetUnitName()}, IsAlive: {unit.IsAlive}, IsPlayerControlled: {unit.IsPlayerControlled}", LogCategory.Ability);
                        
                        // Handle unit click through PAM (which will now only handle selection since we're not in targeting mode)
                        playerActionManager.HandleUnitClick(unit);

                        if(Instance != null && Instance.OnUnitSelected != null)
                        {
                            Instance.OnUnitSelected.Invoke(unit);
                        }
                        else
                        {
                            Debug.LogError($"[InputManager] Failed to invoke OnUnitSelected. Instance is {(Instance == null ? "NULL" : "Valid")}, OnUnitSelected is {(OnUnitSelected == null ? "NULL" : "Valid")}");
                        }
                        
                        HideClickMarker(); // Hide marker when selecting a unit
                        return;
                    }
                    else
                    {
                        SmartLogger.Log($"[InputManager] Hit object has no DokkaebiUnit component. Hierarchy path: {GetGameObjectPath(hit.collider.gameObject)}", LogCategory.Ability);
                    }
                }
                else
                {
                    // Enhanced debug logging for raycast miss
                    SmartLogger.Log($"[InputManager] Raycast missed. Details:", LogCategory.General);
                    SmartLogger.Log($"  - Layer mask value: {unitLayer.value}", LogCategory.General);
                    SmartLogger.Log($"  - Ray origin: {ray.origin}", LogCategory.General);
                    SmartLogger.Log($"  - Ray direction: {ray.direction}", LogCategory.General);
                    SmartLogger.Log($"  - Max distance: {raycastDistance}", LogCategory.General);
                    
                    // Debug draw the ray in the scene view for longer duration
                    Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.red, 2f);
                    
                    // Perform an overlap check to see if there are any units in the area
                    var colliders = Physics.OverlapSphere(ray.origin + ray.direction * (raycastDistance * 0.5f), raycastDistance, unitLayer);
                    if (colliders.Length > 0)
                    {
                        SmartLogger.Log($"[InputManager] Found {colliders.Length} unit layer objects in general area:", LogCategory.General);
                        foreach (var col in colliders)
                        {
                            SmartLogger.Log($"  - {col.gameObject.name} at {col.transform.position}, Layer: {LayerMask.LayerToName(col.gameObject.layer)}", LogCategory.General);
                        }
                    }
                }
                
                // If no unit was hit, check for ground hit
                if (Physics.Raycast(ray, out hit, raycastDistance, groundLayer))
                {
                    GridPosition gridPos = gridManager.WorldToNearestGrid(hit.point);
                    ShowClickMarker(gridPos); // Show marker at calculated grid position
                    Vector2Int vectorPos = gridPos.ToVector2Int();
                    // Debug.Log($"[InputManager] Ground Click: Hit World Pos={hit.point}, Calculated GridPos={gridPos}, Calculated Vector2Int={vectorPos}");
                    playerActionManager.HandleGroundClick(vectorPos);
                }
                else
                {
                    HideClickMarker(); // Hide marker if click hits nothing relevant
                }
            }
            
            // Handle ability keyboard shortcuts (1-4)
            if (unitManager != null)
            {
                var selectedUnit = unitManager.GetSelectedUnit();
                if (selectedUnit != null)
                {
                    var abilities = selectedUnit.GetAbilities();
                    if (abilities != null)
                    {
                        // Check for number keys 1-4
                        for (int i = 0; i < 4; i++)
                        {
                            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                            {
                                if (i < abilities.Count)
                                {
                                    playerActionManager?.StartAbilityTargeting(selectedUnit, i);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            
            // Check for right click to deselect or cancel targeting
            if (UnityEngine.Input.GetMouseButtonDown(1))
            {
                playerActionManager.CancelAbilityTargeting();
                OnUnitDeselected?.Invoke();
                HideClickMarker(); // Hide marker on deselect
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
                GridPosition gridPos = gridManager.WorldToNearestGrid(hit.point);
                // Convert GridPosition to Vector2Int using z instead of y
                Vector2Int vectorPos = new Vector2Int(gridPos.x, gridPos.z);
                OnGridCoordHovered?.Invoke(vectorPos);
            }
            else
            {
                OnGridCoordHovered?.Invoke(null);
            }
        }

        private void ShowClickMarker(GridPosition gridPos)
        {
            if (clickMarkerPrefab == null) return;

            if (currentClickMarker == null)
            {
                currentClickMarker = Instantiate(clickMarkerPrefab, transform);
            }

            // Ensure GridManager instance is available
            if (gridManager == null) gridManager = GridManager.Instance;
            if (gridManager == null) return;

            // Convert grid position to world position
            Vector3 worldPos = gridManager.GridToWorldPosition(gridPos);
            currentClickMarker.transform.position = worldPos + Vector3.up * 0.05f; // Slightly above ground
            currentClickMarker.SetActive(true);

            // Restart the timer coroutine
            if (clickMarkerCoroutine != null)
            {
                StopCoroutine(clickMarkerCoroutine);
            }
            clickMarkerCoroutine = StartCoroutine(ClickMarkerTimer());
        }

        private void HideClickMarker()
        {
            if (currentClickMarker != null)
            {
                currentClickMarker.SetActive(false);
            }
            if (clickMarkerCoroutine != null)
            {
                StopCoroutine(clickMarkerCoroutine);
                clickMarkerCoroutine = null;
            }
        }

        private IEnumerator ClickMarkerTimer()
        {
            yield return new WaitForSeconds(clickMarkerDuration);
            if (currentClickMarker != null)
            {
                currentClickMarker.SetActive(false);
            }
            clickMarkerCoroutine = null;
        }

        // Helper method to get full hierarchy path of GameObject
        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
} 
