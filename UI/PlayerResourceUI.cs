using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Dokkaebi.Core;
using Dokkaebi.Units;
using Dokkaebi.Common;
using Dokkaebi.Interfaces;

namespace Dokkaebi.UI
{
    public class PlayerResourceUI : MonoBehaviour
    {
        [Header("Aura Display")]
        [SerializeField] private Slider auraSlider;
        [SerializeField] private TextMeshProUGUI auraText;
        [SerializeField] private Image auraSliderFill;
        [SerializeField] private Color fullAuraColor = Color.blue;
        [SerializeField] private Color lowAuraColor = Color.red;

        [Header("Ability Usage")]
        [SerializeField] private TextMeshProUGUI abilityUsageText;
        [SerializeField] private Slider abilityUsageSlider;
        [SerializeField] private Image abilityUsageSliderFill;
        [SerializeField] private Color availableAbilityColor = Color.green;
        [SerializeField] private Color usedAbilityColor = Color.gray;

        [Header("Aura Gain Display")]
        [SerializeField] private GameObject auraGainDisplay;

        private UnitStateManager unitStateManager;
        private DokkaebiTurnSystemCore turnSystem;
        private int currentAbilityUsage = 0;
        private int maxAbilitiesPerPhase = 2;

        private void Awake()
        {
            unitStateManager = FindObjectOfType<UnitStateManager>();
            turnSystem = FindObjectOfType<DokkaebiTurnSystemCore>();

            if (unitStateManager == null || turnSystem == null)
            {
                Debug.LogError("Required managers not found in scene!");
                return;
            }
        }

        private void OnEnable()
        {
            // Subscribe to turn system events
            if (turnSystem != null)
            {
                turnSystem.OnPhaseChanged += HandlePhaseChanged;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            if (turnSystem != null)
            {
                turnSystem.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void HandlePhaseChanged(TurnPhase phase)
        {
            // Only show aura gain in aura phases
            bool isAuraPhase = phase == TurnPhase.AuraPhase1A || 
                             phase == TurnPhase.AuraPhase1B || 
                             phase == TurnPhase.AuraPhase2A || 
                             phase == TurnPhase.AuraPhase2B;
            
            auraGainDisplay.SetActive(isAuraPhase);
        }

        private void UpdateAuraDisplay(int currentAura)
        {
            if (auraSlider != null)
            {
                auraSlider.value = (float)currentAura / 100; // Assuming max aura is 100
            }

            if (auraText != null)
            {
                auraText.text = $"{currentAura}/100";
            }

            if (auraSliderFill != null)
            {
                // Update color based on Aura amount
                float auraPercentage = (float)currentAura / 100;
                auraSliderFill.color = Color.Lerp(lowAuraColor, fullAuraColor, auraPercentage);
            }
        }

        private void UpdateAbilityUsageDisplay(int used, int total)
        {
            if (abilityUsageText != null)
            {
                abilityUsageText.text = $"Abilities Used: {used}/{total}";
            }

            if (abilityUsageSlider != null)
            {
                abilityUsageSlider.value = (float)used / total;
            }

            if (abilityUsageSliderFill != null)
            {
                // Update color based on remaining abilities
                float remainingPercentage = 1f - ((float)used / total);
                abilityUsageSliderFill.color = Color.Lerp(usedAbilityColor, availableAbilityColor, remainingPercentage);
            }
        }
    }
} 