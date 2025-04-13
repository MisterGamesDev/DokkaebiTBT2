using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using Dokkaebi.Core;
using Dokkaebi.Core.Data;
using Dokkaebi.Units;
using Dokkaebi.Grid;
using Dokkaebi.Interfaces;
using Dokkaebi.Common;
using Dokkaebi.Utilities;

namespace Dokkaebi.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject unitInfoPanel;
        [SerializeField] private GameObject abilitySelectionPanel;
        [SerializeField] private GameObject turnPhasePanel;
        [SerializeField] private GameObject playerResourcePanel;

        [Header("References")]
        [SerializeField] private TextMeshProUGUI turnPhaseText;
        [SerializeField] private TextMeshProUGUI turnNumberText;
        [SerializeField] private TextMeshProUGUI activePlayerText;
        [SerializeField] private TextMeshProUGUI auraText;
        [SerializeField] private TextMeshProUGUI abilityUsageText;
        [SerializeField] private DokkaebiTurnSystemCore turnSystem;

        [Header("Hover Feedback")]
        [SerializeField] private GameObject hoverHighlightPrefab;
        [SerializeField] private Color validTargetColor = Color.green;
        [SerializeField] private Color invalidTargetColor = Color.red;
        [SerializeField] private float hoverHighlightScale = 1.1f;
        [SerializeField] private float hoverTransitionDuration = 0.2f;

        private GameObject currentHoverHighlight;
        private Dokkaebi.Grid.GridManager gridManager;
        private InputManager inputManager;
        private Dictionary<GameObject, Vector3> originalScales = new Dictionary<GameObject, Vector3>();
        private Dictionary<GameObject, Coroutine> activeTransitions = new Dictionary<GameObject, Coroutine>();

        private void Awake()
        {
            gridManager = FindObjectOfType<Dokkaebi.Grid.GridManager>();
            inputManager = FindObjectOfType<InputManager>();
            if (turnSystem == null) turnSystem = FindObjectOfType<DokkaebiTurnSystemCore>();

            if (gridManager == null || inputManager == null || turnSystem == null)
            {
                SmartLogger.LogError("Required managers not found in scene!", LogCategory.UI, this);
                return;
            }

            // Subscribe to hover events
            inputManager.OnGridCoordHovered += HandleGridHover;
            inputManager.OnUnitHovered += HandleUnitHover;
        }

        private void OnEnable()
        {
            // Subscribe to core game events
            if (turnSystem != null)
            {
                turnSystem.OnPhaseChanged += HandlePhaseStart;
            }
            PlayerActionManager.Instance.OnCommandResult += HandleCommandResult;
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            if (turnSystem != null)
            {
                turnSystem.OnPhaseChanged -= HandlePhaseStart;
            }
            PlayerActionManager.Instance.OnCommandResult -= HandleCommandResult;

            // Unsubscribe from hover events
            if (inputManager != null)
            {
                inputManager.OnGridCoordHovered -= HandleGridHover;
                inputManager.OnUnitHovered -= HandleUnitHover;
            }

            // Clean up hover effects
            ClearHoverEffects();
        }

        private void HandlePhaseStart(TurnPhase phase)
        {
            UpdateTurnPhaseUI(phase);
        }

        private void HandleCommandResult(bool success, string message)
        {
            // Handle command results here
            // For example, show success/failure messages
            SmartLogger.Log($"Command result: {success} - {message}", LogCategory.UI, this);
        }

        private void UpdateTurnPhaseUI(TurnPhase phase)
        {
            if (turnPhaseText != null)
            {
                turnPhaseText.text = phase.ToString();
            }
        }

        private void ShowUnitInfo(DokkaebiUnit unit)
        {
            if (unitInfoPanel != null)
            {
                unitInfoPanel.SetActive(true);
                var unitInfoPanelComponent = unitInfoPanel.GetComponent<UnitInfoPanel>();
                if (unitInfoPanelComponent != null)
                {
                    unitInfoPanelComponent.SetUnit(unit);
                }
            }
        }

        private void HideUnitInfo()
        {
            if (unitInfoPanel != null)
            {
                unitInfoPanel.SetActive(false);
            }
        }

        private void ShowAbilitySelection(DokkaebiUnit unit)
        {
            if (abilitySelectionPanel != null)
            {
                abilitySelectionPanel.SetActive(true);
                var abilitySelectionUI = abilitySelectionPanel.GetComponent<AbilitySelectionUI>();
                if (abilitySelectionUI != null)
                {
                    abilitySelectionUI.SetUnit(unit);
                }
            }
        }

        private void HideAbilitySelection()
        {
            if (abilitySelectionPanel != null)
            {
                abilitySelectionPanel.SetActive(false);
            }
        }

        private void UpdateAuraDisplay(int aura)
        {
            if (auraText != null)
            {
                auraText.text = $"Aura: {aura}";
            }
        }

        private void UpdateAbilityUsageDisplay(int used, int total)
        {
            if (abilityUsageText != null)
            {
                abilityUsageText.text = $"Abilities Used: {used}/{total}";
            }
        }

        private void HandleGridHover(Vector2Int? gridCoord)
        {
            if (!gridCoord.HasValue)
            {
                ClearHoverEffects();
                return;
            }

            // Check if the hovered tile is valid for current action
            bool isValidTarget = IsValidGridTarget(gridCoord.Value);
            UpdateHoverHighlight(gridCoord.Value, isValidTarget);
        }

        private void HandleUnitHover(DokkaebiUnit unit)
        {
            if (unit == null)
            {
                ClearHoverEffects();
                return;
            }

            // Check if the hovered unit is valid for current action
            bool isValidTarget = IsValidUnitTarget(unit);
            UpdateUnitHoverEffect(unit, isValidTarget);
        }

        private bool IsValidGridTarget(Vector2Int gridCoord)
        {
            // Implement validation logic based on current game state
            // For example, check if the tile is within movement range or ability range
            return true; // Placeholder
        }

        private bool IsValidUnitTarget(DokkaebiUnit unit)
        {
            // Implement validation logic based on current game state
            // For example, check if the unit is a valid target for the current ability
            return true; // Placeholder
        }

        private void UpdateHoverHighlight(Vector2Int gridCoord, bool isValid)
        {
            if (currentHoverHighlight == null && hoverHighlightPrefab != null)
            {
                currentHoverHighlight = Instantiate(hoverHighlightPrefab);
            }

            if (currentHoverHighlight != null)
            {
                // Convert Vector2Int to GridPosition for GridToWorldPosition
                var gridPos = new GridPosition(gridCoord.x, gridCoord.y);
                Vector3 worldPos = gridManager.GridToWorldPosition(gridPos);
                currentHoverHighlight.transform.position = worldPos;

                // Update color based on validity
                var renderer = currentHoverHighlight.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = isValid ? validTargetColor : invalidTargetColor;
                }
            }
        }

        private void UpdateUnitHoverEffect(DokkaebiUnit unit, bool isValid)
        {
            GameObject unitObject = unit.gameObject;
            
            // Store original scale if not already stored
            if (!originalScales.ContainsKey(unitObject))
            {
                originalScales[unitObject] = unitObject.transform.localScale;
            }

            // Start scale transition
            Vector3 targetScale = originalScales[unitObject] * hoverHighlightScale;
            StartScaleTransition(unitObject, targetScale);

            // Update unit's outline or highlight effect based on validity
            var outline = unitObject.GetComponent<Outline>();
            if (outline == null)
            {
                outline = unitObject.AddComponent<Outline>();
            }
            outline.effectColor = isValid ? validTargetColor : invalidTargetColor;
            outline.enabled = true;
        }

        private void StartScaleTransition(GameObject target, Vector3 targetScale)
        {
            // Cancel existing transition if any
            if (activeTransitions.ContainsKey(target))
            {
                StopCoroutine(activeTransitions[target]);
                activeTransitions.Remove(target);
            }

            // Start new transition
            var transition = StartCoroutine(ScaleTransition(target, targetScale));
            activeTransitions[target] = transition;
        }

        private System.Collections.IEnumerator ScaleTransition(GameObject target, Vector3 targetScale)
        {
            Vector3 startScale = target.transform.localScale;
            float elapsedTime = 0f;

            while (elapsedTime < hoverTransitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / hoverTransitionDuration;
                target.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            target.transform.localScale = targetScale;
            activeTransitions.Remove(target);
        }

        private void ClearHoverEffects()
        {
            // Clear grid highlight
            if (currentHoverHighlight != null)
            {
                Destroy(currentHoverHighlight);
                currentHoverHighlight = null;
            }

            // Reset unit scales and remove outlines
            foreach (var kvp in originalScales)
            {
                GameObject unitObject = kvp.Key;
                if (unitObject != null)
                {
                    // Cancel any active transitions
                    if (activeTransitions.ContainsKey(unitObject))
                    {
                        StopCoroutine(activeTransitions[unitObject]);
                        activeTransitions.Remove(unitObject);
                    }

                    // Reset scale
                    unitObject.transform.localScale = kvp.Value;

                    // Remove outline
                    var outline = unitObject.GetComponent<Outline>();
                    if (outline != null)
                    {
                        outline.enabled = false;
                    }
                }
            }

            originalScales.Clear();
        }
    }
} 
