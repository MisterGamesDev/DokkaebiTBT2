using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Dokkaebi.Core.Data;
using Dokkaebi.Interfaces;
using Dokkaebi.Units;
using Dokkaebi.Utilities;
using Dokkaebi.Common;

namespace Dokkaebi.Core
{
    /// <summary>
    /// Static system responsible for managing and updating status effects on units.
    /// </summary>
    public static class StatusEffectSystem
    {
        /// <summary>
        /// Apply a status effect to a unit
        /// </summary>
        public static void ApplyStatusEffect(IDokkaebiUnit targetUnit, StatusEffectData effectData, int duration = -1, IDokkaebiUnit sourceUnit = null)
        {
            if (targetUnit == null || effectData == null)
            {
                SmartLogger.LogWarning("Cannot apply null status effect or apply to null unit", LogCategory.General);
                return;
            }

            // Create new effect instance
            var newEffect = new StatusEffectInstance(
                effectData, 
                duration, 
                sourceUnit?.UnitId ?? -1
            );
            
            // Get all existing effects of the same type
            var existingEffects = targetUnit.GetStatusEffects()?
                .Where(e => e.StatusEffectType == effectData.effectType)
                .ToList() ?? new List<IStatusEffectInstance>();
            
            // Handle stacking logic
            if (effectData.isStackable)
            {
                // Check if we've reached max stacks
                if (existingEffects.Count >= effectData.maxStacks)
                {
                    // Find the oldest effect to potentially refresh
                    var oldestEffect = existingEffects
                        .OrderBy(e => e.RemainingDuration)
                        .FirstOrDefault();
                        
                    if (oldestEffect != null)
                    {
                        // Refresh the duration of the oldest stack
                        oldestEffect.RemainingDuration = duration >= 0 ? duration : effectData.duration;
                        SmartLogger.Log($"Max stacks reached for {effectData.displayName} on {targetUnit.DisplayName}, refreshed oldest stack duration", LogCategory.General);
                    }
                }
                else
                {
                    // Add new stack
                    targetUnit.AddStatusEffect(newEffect);
                    if (targetUnit is DokkaebiUnit concreteUnit)
                    {
                        concreteUnit.RaiseStatusEffectApplied(newEffect);
                    }
                    SmartLogger.Log($"Added new stack of {effectData.displayName} to {targetUnit.DisplayName} ({existingEffects.Count + 1}/{effectData.maxStacks} stacks)", LogCategory.General);
                }
            }
            else
            {
                // For non-stackable effects, refresh or apply
                var existingEffect = existingEffects.FirstOrDefault();
                if (existingEffect != null)
                {
                    // Refresh duration of existing effect
                    existingEffect.RemainingDuration = duration >= 0 ? duration : effectData.duration;
                    SmartLogger.Log($"Refreshed duration of {effectData.displayName} on {targetUnit.DisplayName}", LogCategory.General);
                }
                else
                {
                    // Apply new effect
                    targetUnit.AddStatusEffect(newEffect);
                    if (targetUnit is DokkaebiUnit concreteUnit)
                    {
                        concreteUnit.RaiseStatusEffectApplied(newEffect);
                    }
                    SmartLogger.Log($"Applied new effect {effectData.displayName} to {targetUnit.DisplayName}", LogCategory.General);
                }
            }

            // Apply immediate effects if any
            ApplyEffectImmediateImpact(targetUnit, effectData);
        }

        /// <summary>
        /// Remove a specific status effect from a unit
        /// </summary>
        public static void RemoveStatusEffect(IDokkaebiUnit targetUnit, StatusEffectType effectType)
        {
            if (targetUnit == null) return;

            var effectToRemove = targetUnit.GetStatusEffects()?.FirstOrDefault(e => e.StatusEffectType == effectType);
            if (effectToRemove != null)
            {
                targetUnit.RemoveStatusEffect(effectToRemove);
                if (targetUnit is DokkaebiUnit concreteUnit)
                {
                    concreteUnit.RaiseStatusEffectRemoved(effectToRemove);
                }
            }
        }

        /// <summary>
        /// Process status effects at the end of a unit's turn
        /// </summary>
        public static void ProcessTurnEndForUnit(IDokkaebiUnit unit)
        {
            if (unit == null) return;

            var unitEffects = unit.GetStatusEffects()?.ToList();
            if (unitEffects == null) return;

            // Process each effect
            for (int i = unitEffects.Count - 1; i >= 0; i--)
            {
                var effect = unitEffects[i];
                
                // Apply turn end effects
                if (effect is StatusEffectInstance instance)
                {
                    ApplyEffectTurnEndImpact(unit, instance.Effect);

                    // Reduce duration if not permanent
                    if (!instance.Effect.isPermanent)
                    {
                        instance.RemainingDuration--;
                        if (instance.RemainingDuration <= 0)
                        {
                            unit.RemoveStatusEffect(effect);
                            if (unit is DokkaebiUnit concreteUnit)
                            {
                                concreteUnit.RaiseStatusEffectRemoved(effect);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Remove all status effects from a unit.
        /// </summary>
        public static void ClearAllStatusEffects(IDokkaebiUnit target)
        {
            if (target == null) return;

            var unitEffects = target.GetStatusEffects()?.ToList();
            if (unitEffects == null) return;

            // Remove impacts of all effects
            foreach (var effect in unitEffects)
            {
                if (effect is StatusEffectInstance instance)
                {
                    RemoveEffectImpacts(target, instance.Effect);
                }
            }

            if (target is DokkaebiUnit dokkaebiUnit)
            {
                foreach (var effect in unitEffects)
                {
                    dokkaebiUnit.RaiseStatusEffectRemoved(effect);
                }
                dokkaebiUnit.GetStatusEffects().Clear();
            }

            SmartLogger.Log($"Cleared all status effects from {target.DisplayName}", LogCategory.General);
        }

        /// <summary>
        /// Check if a unit has a specific status effect.
        /// </summary>
        public static bool HasStatusEffect(IDokkaebiUnit target, StatusEffectType effectType)
        {
            if (target == null) return false;

            var unitEffects = target.GetStatusEffects();
            return unitEffects?.Any(e => e.StatusEffectType == effectType) ?? false;
        }

        /// <summary>
        /// Aggregates the stat modifier value for a specific attribute from all active status effects on a unit.
        /// </summary>
        public static float GetStatModifier(IDokkaebiUnit unit, UnitAttributeType statType)
        {
            if (unit == null) return 1.0f;

            var unitEffects = unit.GetStatusEffects()?.ToList();
            if (unitEffects == null) return 1.0f;

            float multiplier = 1.0f;
            foreach (var effect in unitEffects)
            {
                if (effect is StatusEffectInstance instance && instance.Effect != null)
                {
                    if (instance.Effect.HasStatModifier(statType))
                    {
                        multiplier *= instance.Effect.GetStatModifier(statType);
                    }
                }
            }
            
            return multiplier;
        }

        /// <summary>
        /// Checks if the unit is prevented from acting by status effects (e.g., Stun).
        /// </summary>
        public static bool CanUnitAct(IDokkaebiUnit unit)
        {
            if (unit == null) return true;

            var unitEffects = unit.GetStatusEffects()?.ToList();
            if (unitEffects == null) return true;

            // Check for effects that prevent acting (e.g., Stun)
            return !unitEffects.Any(effect => 
                effect.StatusEffectType == StatusEffectType.Stun || 
                effect.StatusEffectType == StatusEffectType.Frozen);
        }

        /// <summary>
        /// Checks if the unit is prevented from moving by status effects (e.g., Root).
        /// </summary>
        public static bool CanUnitMove(IDokkaebiUnit unit)
        {
            if (unit == null) return true;

            var unitEffects = unit.GetStatusEffects()?.ToList();
            if (unitEffects == null) return true;

            // Check for effects that prevent movement (e.g., Root)
            return !unitEffects.Any(effect => 
                effect.StatusEffectType == StatusEffectType.Root || 
                effect.StatusEffectType == StatusEffectType.Stun || 
                effect.StatusEffectType == StatusEffectType.Frozen);
        }

        /// <summary>
        /// Apply the turn end impact of a status effect.
        /// </summary>
        private static void ApplyEffectTurnEndImpact(IDokkaebiUnit target, StatusEffectData effectData)
        {
            if (target == null || effectData == null) return;

            // Apply damage over time
            if (effectData.hasDamageOverTime)
            {
                int damage = effectData.damageOverTimeAmount;
                if (target is DokkaebiUnit dokkaebiUnit)
                {
                    dokkaebiUnit.TakeDamage(damage, DamageType.Normal);
                }
            }

            // Apply healing over time
            if (effectData.hasHealingOverTime)
            {
                int healing = effectData.healingOverTimeAmount;
                if (target is DokkaebiUnit dokkaebiUnit)
                {
                    dokkaebiUnit.Heal(healing);
                }
            }
        }

        /// <summary>
        /// Apply the immediate impact of a status effect.
        /// </summary>
        private static void ApplyEffectImmediateImpact(IDokkaebiUnit target, StatusEffectData effectData)
        {
            if (target == null || effectData == null) return;

            // Apply immediate damage
            if (effectData.hasImmediateDamage)
            {
                int damage = effectData.immediateDamageAmount;
                if (target is DokkaebiUnit dokkaebiUnit)
                {
                    dokkaebiUnit.TakeDamage(damage, DamageType.Normal);
                }
            }

            // Apply immediate healing
            if (effectData.hasImmediateHealing)
            {
                int healing = effectData.immediateHealingAmount;
                if (target is DokkaebiUnit dokkaebiUnit)
                {
                    dokkaebiUnit.Heal(healing);
                }
            }
        }

        /// <summary>
        /// Remove the impacts of a status effect.
        /// </summary>
        private static void RemoveEffectImpacts(IDokkaebiUnit target, StatusEffectData effectData)
        {
            if (target == null || effectData == null) return;

            // Remove stat modifiers
            // Note: This is handled automatically through the GetStatModifier method
            // which only considers active effects
        }
    }
} 