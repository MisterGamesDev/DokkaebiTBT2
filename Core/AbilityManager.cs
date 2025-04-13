using UnityEngine;
using System.Collections.Generic;
using Dokkaebi.Core.Data;
using Dokkaebi.Grid;
using Dokkaebi.Units;
using Dokkaebi.Zones;
using System.Linq;
using System.Collections;
using Dokkaebi.Pathfinding;
using Dokkaebi.Utilities;
using Dokkaebi.Interfaces;
using Dokkaebi.Common;

namespace Dokkaebi.Core
{
    /// <summary>
    /// Manager class that handles ability execution and validation
    /// </summary>
    public class AbilityManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UnitManager unitManager;
        [SerializeField] private DokkaebiTurnSystemCore turnSystem;
        
        [Header("Settings")]
        [SerializeField] private bool enableAbilityVFX = true;
        [SerializeField] private LayerMask targetingLayerMask;
        
        // Audio sources
        private AudioSource sfxPlayer;
        
        // VFX reference dicts
        private Dictionary<DamageType, GameObject> damageVFXs = new Dictionary<DamageType, GameObject>();
        private Dictionary<string, GameObject> specialVFXs = new Dictionary<string, GameObject>();
        
        private void Awake()
        {
            // Get component references
            if (unitManager == null)
            {
                unitManager = FindObjectOfType<UnitManager>();
            }
            
            if (turnSystem == null)
            {
                turnSystem = FindObjectOfType<DokkaebiTurnSystemCore>();
            }
            
            
            // Set up audio source for SFX
            sfxPlayer = gameObject.AddComponent<AudioSource>();
            sfxPlayer.playOnAwake = false;
            sfxPlayer.spatialBlend = 0f; // 2D sound
        }
        
        /// <summary>
        /// Execute an ability from a unit to a target position/unit
        /// </summary>
        /// <param name="abilityData">The ability data to execute</param>
        /// <param name="sourceUnit">The unit using the ability</param>
        /// <param name="targetPosition">The target position</param>
        /// <param name="targetUnit">The target unit, if any</param>
        /// <param name="isOverload">Whether this is an overload cast</param>
        /// <returns>True if ability was executed successfully</returns>
        public bool ExecuteAbility(AbilityData abilityData, DokkaebiUnit sourceUnit, GridPosition targetPosition, DokkaebiUnit targetUnit, bool isOverload)
        {
            // Validate critical parameters
            if (abilityData == null)
            {
                SmartLogger.LogError("[AbilityManager.ExecuteAbility] Cannot execute null ability data", LogCategory.Ability);
                return false;
            }
            
            if (sourceUnit == null)
            {
                SmartLogger.LogError("[AbilityManager.ExecuteAbility] Cannot execute ability with null source unit", LogCategory.Ability);
                return false;
            }

            SmartLogger.Log($"[AbilityManager.ExecuteAbility START] Ability='{abilityData.displayName}', Caster='{sourceUnit.GetUnitName()}', TargetPos='{targetPosition}', TargetUnit='{targetUnit?.GetUnitName()}', Overload={isOverload}", LogCategory.Ability);

            // Validate ability state
            bool isValid = ValidateAbility(abilityData, sourceUnit, targetPosition, targetUnit, isOverload);
            SmartLogger.Log($"[AbilityManager.ExecuteAbility] ValidateAbility result: {isValid}", LogCategory.Ability);
            if (!isValid)
            {
                return false;
            }
            
            // Explicit aura cost check before execution
            int auraCost = abilityData.auraCost;
            if (!isOverload && !sourceUnit.HasEnoughUnitAura(auraCost))
            {
                SmartLogger.LogWarning($"[AbilityManager.ExecuteAbility] Unit {sourceUnit.GetUnitName()} does not have enough aura ({auraCost}) to cast {abilityData.displayName}", LogCategory.Ability);
                return false;
            }
            
            // Handle aura cost - only deduct after validation passes
            SmartLogger.Log($"[AbilityManager.ExecuteAbility] Applying unit-specific Aura cost: {auraCost}", LogCategory.Ability);
            sourceUnit.ModifyUnitAura(-auraCost);
            
            // Apply cooldown immediately after cost but before effects
            SmartLogger.Log($"[AbilityManager.ExecuteAbility] Attempting to apply Cooldown: {abilityData.cooldownTurns} turns to Type: {abilityData.abilityType}", LogCategory.Ability);
            sourceUnit.SetAbilityCooldown(abilityData.abilityType, abilityData.cooldownTurns);
            
            // Play cast sound
            if (abilityData.castSound != null)
            {
                PlayAbilitySound(abilityData.castSound);
            }
            
            // Handle immediate effects
            SmartLogger.Log($"[AbilityManager.ExecuteAbility] BEFORE HandleDamageAndHealing. TargetUnit is {(targetUnit != null ? targetUnit.GetUnitName() : "NULL")}", LogCategory.Ability);
            HandleDamageAndHealing(abilityData, sourceUnit, targetUnit, targetPosition, isOverload);
            SmartLogger.Log($"[AbilityManager.ExecuteAbility] AFTER HandleDamageAndHealing.", LogCategory.Ability);
            
            // Apply status effects
            if (targetUnit != null && abilityData.appliedEffects != null && abilityData.appliedEffects.Count > 0)
            {
                SmartLogger.Log($"[AbilityManager.ExecuteAbility] BEFORE ApplyStatusEffects.", LogCategory.Ability);
                ApplyStatusEffects(abilityData, sourceUnit, targetUnit, isOverload);
                SmartLogger.Log($"[AbilityManager.ExecuteAbility] AFTER ApplyStatusEffects.", LogCategory.Ability);
            }
            
            // Create zone if applicable
            if (abilityData.createsZone && abilityData.zoneToCreate != null)
            {
                SmartLogger.Log($"[AbilityManager.ExecuteAbility] BEFORE CreateZone.", LogCategory.Ability);
                CreateZone(abilityData, targetPosition, sourceUnit, isOverload);
                SmartLogger.Log($"[AbilityManager.ExecuteAbility] AFTER CreateZone.", LogCategory.Ability);
            }
            
            // Play ability VFX
            if (enableAbilityVFX && abilityData.abilityVFXPrefab != null)
            {
                PlayAbilityVFX(abilityData, sourceUnit, targetPosition, targetUnit);
            }
            
            SmartLogger.Log($"[AbilityManager.ExecuteAbility END] Successfully executed '{abilityData?.displayName}'", LogCategory.Ability);
            return true;
        }
        
        /// <summary>
        /// Validate if the ability can be executed
        /// </summary>
        private bool ValidateAbility(AbilityData abilityData, DokkaebiUnit sourceUnit, GridPosition targetPosition, DokkaebiUnit targetUnit, bool isOverload)
        {
            // Check if source unit exists and is alive
            if (sourceUnit == null || !sourceUnit.IsAlive)
            {
                SmartLogger.Log($"Invalid source unit for ability {abilityData.displayName}", LogCategory.Ability);
                return false;
            }
            
            // Check if it's the unit's turn to use an ability
            if (turnSystem != null && !turnSystem.CanUnitUseAura(sourceUnit))
            {
                SmartLogger.Log($"Unit {sourceUnit.GetUnitName()} cannot use aura at this time", LogCategory.Ability);
                return false;
            }
            
            // Check cooldown
            if (sourceUnit.IsOnCooldown(abilityData.abilityType))
            {
                SmartLogger.Log($"Ability {abilityData.displayName} is on cooldown", LogCategory.Ability);
                return false;
            }
            
            // Check targeting
            bool isTargetValid = IsTargetValid(abilityData, sourceUnit, targetPosition, targetUnit);
            if (!isTargetValid)
            {
                SmartLogger.Log($"Invalid target for ability {abilityData.displayName}", LogCategory.Ability);
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Check if the target is valid for the ability
        /// </summary>
        private bool IsTargetValid(AbilityData abilityData, DokkaebiUnit sourceUnit, GridPosition targetPosition, DokkaebiUnit targetUnit)
        {
            SmartLogger.Log($"[AbilityManager.IsTargetValid] START - Ability: {abilityData.displayName}, Source: {sourceUnit?.GetUnitName()}, Target Position: {targetPosition}, Target Unit: {targetUnit?.GetUnitName()}", LogCategory.Ability);
            SmartLogger.Log($"[AbilityManager.IsTargetValid] Ability flags - Ground: {abilityData.targetsGround}, Self: {abilityData.targetsSelf}, Ally: {abilityData.targetsAlly}, Enemy: {abilityData.targetsEnemy}", LogCategory.Ability);

            // Calculate distance first
            GridPosition sourcePos = sourceUnit.GetGridPosition();
            int distance = GridPosition.GetManhattanDistance(sourcePos, targetPosition);
            
            // Check range first - this applies to ALL targeting types
            if (distance > abilityData.range)
            {
                SmartLogger.Log($"[AbilityManager.IsTargetValid] FAILED: Target out of range. Distance: {distance}, Range: {abilityData.range}", LogCategory.Ability);
                return false;
            }

            // For ground-targeting abilities, we only care about position validity
            if (abilityData.targetsGround)
            {
                bool isValidPosition = GridManager.Instance.IsPositionValid(targetPosition);
                SmartLogger.Log($"[AbilityManager.IsTargetValid] Ground-targeting ability check - Position valid: {isValidPosition}", LogCategory.Ability);
                return isValidPosition;
            }

            // If we get here, this is a unit-targeting ability, so we need a valid target unit
            if (targetUnit == null)
            {
                SmartLogger.Log("[AbilityManager.IsTargetValid] FAILED: Unit-targeting ability requires valid target unit", LogCategory.Ability);
                return false;
            }

            if (!targetUnit.IsAlive)
            {
                SmartLogger.Log("[AbilityManager.IsTargetValid] FAILED: Target unit is not alive", LogCategory.Ability);
                return false;
            }

            // Check unit targeting rules
            bool canTargetSelf = abilityData.targetsSelf && targetUnit == sourceUnit;
            bool canTargetAlly = abilityData.targetsAlly && targetUnit != sourceUnit && targetUnit.IsPlayer() == sourceUnit.IsPlayer();
            bool canTargetEnemy = abilityData.targetsEnemy && targetUnit.IsPlayer() != sourceUnit.IsPlayer();

            bool isValidTarget = canTargetSelf || canTargetAlly || canTargetEnemy;

            SmartLogger.Log($"[AbilityManager.IsTargetValid] Unit targeting check - CanTargetSelf: {canTargetSelf}, CanTargetAlly: {canTargetAlly}, CanTargetEnemy: {canTargetEnemy}, Final result: {isValidTarget}", LogCategory.Ability);

            return isValidTarget;
        }
        
        /// <summary>
        /// Handle damage and healing from ability
        /// </summary>
        private void HandleDamageAndHealing(AbilityData abilityData, DokkaebiUnit sourceUnit, DokkaebiUnit targetUnit, GridPosition targetPosition, bool isOverload)
        {
            // Validate critical parameters
            if (abilityData == null)
            {
                SmartLogger.LogError("[AbilityManager.HandleDamageAndHealing] Cannot process null ability data", LogCategory.Ability);
                return;
            }

            if (sourceUnit == null)
            {
                SmartLogger.LogError("[AbilityManager.HandleDamageAndHealing] Cannot process ability with null source unit", LogCategory.Ability);
                return;
            }

            SmartLogger.Log($"[AbilityManager.HandleDamageAndHealing START] Ability='{abilityData.displayName}', Caster='{sourceUnit.GetUnitName()}', Target='{targetUnit?.GetUnitName()}'", LogCategory.Ability);

            // Skip damage/healing if target is null or not alive
            if (targetUnit == null || !targetUnit.IsAlive)
            {
                SmartLogger.Log("[AbilityManager.HandleDamageAndHealing] Target unit is null or not alive - skipping damage/healing", LogCategory.Ability);
                return;
            }

            // Handle damage
            if (abilityData.damageAmount > 0)
            {
                // Calculate final damage using the service
                int actualDamage = CombatCalculationService.CalculateFinalDamage(abilityData, sourceUnit, targetUnit, isOverload);
                SmartLogger.Log($"[AbilityManager.HandleDamageAndHealing] Calculated damage amount: {actualDamage}, DamageType: {abilityData.damageType}", LogCategory.Ability);
                
                // Log target's state before damage
                SmartLogger.Log($"[AbilityManager.HandleDamageAndHealing] Target state BEFORE damage - Health: {targetUnit.GetCurrentHealth()}/{targetUnit.GetMaxHealth()}, Status Effects: {string.Join(", ", targetUnit.GetStatusEffects().Select(e => e.StatusEffectType.ToString()))}", LogCategory.Ability);
                
                // Apply damage and log the result
                targetUnit.ModifyHealth(-actualDamage, abilityData.damageType);
                
                // Log target's state after damage
                SmartLogger.Log($"[AbilityManager.HandleDamageAndHealing] Target state AFTER damage - Health: {targetUnit.GetCurrentHealth()}/{targetUnit.GetMaxHealth()}, Damage Applied: {actualDamage}, DamageType: {abilityData.damageType}", LogCategory.Ability);
                
                // Play hit sound if available
                if (abilityData.hitSound != null)
                {
                    PlayAbilitySound(abilityData.hitSound);
                    SmartLogger.Log("[AbilityManager.HandleDamageAndHealing] Hit sound played", LogCategory.Ability);
                }
            }
            
            // Handle healing
            if (abilityData.healAmount > 0)
            {
                // Calculate final healing using the service
                int actualHealing = CombatCalculationService.CalculateFinalHealing(abilityData, sourceUnit, targetUnit, isOverload);
                SmartLogger.Log($"[AbilityManager.HandleDamageAndHealing] Calculated healing amount: {actualHealing}", LogCategory.Ability);
                
                // Log target's state before healing
                SmartLogger.Log($"[AbilityManager.HandleDamageAndHealing] Target state BEFORE healing - Health: {targetUnit.GetCurrentHealth()}/{targetUnit.GetMaxHealth()}", LogCategory.Ability);
                
                // Apply healing and log the result
                targetUnit.ModifyHealth(actualHealing);
                
                // Log target's state after healing
                SmartLogger.Log($"[AbilityManager.HandleDamageAndHealing] Target state AFTER healing - Health: {targetUnit.GetCurrentHealth()}/{targetUnit.GetMaxHealth()}, Healing Applied: {actualHealing}", LogCategory.Ability);
            }

            SmartLogger.Log($"[AbilityManager.HandleDamageAndHealing END] Ability execution completed for '{abilityData.displayName}'", LogCategory.Ability);
        }
        
        /// <summary>
        /// Apply status effects from ability
        /// </summary>
        private void ApplyStatusEffects(AbilityData abilityData, DokkaebiUnit sourceUnit, DokkaebiUnit targetUnit, bool isOverload)
        {
            if (targetUnit == null || !targetUnit.IsAlive || abilityData.appliedEffects == null || abilityData.appliedEffects.Count == 0)
            {
                SmartLogger.Log("Invalid target or no status effects to apply", LogCategory.Ability);
                return;
            }

            foreach (var effectData in abilityData.appliedEffects)
            {
                if (effectData != null)
                {
                    int actualDuration = effectData.duration;
                    if (isOverload && abilityData.hasOverloadVariant)
                    {
                        actualDuration = Mathf.RoundToInt(actualDuration * abilityData.overloadEffectDurationMultiplier);
                        SmartLogger.Log($"Overload active: Effect duration increased from {effectData.duration} to {actualDuration}", LogCategory.Ability);
                    }
                    
                    StatusEffectSystem.ApplyStatusEffect(targetUnit, effectData, actualDuration, sourceUnit);
                    SmartLogger.Log($"Applied {effectData.displayName} to {targetUnit.GetUnitName()} for {actualDuration} turns", LogCategory.Ability);
                }
            }
        }
        
        /// <summary>
        /// Create a zone from ability
        /// </summary>
        private void CreateZone(AbilityData abilityData, GridPosition position, DokkaebiUnit sourceUnit, bool isOverload)
        {
            if (!abilityData.createsZone || abilityData.zoneToCreate == null)
            {
                SmartLogger.Log("No zone to create or invalid zone data", LogCategory.Ability);
                return;
            }

            var zoneManager = ZoneManager.Instance;
            if (zoneManager == null)
            {
                SmartLogger.LogError("ZoneManager instance not found", LogCategory.Ability);
                return;
            }

            // Determine base duration
            int zoneDuration = abilityData.zoneDuration > 0 ? abilityData.zoneDuration : abilityData.zoneToCreate.defaultDuration;
            
            // Apply overload multiplier if applicable
            if (isOverload && abilityData.hasOverloadVariant)
            {
                zoneDuration = Mathf.RoundToInt(zoneDuration * abilityData.overloadZoneEffectMultiplier);
                SmartLogger.Log($"Overload active: Zone duration increased to {zoneDuration} turns", LogCategory.Ability);
            }
            
            zoneManager.CreateZone(position, abilityData.zoneToCreate, sourceUnit, zoneDuration);
            SmartLogger.Log($"Created {abilityData.zoneToCreate.displayName} zone at {position} lasting {zoneDuration} turns", LogCategory.Ability);
        }
        
        /// <summary>
        /// Play ability visual effects
        /// </summary>
        private void PlayAbilityVFX(AbilityData abilityData, DokkaebiUnit sourceUnit, GridPosition targetPosition, DokkaebiUnit targetUnit)
        {
            Vector3 worldPosition = targetPosition != null 
                ? GridManager.Instance.GridToWorldPosition(targetPosition)
                : sourceUnit.transform.position;
            
            // Instantiate the VFX prefab
            GameObject vfxInstance = Instantiate(abilityData.abilityVFXPrefab, worldPosition, Quaternion.identity);
            
            // Destroy after some time
            Destroy(vfxInstance, 3f);
        }
        
        /// <summary>
        /// Play ability sound effects
        /// </summary>
        private void PlayAbilitySound(AudioClip clip)
        {
            if (clip != null && sfxPlayer != null)
            {
                sfxPlayer.clip = clip;
                sfxPlayer.Play();
            }
        }
        
        /// <summary>
        /// Execute tactical repositioning for a unit
        /// </summary>
        public bool ExecuteTacticalRepositioning(DokkaebiUnit unit, GridPosition targetPosition)
        {
            if (unit == null)
            {
                SmartLogger.LogError("Invalid unit for tactical repositioning", LogCategory.Ability);
                return false;
            }
            
            // Check if the target position is valid
            GridPosition currentPosition = unit.GetGridPosition();
            
            // Calculate Manhattan distance
            int distance = Mathf.Abs(targetPosition.x - currentPosition.x) + 
                          Mathf.Abs(targetPosition.z - currentPosition.z);
            
            // Ensure the distance is within the tactical repositioning range (1-2 tiles)
            if (distance < 1 || distance > 2)
            {
                SmartLogger.LogWarning($"Invalid tactical repositioning distance ({distance})", LogCategory.Ability);
                return false;
            }
            
            // Check if the target position is passable
            // This would typically check with GridManager if the tile is passable
            
            // Move the unit to the target position
            unit.SetGridPosition(targetPosition);
            
            SmartLogger.Log($"Unit {unit.GetUnitName()} repositioned to {targetPosition}", LogCategory.Ability);
            return true;
        }
    }
} 