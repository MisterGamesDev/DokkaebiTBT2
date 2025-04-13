using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Dokkaebi.Units;
using Dokkaebi.Core.Data;
using Dokkaebi.Core;
using Dokkaebi.Grid;
using Dokkaebi.Interfaces;

namespace Dokkaebi.UI
{
    public class AbilitySelectionUI : MonoBehaviour
    {
        [System.Serializable]
        public class AbilityButton
        {
            public Button button;
            public Image icon;
            public Image cooldownOverlay;
            public TextMeshProUGUI cooldownText;
            public TextMeshProUGUI auraCostText;
            public int abilityIndex;
        }

        [Header("References")]
        [SerializeField] private DokkaebiTurnSystemCore turnSystem;
        [SerializeField] private AbilityButton[] abilityButtons;
        [SerializeField] private GameObject tooltipPrefab;
        [SerializeField] private GameObject targetingIndicator;
        [SerializeField] private TextMeshProUGUI targetingInstructionsText;

        [Header("Button Visuals")]
        [SerializeField] private Color enabledColor = Color.white;
        [SerializeField] private Color disabledColor = Color.gray;
        [SerializeField] private Vector2 tooltipOffset = new Vector2(10f, 10f);

        private DokkaebiUnit currentUnit;
        private GameObject currentTooltip;
        private AbilityButton hoveredButton;
        private PlayerActionManager playerActionManager;
        private bool isTargeting = false;

        private void Awake()
        {
            playerActionManager = PlayerActionManager.Instance;
            if (playerActionManager == null)
            {
                Debug.LogError("PlayerActionManager not found in scene!");
                return;
            }

            if (turnSystem == null)
            {
                turnSystem = FindObjectOfType<DokkaebiTurnSystemCore>();
                if (turnSystem == null)
                {
                    Debug.LogError("DokkaebiTurnSystemCore not found in scene!");
                }
            }

            // Subscribe to events
            playerActionManager.OnCommandResult += HandleCommandResult;
            playerActionManager.OnAbilityTargetingStarted += HandleAbilityTargetingStarted;
            playerActionManager.OnAbilityTargetingCancelled += HandleAbilityTargetingCancelled;
        }

        private void OnEnable()
        {
            if (currentUnit != null)
            {
                UpdateAbilityButtons();
            }
        }

        private void OnDisable()
        {
            HideTooltip();
            if (isTargeting)
            {
                playerActionManager.CancelAbilityTargeting();
            }
        }

        private void OnDestroy()
        {
            if (playerActionManager != null)
            {
                playerActionManager.OnCommandResult -= HandleCommandResult;
                playerActionManager.OnAbilityTargetingStarted -= HandleAbilityTargetingStarted;
                playerActionManager.OnAbilityTargetingCancelled -= HandleAbilityTargetingCancelled;
            }
        }

        private void Update()
        {
            // Cancel targeting if escape is pressed
            if (isTargeting && Input.GetKeyDown(KeyCode.Escape))
            {
                playerActionManager.CancelAbilityTargeting();
            }
        }

        public void SetUnit(DokkaebiUnit unit)
        {
            currentUnit = unit;
            UpdateAbilityButtons();
        }

        private void UpdateAbilityButtons()
        {
            Debug.Log($"[AbilitySelectionUI] UpdateAbilityButtons called. Current unit: {(currentUnit ? currentUnit.name : "NULL")}");
            
            if (currentUnit == null)
            {
                Debug.Log("[AbilitySelectionUI] No unit selected, hiding all ability buttons");
                // Hide all ability buttons when no unit is selected
                if (abilityButtons != null)
                {
                    foreach (var button in abilityButtons)
                    {
                        if (button?.button?.gameObject != null)
                        {
                            button.button.gameObject.SetActive(false);
                        }
                    }
                }
                return;
            }

            var abilities = currentUnit.GetAbilities();
            Debug.Log($"[AbilitySelectionUI] Unit {currentUnit.name} has {(abilities == null ? "NULL" : abilities.Count.ToString())} abilities");
            
            if (abilities == null)
            {
                Debug.LogError($"UpdateAbilityButtons: GetAbilities() returned null for unit {currentUnit.GetUnitName()}");
                return;
            }

            if (abilityButtons == null)
            {
                Debug.LogError("UpdateAbilityButtons: abilityButtons array is null");
                return;
            }

            for (int i = 0; i < abilityButtons.Length; i++)
            {
                var button = abilityButtons[i];
                if (button == null)
                {
                    Debug.LogError($"UpdateAbilityButtons: abilityButtons[{i}] is null");
                    continue;
                }

                if (button.button == null)
                {
                    Debug.LogError($"UpdateAbilityButtons: abilityButtons[{i}].button is null");
                    continue;
                }

                if (i < abilities.Count)
                {
                    AbilityData ability = abilities[i];
                    
                    // Add null check for ability
                    if (ability == null)
                    {
                        Debug.LogError($"Null ability found at index {i} for unit {currentUnit.GetUnitName()}");
                        button.button.gameObject.SetActive(false);
                        continue;
                    }

                    Debug.Log($"[AbilitySelectionUI] Processing ability button {i}: {ability.displayName}");
                    button.button.gameObject.SetActive(true);
                    
                    // Add null check for button.icon
                    if (button.icon != null)
                    {
                        button.icon.sprite = ability.icon;
                        Debug.Log($"[AbilitySelectionUI] Set icon for {ability.displayName}: {(ability.icon != null ? ability.icon.name : "NULL")}");
                    }
                    else
                    {
                        Debug.LogWarning($"Button icon component is missing for ability button at index {i}");
                    }

                    button.abilityIndex = i;

                    // Update button visuals based on state
                    bool isOnCooldown = currentUnit.IsOnCooldown(ability.abilityType);
                    bool hasEnoughAura = currentUnit.HasEnoughUnitAura(ability.auraCost);
                    bool canUseAura = turnSystem.CanUnitUseAura(currentUnit);

                    // Update button state
                    button.button.interactable = !isOnCooldown && hasEnoughAura && canUseAura;

                    // Update button colors
                    var buttonImage = button.button.GetComponent<UnityEngine.UI.Image>();
                    buttonImage.color = !isOnCooldown && hasEnoughAura && canUseAura ? enabledColor : disabledColor;
                    
                    // Show cooldown overlay and text
                    if (button.cooldownOverlay == null || button.cooldownText == null)
                    {
                        Debug.LogError($"UpdateAbilityButtons: cooldownOverlay or cooldownText is null for button {i}");
                        continue;
                    }

                    if (isOnCooldown)
                    {
                        // Show cooldown state
                        button.cooldownOverlay.gameObject.SetActive(true);
                        button.cooldownText.text = "X"; // X to indicate cooldown
                        button.cooldownOverlay.color = new Color(0.8f, 0.2f, 0.2f, 0.5f); // Red tint
                    }
                    else if (!hasEnoughAura)
                    {
                        // Show insufficient aura state
                        button.cooldownOverlay.gameObject.SetActive(true);
                        button.cooldownText.text = "X"; // X to indicate insufficient aura
                        button.cooldownOverlay.color = new Color(0.8f, 0.2f, 0.2f, 0.5f); // Red tint
                    }
                    else
                    {
                        // Ability is ready to use
                        button.cooldownOverlay.gameObject.SetActive(false);
                    }
                    
                    // Update aura cost text
                    if (button.auraCostText != null)
                    {
                        button.auraCostText.text = ability.auraCost.ToString();
                    }
                    else
                    {
                        Debug.LogError($"UpdateAbilityButtons: auraCostText is null for button {i}");
                    }

                    // Add click handler
                    button.button.onClick.RemoveAllListeners();
                    int index = i; // Capture for lambda
                    button.button.onClick.AddListener(() => HandleAbilityButtonClick(index));

                    // Add hover handlers
                    var eventTrigger = button.button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                    if (eventTrigger == null)
                    {
                        eventTrigger = button.button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                    }

                    // Clear existing triggers
                    eventTrigger.triggers.Clear();

                    // Add enter trigger
                    var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
                    enterEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
                    enterEntry.callback.AddListener((data) => ShowTooltip(ability, button));
                    eventTrigger.triggers.Add(enterEntry);

                    // Add exit trigger
                    var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
                    exitEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
                    exitEntry.callback.AddListener((data) => HideTooltip());
                    eventTrigger.triggers.Add(exitEntry);
                }
                else
                {
                    button.button.gameObject.SetActive(false);
                }
            }
        }

        private bool CanUseAbility(AbilityData ability)
        {
            if (currentUnit == null) return false;

            // Check cooldown
            if (currentUnit.IsOnCooldown(ability.abilityType))
            {
                return false;
            }

            // Check unit-specific aura cost
            if (!currentUnit.HasEnoughUnitAura(ability.auraCost))
            {
                return false;
            }

            return true;
        }

        private void HandleAbilityButtonClick(int abilityIndex)
        {
            Debug.Log($"[AbilitySelectionUI] Ability button clicked with index: {abilityIndex}");
            if (currentUnit == null) return;

            var abilities = currentUnit.GetAbilities();
            if (abilityIndex >= 0 && abilityIndex < abilities.Count)
            {
                AbilityData ability = abilities[abilityIndex];
                if (CanUseAbility(ability))
                {
                    // Start targeting mode
                    playerActionManager.StartAbilityTargeting(currentUnit, abilityIndex);
                }
            }
        }

        private void HandleAbilityTargetingStarted(AbilityData ability)
        {
            isTargeting = true;
            if (targetingIndicator != null)
            {
                targetingIndicator.SetActive(true);
            }
            if (targetingInstructionsText != null)
            {
                string instructions = GetTargetingInstructions(ability);
                targetingInstructionsText.text = instructions;
                targetingInstructionsText.gameObject.SetActive(true);
            }
        }

        private void HandleAbilityTargetingCancelled()
        {
            isTargeting = false;
            if (targetingIndicator != null)
            {
                targetingIndicator.SetActive(false);
            }
            if (targetingInstructionsText != null)
            {
                targetingInstructionsText.gameObject.SetActive(false);
            }
        }

        private string GetTargetingInstructions(AbilityData ability)
        {
            if (ability == null) return string.Empty;

            string targetTypes = "";
            if (ability.targetsSelf) targetTypes += "self, ";
            if (ability.targetsAlly) targetTypes += "allies, ";
            if (ability.targetsEnemy) targetTypes += "enemies, ";
            if (ability.targetsGround) targetTypes += "ground, ";
            
            // Remove trailing comma and space
            if (targetTypes.Length > 2)
            {
                targetTypes = targetTypes.Substring(0, targetTypes.Length - 2);
            }

            string instructions = $"Select target for {ability.displayName}\n";
            instructions += $"Range: {ability.range} tiles\n";
            if (ability.areaOfEffect > 0)
            {
                instructions += $"Area: {ability.areaOfEffect} tiles\n";
            }
            instructions += $"Can target: {targetTypes}\n";
            instructions += "Press ESC to cancel";

            return instructions;
        }

        private void HandleCommandResult(bool success, string message)
        {
            if (success)
            {
                // Clear targeting UI
                HandleAbilityTargetingCancelled();
                
                // Update ability buttons to reflect new cooldowns/costs
                UpdateAbilityButtons();
            }
            else
            {
                // Show error message
                Debug.LogWarning($"Ability command failed: {message}");
            }
        }

        private void ShowTooltip(AbilityData ability, AbilityButton button)
        {
            if (tooltipPrefab == null) return;

            hoveredButton = button;
            if (currentTooltip == null)
            {
                currentTooltip = Instantiate(tooltipPrefab, transform);
            }

            var tooltipContent = currentTooltip.GetComponent<AbilityTooltipContent>();
            if (tooltipContent != null)
            {
                tooltipContent.UpdateContent(ability);
            }

            // Position tooltip
            RectTransform buttonRect = button.button.GetComponent<RectTransform>();
            RectTransform tooltipRect = currentTooltip.GetComponent<RectTransform>();
            tooltipRect.position = buttonRect.position + (Vector3)tooltipOffset;
        }

        private void HideTooltip()
        {
            if (currentTooltip != null)
            {
                currentTooltip.SetActive(false);
            }
            hoveredButton = null;
        }
    }
} 
