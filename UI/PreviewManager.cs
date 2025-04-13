using UnityEngine;
using System.Collections.Generic;
using Dokkaebi.Units;
using Dokkaebi.Core.Data;
using Dokkaebi.Grid;
using Dokkaebi.Pathfinding;
using Dokkaebi.Core;
using Dokkaebi.Interfaces;
using Dokkaebi.Common;
using Dokkaebi.Utilities;

namespace Dokkaebi.UI
{
    public class PreviewManager : MonoBehaviour
    {
        [Header("Movement Preview")]
        [SerializeField] private LineRenderer movementLine;
        [SerializeField] private Material movementLineMaterial;
        [SerializeField] private float lineWidth = 0.1f;
        [SerializeField] private Color validPathColor = Color.green;
        [SerializeField] private Color invalidPathColor = Color.red;

        [Header("Ability Preview")]
        [SerializeField] private GameObject tileHighlightPrefab;
        [SerializeField] private Material validTargetMaterial;
        [SerializeField] private Material invalidTargetMaterial;
        [SerializeField] private float highlightHeight = 0.1f;

        private Dictionary<Vector2Int, GameObject> activeHighlights = new Dictionary<Vector2Int, GameObject>();
        private DokkaebiUnit selectedUnit;
        private AbilityData selectedAbility;
        private bool isAbilityTargetingMode = false;
        private PlayerActionManager playerActionManager;
        private UnitManager unitManager;

        private void Awake()
        {
            playerActionManager = PlayerActionManager.Instance;
            unitManager = UnitManager.Instance;
            if (playerActionManager == null)
            {
                SmartLogger.LogError("PlayerActionManager not found in scene!", LogCategory.UI);
                return;
            }
            if (unitManager == null)
            {
                SmartLogger.LogError("UnitManager not found in scene!", LogCategory.UI);
                return;
            }
        }

        private void OnEnable()
        {
            // Subscribe to input events
            if (playerActionManager != null)
            {
                playerActionManager.OnCommandResult += HandleCommandResult;
                playerActionManager.OnAbilityTargetingStarted += HandleAbilityTargetingStarted;
                playerActionManager.OnAbilityTargetingCancelled += HandleAbilityTargetingCancelled;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            if (playerActionManager != null)
            {
                playerActionManager.OnCommandResult -= HandleCommandResult;
                playerActionManager.OnAbilityTargetingStarted -= HandleAbilityTargetingStarted;
                playerActionManager.OnAbilityTargetingCancelled -= HandleAbilityTargetingCancelled;
            }

            // Clean up
            ClearHighlights();
            if (movementLine != null)
            {
                movementLine.enabled = false;
            }
        }

        private void Start()
        {
            if (movementLine == null)
            {
                movementLine = gameObject.AddComponent<LineRenderer>();
                movementLine.material = movementLineMaterial;
                movementLine.startWidth = lineWidth;
                movementLine.endWidth = lineWidth;
                movementLine.useWorldSpace = true;
            }
        }

        private void HandleCommandResult(bool success, string message)
        {
            // Clear previews on command completion
            if (isAbilityTargetingMode)
            {
                ClearHighlights();
                isAbilityTargetingMode = false;
            }
            else
            {
                if (movementLine != null)
                {
                    movementLine.enabled = false;
                }
            }
        }

        private void HandleAbilityTargetingStarted(AbilityData ability)
        {
            selectedAbility = ability;
            selectedUnit = unitManager.GetSelectedUnit();
            isAbilityTargetingMode = true;

            // Clear any existing previews
            ClearHighlights();
            if (movementLine != null)
            {
                movementLine.enabled = false;
            }
        }

        private void HandleAbilityTargetingCancelled()
        {
            selectedAbility = null;
            isAbilityTargetingMode = false;
            ClearHighlights();
        }

        public void UpdatePreview(Vector2Int hoverPosition)
        {
            if (isAbilityTargetingMode)
            {
                UpdateAbilityPreview(hoverPosition);
            }
            else
            {
                UpdateMovementPreview(hoverPosition);
            }
        }

        private void UpdateMovementPreview(Vector2Int targetPos)
        {
            selectedUnit = unitManager.GetSelectedUnit();
            if (selectedUnit == null || !movementLine) return;

            // Convert Vector2Int to GridPosition
            GridPosition startPos = selectedUnit.GetGridPosition();
            GridPosition endPos = GridPosition.FromVector2Int(targetPos);

            // Get path from unit to target using GridManager
            var path = GridManager.Instance.FindPath(
                startPos,
                endPos,
                selectedUnit.GetMovementRange()
            );

            // Update line renderer
            if (path != null && path.Count > 0)
            {
                movementLine.positionCount = path.Count;
                for (int i = 0; i < path.Count; i++)
                {
                    Vector3 worldPos = GridManager.Instance.GridToWorldPosition(path[i]);
                    movementLine.SetPosition(i, worldPos + Vector3.up * highlightHeight);
                }

                // Set color based on path validity
                bool isValidPath = path.Count <= selectedUnit.GetMovementRange() + 1;
                Color pathColor = isValidPath ? validPathColor : invalidPathColor;
                movementLine.startColor = pathColor;
                movementLine.endColor = pathColor;
                movementLine.enabled = true;
            }
            else
            {
                movementLine.enabled = false;
            }
        }

        private void UpdateAbilityPreview(Vector2Int targetPos)
        {
            selectedUnit = unitManager.GetSelectedUnit();
            if (selectedUnit == null || selectedAbility == null) return;

            ClearHighlights();

            // Get affected tiles based on ability range and area
            var affectedTiles = GetAffectedTiles(targetPos, selectedAbility);

            // Create highlights for affected tiles
            foreach (var tile in affectedTiles)
            {
                bool isValidTarget = IsValidTarget(tile, selectedAbility);
                CreateTileHighlight(tile, isValidTarget);
            }
        }

        private List<Vector2Int> GetAffectedTiles(Vector2Int center, AbilityData ability)
        {
            var affectedTiles = new List<Vector2Int>();
            int range = ability.range;
            int area = ability.areaOfEffect;

            // Add center tile
            affectedTiles.Add(center);

            // Add tiles in area of effect
            for (int x = -area; x <= area; x++)
            {
                for (int y = -area; y <= area; y++)
                {
                    if (x == 0 && y == 0) continue; // Skip center tile

                    Vector2Int offset = new Vector2Int(x, y);
                    Vector2Int tilePos = center + offset;

                    // Check if tile is within range and grid bounds
                    if (Vector2Int.Distance(center, tilePos) <= area &&
                        GridManager.Instance.IsPositionValid(GridPosition.FromVector2Int(tilePos)))
                    {
                        affectedTiles.Add(tilePos);
                    }
                }
            }

            return affectedTiles;
        }

        private bool IsValidTarget(Vector2Int targetPos, AbilityData ability)
        {
            selectedUnit = unitManager.GetSelectedUnit();
            if (selectedUnit == null) return false;

            // Check if target is within range
            float distance = Vector2.Distance(selectedUnit.GetGridPosition().ToVector2Int(), targetPos);
            if (distance > ability.range)
            {
                return false;
            }

            // Get units at the target position
            var unitsAtPosition = UnitManager.Instance.GetUnitsAtPosition(targetPos);
            if (unitsAtPosition == null || unitsAtPosition.Count == 0)
            {
                // If no units at position, only valid if targeting ground
                return ability.targetsGround;
            }

            // Check each unit at the position
            foreach (var targetUnit in unitsAtPosition)
            {
                if (ability.targetsSelf && targetUnit == selectedUnit)
                    return true;
                if (ability.targetsAlly && targetUnit.IsPlayer() == selectedUnit.IsPlayer())
                    return true;
                if (ability.targetsEnemy && targetUnit.IsPlayer() != selectedUnit.IsPlayer())
                    return true;
            }

            // If we get here and targetsGround is true, it's valid
            return ability.targetsGround;
        }

        private void CreateTileHighlight(Vector2Int gridPos, bool isValid)
        {
            if (tileHighlightPrefab == null) return;

            GameObject highlight = Instantiate(tileHighlightPrefab, transform);
            highlight.transform.position = GridManager.Instance.GridToWorldPosition(GridPosition.FromVector2Int(gridPos)) + Vector3.up * highlightHeight;

            // Set material based on validity
            var renderer = highlight.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = isValid ? validTargetMaterial : invalidTargetMaterial;
            }

            activeHighlights[gridPos] = highlight;
        }

        private void ClearHighlights()
        {
            foreach (var highlight in activeHighlights.Values)
            {
                if (highlight != null)
                {
                    Destroy(highlight);
                }
            }
            activeHighlights.Clear();
        }
    }
} 
