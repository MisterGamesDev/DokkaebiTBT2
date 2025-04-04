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

        [SerializeField] private AbilityButton[] abilityButtons;
        [SerializeField] private GameObject tooltipPrefab;
        [SerializeField] private Vector2 tooltipOffset = new Vector2(10f, 10f);

        private DokkaebiUnit currentUnit;
        private GameObject currentTooltip;
        private AbilityButton hoveredButton;
        private PlayerActionManager playerActionManager;

        private void Awake()
        {
            playerActionManager = PlayerActionManager.Instance;
            if (playerActionManager == null)
            {
                Debug.LogError("PlayerActionManager not found in scene!");
                return;
            }

            // Subscribe to command result events
            playerActionManager.OnCommandResult += HandleCommandResult;
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
        }

        private void OnDestroy()
        {
            if (playerActionManager != null)
            {
                playerActionManager.OnCommandResult -= HandleCommandResult;
            }
        }

        public void SetUnit(DokkaebiUnit unit)
        {
            currentUnit = unit;
            UpdateAbilityButtons();
        }

        private void UpdateAbilityButtons()
        {
            if (currentUnit == null) return;

            var abilities = currentUnit.GetAbilities();
            for (int i = 0; i < abilityButtons.Length; i++)
            {
                var button = abilityButtons[i];
                if (i < abilities.Count)
                {
                    AbilityData ability = abilities[i];
                    button.button.gameObject.SetActive(true);
                    button.icon.sprite = ability.icon;
                    button.abilityIndex = i;

                    // Update cooldown
                    int cooldown = currentUnit.GetRemainingCooldown(ability.abilityType);
                    bool canUse = CanUseAbility(ability);
                    button.button.interactable = canUse;
                    button.cooldownOverlay.gameObject.SetActive(cooldown > 0);
                    button.cooldownText.text = cooldown.ToString();
                    button.auraCostText.text = ability.auraCost.ToString();

                    // Add click handler
                    button.button.onClick.RemoveAllListeners();
                    button.button.onClick.AddListener(() => HandleAbilityButtonClick(i));

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

            // Check aura cost
            if (currentUnit.GetCurrentAura() < ability.auraCost)
            {
                return false;
            }

            return true;
        }

        private void HandleAbilityButtonClick(int abilityIndex)
        {
            if (currentUnit == null) return;

            var abilities = currentUnit.GetAbilities();
            if (abilityIndex >= 0 && abilityIndex < abilities.Count)
            {
                AbilityData ability = abilities[abilityIndex];
                if (CanUseAbility(ability))
                {
                    // Convert GridPosition to Vector2Int for the command
                    GridPosition gridPos = currentUnit.GetGridPosition();
                    Vector2Int vectorPos = DokkaebiGridConverter.GridToVector2Int(gridPos);
                    
                    // Raise ability selection event through PlayerActionManager
                    playerActionManager.ExecuteAbilityCommand(currentUnit.GetUnitId(), abilityIndex, vectorPos);
                }
            }
        }

        private void HandleCommandResult(bool success, string message)
        {
            // Update UI based on command result
            if (!success)
            {
                // Show error message or visual feedback
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
                Destroy(currentTooltip);
                currentTooltip = null;
                hoveredButton = null;
            }
        }
    }
} 