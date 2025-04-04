using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Dokkaebi.Core.Data;

namespace Dokkaebi.UI
{
    public class AbilityTooltipContent : MonoBehaviour
    {
        [Header("Basic Info")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI auraCostText;
        [SerializeField] private TextMeshProUGUI cooldownText;

        [Header("Targeting Info")]
        [SerializeField] private TextMeshProUGUI targetTypeText;
        [SerializeField] private TextMeshProUGUI rangeText;
        [SerializeField] private TextMeshProUGUI areaText;

        [Header("Effects")]
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private TextMeshProUGUI healingText;
        [SerializeField] private TextMeshProUGUI statusEffectsText;

        public void UpdateContent(AbilityData ability)
        {
            if (ability == null) return;

            // Update basic info
            if (nameText != null)
            {
                nameText.text = ability.displayName;
            }

            if (descriptionText != null)
            {
                descriptionText.text = ability.description;
            }

            if (auraCostText != null)
            {
                auraCostText.text = $"Aura Cost: {ability.auraCost}";
            }

            if (cooldownText != null)
            {
                cooldownText.text = $"Cooldown: {ability.cooldownTurns} turns";
            }

            // Update targeting info
            if (targetTypeText != null)
            {
                string targetTypes = "";
                if (ability.targetsSelf) targetTypes += "Self, ";
                if (ability.targetsAlly) targetTypes += "Ally, ";
                if (ability.targetsEnemy) targetTypes += "Enemy, ";
                if (ability.targetsGround) targetTypes += "Ground, ";
                targetTypes = targetTypes.TrimEnd(',', ' ');
                targetTypeText.text = $"Target Type: {targetTypes}";
            }

            if (rangeText != null)
            {
                rangeText.text = $"Range: {ability.range}";
            }

            if (areaText != null)
            {
                areaText.text = $"Area: {ability.areaOfEffect}";
            }

            // Update effects
            if (damageText != null)
            {
                damageText.text = $"Damage: {ability.damageAmount}";
                damageText.gameObject.SetActive(ability.damageAmount > 0);
            }

            if (healingText != null)
            {
                healingText.text = $"Healing: {ability.healAmount}";
                healingText.gameObject.SetActive(ability.healAmount > 0);
            }

            if (statusEffectsText != null)
            {
                if (ability.appliedEffects != null && ability.appliedEffects.Count > 0)
                {
                    string effects = string.Join(", ", ability.appliedEffects);
                    statusEffectsText.text = $"Status Effects: {effects}";
                    statusEffectsText.gameObject.SetActive(true);
                }
                else
                {
                    statusEffectsText.gameObject.SetActive(false);
                }
            }
        }
    }
} 