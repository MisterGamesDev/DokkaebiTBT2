using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Core;
using Dokkaebi.Interfaces;
using Dokkaebi.Common;
using Dokkaebi.Units;
using Dokkaebi.Utilities;
using Dokkaebi.Grid;
using Dokkaebi.Pathfinding;
using Dokkaebi.Core.Data;

namespace Dokkaebi.UI
{
    /// <summary>
    /// Handles unit selection and targeting input
    /// </summary>
    public class UnitSelectionController : MonoBehaviour, DokkaebiUpdateManager.IUpdateObserver
    {
        [Header("References")]
        [SerializeField] private PlayerActionManager actionManager;
        [SerializeField] private PreviewManager previewManager;
        [SerializeField] private AbilitySelectionUI abilityUI;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private UnitManager unitManager;
        [SerializeField] private InputManager inputManager;
        [SerializeField] private UnitInfoPanel unitInfoPanel;

        [Header("Selection Settings")]
        [SerializeField] private float raycastDistance = 100f;
        [SerializeField] private LayerMask unitLayer;
        [SerializeField] private LayerMask groundLayer;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject moveTargetMarker;
        [SerializeField] private GameObject abilityTargetMarker;
        [SerializeField] private Material validMoveMaterial;
        [SerializeField] private Material invalidMoveMaterial;
        [SerializeField] private Material validAbilityTargetMaterial;
        [SerializeField] private Material invalidAbilityTargetMaterial;

        // State tracking
        private bool isSelectingAbility;
        private bool isTargetingAbility;
        private AbilityData selectedAbility;
        private HashSet<Interfaces.GridPosition> validAbilityTargets;
        private List<GameObject> moveTargetMarkers = new List<GameObject>();
        private List<GameObject> abilityTargetMarkers = new List<GameObject>();
        private DokkaebiUnit selectedUnit;

        private void Awake()
        {
            // Get references if needed
            if (actionManager == null) actionManager = FindObjectOfType<PlayerActionManager>();
            if (previewManager == null) previewManager = FindObjectOfType<PreviewManager>();
            if (abilityUI == null) abilityUI = FindObjectOfType<AbilitySelectionUI>();
            if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
            if (unitManager == null) unitManager = FindObjectOfType<UnitManager>();
            if (inputManager == null) inputManager = FindObjectOfType<InputManager>();

            if (actionManager == null || previewManager == null || abilityUI == null || gridManager == null || unitManager == null || inputManager == null)
            {
                SmartLogger.LogError("Required references not found!", LogCategory.General);
                return;
            }

            // Initialize state
            isSelectingAbility = false;
            isTargetingAbility = false;
            validAbilityTargets = new HashSet<Interfaces.GridPosition>();

            // Subscribe to events
            actionManager.OnAbilityTargetingStarted += HandleAbilityTargetingStarted;
            actionManager.OnAbilityTargetingCancelled += HandleAbilityTargetingCancelled;
        }

        private void OnEnable()
        {
            DokkaebiUpdateManager.Instance?.RegisterUpdateObserver(this);
            
            // Subscribe to input manager events
            if (inputManager != null)
            {
                inputManager.OnUnitSelected += HandleUnitSelected;
                inputManager.OnUnitDeselected += HandleUnitDeselected;
                inputManager.OnGridCoordHovered += HandleGridCoordHovered;
            }
        }

        private void OnDisable()
        {
            DokkaebiUpdateManager.Instance?.UnregisterUpdateObserver(this);
            
            // Unsubscribe from input manager events
            if (inputManager != null)
            {
                inputManager.OnUnitSelected -= HandleUnitSelected;
                inputManager.OnUnitDeselected -= HandleUnitDeselected;
                inputManager.OnGridCoordHovered -= HandleGridCoordHovered;
            }

            // Clean up any remaining markers
            HideMoveTargets();
            HideAbilityTargets();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (actionManager != null)
            {
                actionManager.OnAbilityTargetingStarted -= HandleAbilityTargetingStarted;
                actionManager.OnAbilityTargetingCancelled -= HandleAbilityTargetingCancelled;
            }
        }

        public void CustomUpdate(float deltaTime)
        {
            if (isTargetingAbility)
            {
                HandleAbilityTargetingInput();
            }
            else
            {
                HandleSelectionInput();
            }
        }

        private void HandleSelectionInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                // Check for unit hit
                if (Physics.Raycast(ray, out hit, raycastDistance, unitLayer))
                {
                    var unit = hit.collider.GetComponent<DokkaebiUnit>();
                    if (unit != null)
                    {
                        HandleUnitClick(unit);
                    }
                }
                // Check for ground hit
                else if (Physics.Raycast(ray, out hit, raycastDistance, groundLayer))
                {
                    var gridPos = gridManager.WorldToGridPosition(hit.point);
                    HandleGroundClick(gridPos);
                }
            }
        }

        private void HandleAbilityTargetingInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                // Check for unit hit
                if (Physics.Raycast(ray, out hit, raycastDistance, unitLayer))
                {
                    var unit = hit.collider.GetComponent<DokkaebiUnit>();
                    if (unit != null)
                    {
                        actionManager.HandleUnitClick(unit);
                    }
                }
                // Check for ground hit
                else if (Physics.Raycast(ray, out hit, raycastDistance, groundLayer))
                {
                    var gridPos = gridManager.WorldToGridPosition(hit.point);
                    actionManager.HandleGroundClick(gridPos.ToVector2Int());
                }
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                actionManager.CancelAbilityTargeting();
            }
        }

        private void HandleUnitClick(DokkaebiUnit unit)
        {
            SmartLogger.Log($"[UnitSelectionController] HandleUnitClick called for unit: {unit?.GetUnitName()}, isTargetingAbility: {isTargetingAbility}, Current PAM State: {actionManager?.GetCurrentActionState()}", LogCategory.Ability);

            if (isTargetingAbility || (actionManager != null && actionManager.GetCurrentActionState() == PlayerActionManager.ActionState.SelectingAbilityTarget))
            {
                SmartLogger.Log($"[UnitSelectionController] In targeting mode, forwarding to actionManager. Stack trace:\n{System.Environment.StackTrace}", LogCategory.Ability);
                actionManager.HandleUnitClick(unit);
                return;
            }

            // Only handle unit selection if we're not in targeting mode and it's a player unit
            if (unit != null && unit.IsPlayerControlled)
            {
                SmartLogger.Log($"[UnitSelectionController] Selecting player unit: {unit.GetUnitName()}", LogCategory.Ability);
                SelectUnit(unit);
            }
        }

        private void HandleGroundClick(Interfaces.GridPosition gridPos)
        {
            if (isTargetingAbility)
            {
                actionManager.HandleGroundClick(gridPos.ToVector2Int());
            }
        }

        private void HandleUnitSelected(DokkaebiUnit unit)
        {
            SmartLogger.Log($"[UnitSelectionController] HandleUnitSelected called for unit: {unit?.GetUnitName()}, isTargetingAbility: {isTargetingAbility}", LogCategory.Ability);
            
            // Ignore unit selection events during ability targeting
            if (isTargetingAbility)
            {
                SmartLogger.Log("[UnitSelectionController] Ignoring unit selection during ability targeting", LogCategory.Ability);
                return;
            }

            if (unit != null && unit.IsPlayerControlled)
            {
                SelectUnit(unit);
            }
        }

        private void HandleUnitDeselected()
        {
            // Debug.Log("[UnitSelectionController] Attempting to set selected unit to NULL in UnitManager via HandleUnitDeselected.");
            unitManager.SetSelectedUnit(null);
            selectedUnit = null;
            HideMoveTargets();
            HideAbilityTargets();

            // Update UI
            if (abilityUI != null)
            {
                abilityUI.SetUnit(null);
            }
            
            // Clear unit info panel
            if (unitInfoPanel != null)
            {
                unitInfoPanel.SetUnit(null);
            }

            SmartLogger.Log("Handled unit deselected", LogCategory.Unit);
        }

        private void HandleGridCoordHovered(Vector2Int? gridCoord)
        {
            if (previewManager == null) return;

            if (gridCoord.HasValue)
            {
                // Update preview based on current mode (movement or ability targeting)
                previewManager.UpdatePreview(gridCoord.Value);
            }
            else
            {
                // Clear preview when not hovering over grid
                previewManager.UpdatePreview(new Vector2Int(-1, -1));
            }
        }

        private void SelectUnit(DokkaebiUnit unit)
        {
            if (unit == null) return;

            // Update selection state
            selectedUnit = unit;
            unitManager.SetSelectedUnit(unit);

            // Update UI
            if (abilityUI != null)
            {
                abilityUI.SetUnit(unit);
            }
            
            // Update unit info panel
            if (unitInfoPanel != null)
            {
                unitInfoPanel.SetUnit(unit);
            }

            // Show valid move targets
            ShowMoveTargets();

            SmartLogger.Log($"Selected unit: {unit.GetUnitName()}", LogCategory.Unit);
        }

        private void ShowMoveTargets()
        {
            HideMoveTargets();

            if (selectedUnit == null || selectedUnit.GetComponent<DokkaebiMovementHandler>() == null)
                return;

            var movementHandler = selectedUnit.GetComponent<DokkaebiMovementHandler>();
            var validMovePositions = movementHandler.GetValidMovePositions();

            foreach (var pos in validMovePositions)
            {
                if (gridManager.IsValidGridPosition(pos))
                {
                    var marker = Instantiate(moveTargetMarker, gridManager.GridToWorldPosition(pos), Quaternion.identity);
                    marker.GetComponent<Renderer>().material = validMoveMaterial;
                    moveTargetMarkers.Add(marker);
                }
            }
        }

        private void HideMoveTargets()
        {
            // Clean up move target markers
            foreach (var marker in moveTargetMarkers)
            {
                if (marker != null)
                {
                    Destroy(marker);
                }
            }
            moveTargetMarkers.Clear();
        }

        private void HandleAbilityTargetingStarted(AbilityData ability)
        {
            SmartLogger.Log($"[UnitSelectionController] Ability targeting started: {ability?.displayName}", LogCategory.Ability);
            isTargetingAbility = true;
            selectedAbility = ability;
            // Clear any existing selection visuals
            HideMoveTargets();
            // Show ability targeting visuals if needed
            ShowAbilityTargets();
        }

        private void HandleAbilityTargetingCancelled()
        {
            SmartLogger.Log($"[UnitSelectionController] Ability targeting cancelled", LogCategory.Ability);
            isTargetingAbility = false;
            selectedAbility = null;
            HideAbilityTargets();
            // Restore selection visuals if needed
            if (selectedUnit != null)
            {
                ShowMoveTargets();
            }
        }

        private void ShowAbilityTargets()
        {
            if (selectedAbility == null) return;

            var selectedUnit = unitManager.GetSelectedUnit();
            if (selectedUnit == null) return;

            // Clear any existing markers first
            HideAbilityTargets();

            // Get valid ability targets
            validAbilityTargets = GetValidAbilityTargets(selectedUnit, selectedAbility);

            // Show ability target markers
            foreach (var pos in validAbilityTargets)
            {
                var worldPos = gridManager.GridToWorldPosition(pos);
                var marker = Instantiate(abilityTargetMarker, worldPos, Quaternion.identity);
                marker.GetComponent<Renderer>().material = validAbilityTargetMaterial;
                abilityTargetMarkers.Add(marker);
            }
        }

        private void HideAbilityTargets()
        {
            // Clean up ability target markers
            foreach (var marker in abilityTargetMarkers)
            {
                if (marker != null)
                {
                    Destroy(marker);
                }
            }
            abilityTargetMarkers.Clear();
        }

        private HashSet<Interfaces.GridPosition> GetValidAbilityTargets(DokkaebiUnit unit, AbilityData ability)
        {
            var validTargets = new HashSet<Interfaces.GridPosition>();
            var currentPos = unit.GetGridPosition();
            var range = ability.range;

            // Get all positions within ability range
            for (int x = -range; x <= range; x++)
            {
                for (int z = -range; z <= range; z++)
                {
                    var pos = new Interfaces.GridPosition(currentPos.x + x, currentPos.z + z);
                    if (!gridManager.IsValidGridPosition(pos)) continue;

                    // Check if position is valid based on ability targeting rules
                    bool isValid = false;

                    // Check ground targeting
                    if (ability.targetsGround)
                    {
                        isValid = true;
                    }

                    // Check unit targeting
                    var unitsAtPos = unitManager.GetUnitsAtPosition(pos.ToVector2Int());
                    foreach (var targetUnit in unitsAtPos)
                    {
                        if (ability.targetsSelf && targetUnit == unit)
                        {
                            isValid = true;
                            break;
                        }
                        if (ability.targetsAlly && targetUnit.IsPlayerControlled == unit.IsPlayerControlled)
                        {
                            isValid = true;
                            break;
                        }
                        if (ability.targetsEnemy && targetUnit.IsPlayerControlled != unit.IsPlayerControlled)
                        {
                            isValid = true;
                            break;
                        }
                    }

                    if (isValid)
                    {
                        validTargets.Add(pos);
                    }
                }
            }

            return validTargets;
        }
    }
}
