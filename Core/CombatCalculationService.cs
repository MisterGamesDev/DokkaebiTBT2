using UnityEngine;
using Dokkaebi.Core.Data;
using Dokkaebi.Units;
using Dokkaebi.Common;
using Dokkaebi.Utilities;

namespace Dokkaebi.Core
{
    /// <summary>
    /// Static service class that handles combat-related calculations
    /// </summary>
    public static class CombatCalculationService
    {
        /// <summary>
        /// Calculate the final damage amount for an ability
        /// </summary>
        /// <param name="ability">The ability being used</param>
        /// <param name="source">The unit using the ability</param>
        /// <param name="target">The unit being targeted</param>
        /// <param name="isOverload">Whether this is an overload cast</param>
        /// <returns>The final calculated damage amount</returns>
        public static int CalculateFinalDamage(AbilityData ability, DokkaebiUnit source, DokkaebiUnit target, bool isOverload)
        {
            if (ability == null || source == null || target == null)
            {
                SmartLogger.LogError("[CombatCalculationService.CalculateFinalDamage] Invalid parameters provided", LogCategory.Ability);
                return 0;
            }

            int baseDamage = ability.damageAmount;
            
            // Apply overload multiplier if applicable
            if (isOverload && ability.hasOverloadVariant)
            {
                baseDamage = Mathf.RoundToInt(baseDamage * ability.overloadDamageMultiplier);
                SmartLogger.Log($"[CombatCalculationService.CalculateFinalDamage] Applied overload multiplier: {ability.overloadDamageMultiplier} to base damage: {ability.damageAmount}, result: {baseDamage}", LogCategory.Ability);
            }

            return baseDamage;
        }

        /// <summary>
        /// Calculate the final healing amount for an ability
        /// </summary>
        /// <param name="ability">The ability being used</param>
        /// <param name="source">The unit using the ability</param>
        /// <param name="target">The unit being targeted</param>
        /// <param name="isOverload">Whether this is an overload cast</param>
        /// <returns>The final calculated healing amount</returns>
        public static int CalculateFinalHealing(AbilityData ability, DokkaebiUnit source, DokkaebiUnit target, bool isOverload)
        {
            if (ability == null || source == null || target == null)
            {
                SmartLogger.LogError("[CombatCalculationService.CalculateFinalHealing] Invalid parameters provided", LogCategory.Ability);
                return 0;
            }

            int baseHealing = ability.healAmount;
            
            // Apply overload multiplier if applicable
            if (isOverload && ability.hasOverloadVariant)
            {
                baseHealing = Mathf.RoundToInt(baseHealing * ability.overloadHealMultiplier);
                SmartLogger.Log($"[CombatCalculationService.CalculateFinalHealing] Applied overload multiplier: {ability.overloadHealMultiplier} to base healing: {ability.healAmount}, result: {baseHealing}", LogCategory.Ability);
            }

            return baseHealing;
        }
    }
} 