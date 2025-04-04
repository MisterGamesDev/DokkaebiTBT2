using UnityEngine;
using System.Collections.Generic;
using Dokkaebi.Units;
using Dokkaebi.Core.Data;
using Dokkaebi.Grid;
using Dokkaebi.Pathfinding;
using Dokkaebi.Core;
using Dokkaebi.Interfaces;

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

        private void OnEnable()
        {
            // Subscribe to input events
            PlayerActionManager.Instance.OnCommandResult += HandleCommandResult;
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            PlayerActionManager.Instance.OnCommandResult -= HandleCommandResult;
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
            // Handle command results here
            // For example, show success/failure messages
            Debug.Log($"Command result: {success} - {message}");
        }

        private void UpdateMovementPreview(Vector2Int targetPos)
        {
            if (selectedUnit == null) return;

            // Convert Vector2Int to GridPosition
            GridPosition startPos = GridPosition.FromVector2Int(selectedUnit.GetGridPosition().ToVector2Int());
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
                    Vector3 worldPos = GridManager.Instance.GridToWorld(path[i]);
                    movementLine.SetPosition(i, worldPos + Vector3.up * highlightHeight);
                }

                // Set color based on path validity
                movementLine.startColor = validPathColor;
                movementLine.endColor = validPathColor;
                movementLine.enabled = true;
            }
            else
            {
                movementLine.enabled = false;
            }
        }

        private void UpdateAbilityPreview(Vector2Int targetPos)
        {
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

                    // Check if tile is within range
                    if (Vector2Int.Distance(center, tilePos) <= range)
                    {
                        affectedTiles.Add(tilePos);
                    }
                }
            }

            return affectedTiles;
        }

        private bool IsValidTarget(Vector2Int targetPos, AbilityData ability)
        {
            // Check if target is within range
            if (Vector2Int.Distance(selectedUnit.GetGridPosition().ToVector2Int(), targetPos) > ability.range)
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
            highlight.transform.position = GridManager.Instance.GridToWorld(GridPosition.FromVector2Int(gridPos)) + Vector3.up * highlightHeight;

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
            movementLine.enabled = false;
        }
    }
} 