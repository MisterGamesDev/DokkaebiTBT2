using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Core;
using Dokkaebi.Interfaces;
using Dokkaebi.Common;
using Dokkaebi.Units;
using Dokkaebi.Utilities;
using Dokkaebi.Grid;

namespace Dokkaebi.UI
{
    public class UnitSelectionController : MonoBehaviour, DokkaebiUpdateManager.IUpdateObserver
    {
        [Header("References")]
        [SerializeField] private GameObject turnSystemObject;
        [SerializeField] private GameObject cameraControllerObject;
        [SerializeField] private GameObject abilityManagerObject;
        
        [Header("Selection Settings")]
        [SerializeField] private LayerMask unitLayer;
        [SerializeField] private LayerMask groundLayer;
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject moveTargetPrefab;
        [SerializeField] private GameObject selectionIndicatorPrefab;
        [SerializeField] private GameObject abilityTargetPrefab;
        [SerializeField] private Color validMoveColor = Color.green;
        [SerializeField] private Color invalidMoveColor = Color.red;
        [SerializeField] private Color abilityRangeColor = Color.blue;
        
        // References to core systems via interfaces
        private ITurnSystem turnSystem;
        private ICameraController cameraController;
        private IAbilitySystem abilitySystem;
        private InputManager inputManager;
        private UnitManager unitManager;
        
        // Currently selected unit
        private IDokkaebiUnit selectedUnit;
        private GameObject selectionIndicator;
        
        // Valid movement visualization
        private List<GameObject> moveTargetMarkers = new List<GameObject>();
        private HashSet<GridPosition> validMovePositions = new HashSet<GridPosition>();
        private bool showingMoveTargets = false;
        
        // Ability selection and targeting
        private bool isSelectingAbility = false;
        private bool isTargetingAbility = false;
        private int selectedAbilityIndex = -1;
        private IAbilityData selectedAbility;
        private List<GameObject> abilityRangeIndicators = new List<GameObject>();
        private HashSet<GridPosition> validAbilityTargets = new HashSet<GridPosition>();
        
        // Temporary grid position when hovering over terrain
        private GridPosition hoverGridPosition;
        private bool isHoveringValidMove = false;
        private bool isHoveringValidAbilityTarget = false;
        
        private void Awake()
        {
            // Get interface references from GameObjects
            if (turnSystemObject != null)
            {
                turnSystem = turnSystemObject.GetComponent<ITurnSystem>();
            }
            
            if (cameraControllerObject != null)
            {
                cameraController = cameraControllerObject.GetComponent<ICameraController>();
            }
            
            if (abilityManagerObject != null)
            {
                abilitySystem = abilityManagerObject.GetComponent<IAbilitySystem>();
            }
            
            // Find required managers
            inputManager = InputManager.Instance;
            unitManager = UnitManager.Instance;
            
            // Log warnings if interfaces/managers not found
            if (turnSystem == null)
            {
                Debug.LogWarning("UnitSelectionController: No ITurnSystem interface found.");
            }
            
            if (cameraController == null)
            {
                Debug.LogWarning("UnitSelectionController: No ICameraController interface found.");
            }
            
            if (abilitySystem == null)
            {
                Debug.LogWarning("UnitSelectionController: No IAbilitySystem interface found.");
            }
            
            if (inputManager == null)
            {
                Debug.LogError("UnitSelectionController: InputManager not found in scene!");
            }
            
            if (unitManager == null)
            {
                Debug.LogError("UnitSelectionController: UnitManager not found in scene!");
            }
        }
        
        private void Start()
        {
            // Register with update manager
            DokkaebiUpdateManager.Instance.RegisterUpdateObserver(this);
            
            // Create selection indicator but make it inactive initially
            if (selectionIndicatorPrefab != null)
            {
                selectionIndicator = Instantiate(selectionIndicatorPrefab);
                selectionIndicator.SetActive(false);
            }
            
            // Subscribe to turn system events
            if (turnSystem != null)
            {
                turnSystem.OnPhaseChanged += HandlePhaseChanged;
            }
        }
        
        private void OnDestroy()
        {
            // Unregister from update manager
            if (DokkaebiUpdateManager.Instance != null)
            {
                DokkaebiUpdateManager.Instance.UnregisterUpdateObserver(this);
            }
            
            // Unsubscribe from turn system events
            if (turnSystem != null)
            {
                turnSystem.OnPhaseChanged -= HandlePhaseChanged;
            }
            
            // Unsubscribe from input events
            if (inputManager != null)
            {
                inputManager.OnUnitSelected -= HandleUnitSelected;
                inputManager.OnUnitDeselected -= HandleUnitDeselected;
            }
            
            // Clean up move target markers
            ClearMoveTargets();
        }

        private void OnEnable()
{
    Debug.Log("[UnitSelectionController] OnEnable called.");

    // ADD THIS SPECIFIC LOG:
    Debug.Log($"[UnitSelectionController] In OnEnable - Checking InputManager.Instance directly: {(InputManager.Instance != null)}");

    // Your existing logic to get/check inputManager variable:
    if (inputManager == null) inputManager = InputManager.Instance; // Ensure you try to get it if null

    if (inputManager != null)
    {
         Debug.Log("[UnitSelectionController] Subscribing to inputManager events in OnEnable.");
        inputManager.OnUnitSelected += HandleUnitSelected;
        inputManager.OnUnitDeselected += HandleUnitDeselected;
    } else {
        Debug.LogError("[UnitSelectionController] Cannot subscribe in OnEnable, InputManager reference is null!");
    }
}

private void OnDisable()
{
    Debug.Log("[UnitSelectionController] OnDisable called.");
    // Unsubscribe from input events
    if (inputManager != null)
    {
         Debug.Log("[UnitSelectionController] Unsubscribing from inputManager events in OnDisable.");
        inputManager.OnUnitSelected -= HandleUnitSelected;
        inputManager.OnUnitDeselected -= HandleUnitDeselected;
    }
}
        
        private void HandleUnitSelected(DokkaebiUnit unit)
        {
            Debug.Log($"[UnitSelectionController] HandleUnitSelected event received for unit: {(unit != null ? unit.GetUnitName() : "NULL")}");

    if (unit == null /* || !unit.IsPlayer() */) // Check if unit is valid (add player check back if needed)
    {
        Debug.Log("[UnitSelectionController] Invalid unit received or not player unit. Deselecting.");
        HandleUnitDeselected(); // Call deselect if unit is invalid
        return;
    }

    // ADD THIS LOG:
    Debug.Log($"[UnitSelectionController] Calling UnitManager.SetSelectedUnit for {unit.GetUnitName()}");
    if(unitManager == null) Debug.LogError("[UnitSelectionController] UnitManager reference is NULL!"); // Add null check
    else unitManager.SetSelectedUnit(unit); // Ensure this line is called
            
            // Update UnitManager's selected unit
            unitManager.SetSelectedUnit(unit);
            
            // Store locally
            selectedUnit = unit;
            
            // Show visual feedback
            ShowSelectionIndicator(unit);
            ShowMoveTargets();
            
            SmartLogger.Log($"Selected unit: {unit.GetUnitName()}", LogCategory.Unit);
        }
        
        private void HandleUnitDeselected()
        {
            Debug.Log($"[UnitSelectionController] HandleUnitDeselected event received/called.");
    if(unitManager == null) Debug.LogError("[UnitSelectionController] UnitManager reference is NULL!"); // Add null check
    else unitManager.ClearSelectedUnit(); // Ensure this is called
            // Clear UnitManager's selected unit
            unitManager.ClearSelectedUnit();
            
            // Clear local reference
            selectedUnit = null;
            
            // Hide visual feedback
            HideSelectionIndicator();
            HideMoveTargets();
            
            SmartLogger.Log("Unit deselected", LogCategory.Unit);
        }
        
        // Placeholder methods for visual feedback
        private void ShowSelectionIndicator(IDokkaebiUnit unit)
        {
            if (selectionIndicator == null || unit == null)
            {
                SmartLogger.LogWarning("Cannot show selection indicator - missing references", LogCategory.Unit);
                return;
            }

            var unitGameObject = unit.GameObject;
            if (unitGameObject == null)
            {
                SmartLogger.LogWarning("Cannot show selection indicator - unit has no GameObject", LogCategory.Unit);
                return;
            }

            // Position the indicator at the unit's position
            selectionIndicator.transform.position = unitGameObject.transform.position;
            
            // Parent to the unit for automatic movement following
            selectionIndicator.transform.SetParent(unitGameObject.transform, true);
            
            // Show the indicator
            selectionIndicator.SetActive(true);
        }
        
        private void HideSelectionIndicator()
        {
            if (selectionIndicator == null)
                return;
                
            // Unparent from any unit
            selectionIndicator.transform.SetParent(null);
            
            // Hide the indicator
            selectionIndicator.SetActive(false);
        }
        
        private void ShowMoveTargets()
        {
            // Clear any existing markers first
            HideMoveTargets();

            // Validate requirements
            if (selectedUnit == null || moveTargetPrefab == null)
            {
                SmartLogger.LogWarning("Cannot show move targets - missing unit or prefab", LogCategory.Movement);
                return;
            }

            // Check if unit can move
            if (!turnSystem.CanUnitMove(selectedUnit))
            {
                SmartLogger.LogWarning($"Unit {selectedUnit.GetUnitName()} cannot move at this time", LogCategory.Movement);
                return;
            }

            // Get valid move positions
            validMovePositions = new HashSet<GridPosition>(selectedUnit.GetValidMovePositions());

            // Create markers for each valid position
            foreach (var pos in validMovePositions)
            {
                // Get world position
                Vector3 worldPos = GridManager.Instance.GridToWorld(pos);

                // Instantiate marker
                GameObject marker = Instantiate(moveTargetPrefab, worldPos, Quaternion.identity, transform);

                // Set color
                var renderer = marker.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = validMoveColor;
                }

                // Add to list for cleanup
                moveTargetMarkers.Add(marker);
            }

            showingMoveTargets = true;
            SmartLogger.Log($"Showing {validMovePositions.Count} valid move positions for {selectedUnit.GetUnitName()}", LogCategory.Movement);
        }
        
        private void HideMoveTargets()
        {
            // Destroy all markers
            foreach (var marker in moveTargetMarkers)
            {
                if (marker != null)
                {
                    Destroy(marker);
                }
            }

            // Clear collections
            moveTargetMarkers.Clear();
            validMovePositions.Clear();
            showingMoveTargets = false;
        }
        
        private void ClearMoveTargets()
        {
            HideMoveTargets();
        }
        
        // Interface-based update method
        public void CustomUpdate(float deltaTime)
        {
            if (isTargetingAbility)
            {
                HandleAbilityTargetingInput();
            }
            else
            {
                HandleSelectionInput();
                HandleMovementInput();
                HandleAbilitySelectionInput();
            }
            
            // Handle keyboard shortcuts
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                EndCurrentPhase();
            }
            
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                if (isTargetingAbility)
                {
                    CancelAbilityTargeting();
                }
                else if (selectedUnit != null)
                {
                    HandleUnitDeselected();
                }
            }
        }
        
        private void HandlePhaseChanged(TurnPhase newPhase)
        {
            // Handle phase changes based on interfaces
            // Implementation depends on the events available in ITurnSystem
        }
        
        private void HandleSelectionInput()
        {
            // Implementation using interfaces
        }
        
        private void HandleMovementInput()
        {
            // Implementation using interfaces
        }
        
        private void HandleAbilitySelectionInput()
        {
            // Implementation using interfaces
        }
        
        private void EndCurrentPhase()
        {
            // Implementation using interfaces
        }
        
        private void CancelAbilityTargeting()
        {
            // Implementation using interfaces
        }
        
        private void HandleAbilityTargetingInput()
        {
            // Implementation will be added in a future task
        }
    }
}