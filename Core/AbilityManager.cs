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
            // Validate ability state
            if (!ValidateAbility(abilityData, sourceUnit, targetPosition, targetUnit, isOverload))
            {
                SmartLogger.Log($"Ability validation failed for {abilityData.displayName}", LogCategory.Ability);
                return false;
            }
            
            // Handle aura cost
            int auraCost = abilityData.auraCost;
            if (isOverload && abilityData.requiresOverload)
            {
                auraCost = 0; // Overload abilities don't cost aura for prototype
            }
            else if (sourceUnit.GetCurrentAura() < auraCost)
            {
                SmartLogger.Log($"Not enough aura to cast {abilityData.displayName}", LogCategory.Ability);
                return false;
            }
            
            // Apply aura cost
            sourceUnit.ModifyAura(-auraCost);
            
            // Apply cooldown
            sourceUnit.SetAbilityCooldown(abilityData.abilityType, abilityData.cooldownTurns);
            
            // Play cast sound
            if (abilityData.castSound != null)
            {
                PlayAbilitySound(abilityData.castSound);
            }
            
            // Handle immediate effects
            HandleDamageAndHealing(abilityData, sourceUnit, targetUnit, targetPosition, isOverload);
            
            // Apply status effects
            if (targetUnit != null && abilityData.appliedEffects != null && abilityData.appliedEffects.Count > 0)
            {
                ApplyStatusEffects(abilityData, targetUnit, isOverload);
            }
            
            // Create zone if applicable
            if (abilityData.createsZone && abilityData.zoneToCreate != null)
            {
                CreateZone(abilityData, targetPosition, sourceUnit, isOverload);
            }
            
            // Play ability VFX
            if (enableAbilityVFX && abilityData.abilityVFXPrefab != null)
            {
                PlayAbilityVFX(abilityData, sourceUnit, targetPosition, targetUnit);
            }
            
            // Mark ability as used for this unit
            sourceUnit.PlanAbilityUse((int)abilityData.abilityType, targetPosition);
            
            SmartLogger.Log($"Ability {abilityData.displayName} executed successfully", LogCategory.Ability);
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
                return false;
            }
            
            // Check if it's the unit's turn to use an ability
            if (turnSystem != null && !turnSystem.CanUnitUseAura(sourceUnit))
            {
                return false;
            }
            
            // Check cooldown
            if (sourceUnit.IsOnCooldown(abilityData.abilityType))
            {
                return false;
            }
            
            // Check aura cost
            int auraCost = abilityData.auraCost;
            if (isOverload && abilityData.requiresOverload)
            {
                // Overload abilities don't cost aura for prototype
            }
            else if (sourceUnit.GetCurrentAura() < auraCost)
            {
                return false;
            }
            
            // Check if unit has already used an ability
            if (sourceUnit.HasPendingAbility())
            {
                return false;
            }
            
            // Check targeting
            bool isTargetValid = IsTargetValid(abilityData, sourceUnit, targetPosition, targetUnit);
            if (!isTargetValid)
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Check if the target is valid for the ability
        /// </summary>
        private bool IsTargetValid(AbilityData abilityData, DokkaebiUnit sourceUnit, GridPosition targetPosition, DokkaebiUnit targetUnit)
        {
            // Calculate distance
            GridPosition sourcePos = sourceUnit.GetGridPosition();
            int distance = GridPosition.GetManhattanDistance(sourcePos, targetPosition);
            
            // Check range
            if (distance > abilityData.range)
            {
                return false;
            }
            
            // Check self-targeting
            if (abilityData.targetsSelf && targetUnit != sourceUnit)
            {
                return false;
            }
            
            // Check ally targeting
            if (abilityData.targetsAlly && (targetUnit == null || targetUnit.IsPlayer() != sourceUnit.IsPlayer()))
            {
                return false;
            }
            
            // Check enemy targeting
            if (abilityData.targetsEnemy && (targetUnit == null || targetUnit.IsPlayer() == sourceUnit.IsPlayer()))
            {
                return false;
            }
            
            // Check ground targeting
            if (abilityData.targetsGround && !GridManager.Instance.IsPositionValid(targetPosition))
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Handle damage and healing from ability
        /// </summary>
        private void HandleDamageAndHealing(AbilityData abilityData, DokkaebiUnit sourceUnit, DokkaebiUnit targetUnit, GridPosition targetPosition, bool isOverload)
        {
            // Direct damage
            if (abilityData.damageAmount > 0 && targetUnit != null)
            {
                int damage = abilityData.damageAmount;
                if (isOverload && abilityData.hasOverloadVariant)
                {
                    damage *= abilityData.overloadDamageMultiplier;
                }
                
                targetUnit.ModifyHealth(-damage, abilityData.damageType);
                
                // Play hit sound
                if (abilityData.hitSound != null)
                {
                    PlayAbilitySound(abilityData.hitSound);
                }
            }
            
            // Direct healing
            if (abilityData.healAmount > 0 && targetUnit != null)
            {
                int healing = abilityData.healAmount;
                if (isOverload && abilityData.hasOverloadVariant)
                {
                    healing *= abilityData.overloadHealMultiplier;
                }
                
                targetUnit.ModifyHealth(healing);
            }
        }
        
        /// <summary>
        /// Apply status effects from ability
        /// </summary>
        private void ApplyStatusEffects(AbilityData abilityData, DokkaebiUnit targetUnit, bool isOverload)
        {
            foreach (var effect in abilityData.appliedEffects)
            {
                if (effect != null)
                {
                    int duration = effect.duration;
                    if (isOverload && abilityData.hasOverloadVariant)
                    {
                        duration = Mathf.RoundToInt(duration * abilityData.overloadEffectDurationMultiplier);
                    }
                    
                    targetUnit.ApplyStatusEffect(effect, duration);
                }
            }
        }
        
        /// <summary>
        /// Create a zone from ability
        /// </summary>
        private void CreateZone(AbilityData abilityData, GridPosition position, DokkaebiUnit sourceUnit, bool isOverload)
        {
            var zoneManager = FindObjectOfType<Dokkaebi.Zones.ZoneManager>();
            if (zoneManager != null && abilityData.zoneToCreate != null)
            {
                int duration = abilityData.zoneDuration > 0 ? abilityData.zoneDuration : abilityData.zoneToCreate.defaultDuration;
                if (isOverload && abilityData.hasOverloadVariant)
                {
                    duration = Mathf.RoundToInt(duration * abilityData.overloadZoneEffectMultiplier);
                }
                
                zoneManager.CreateZone(
                    position,
                    abilityData.zoneToCreate,
                    sourceUnit,
                    duration
                );
            }
        }
        
        /// <summary>
        /// Play ability visual effects
        /// </summary>
        private void PlayAbilityVFX(AbilityData abilityData, DokkaebiUnit sourceUnit, GridPosition targetPosition, DokkaebiUnit targetUnit)
        {
            Vector3 targetWorldPos = targetUnit != null 
                ? targetUnit.transform.position 
                : GridManager.Instance.GridToWorld(targetPosition);
            
            // Instantiate the VFX prefab
            GameObject vfxInstance = Instantiate(abilityData.abilityVFXPrefab, targetWorldPos, Quaternion.identity);
            
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
                Debug.LogError("AbilityManager: Invalid unit for tactical repositioning");
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
                Debug.LogWarning($"AbilityManager: Invalid tactical repositioning distance ({distance})");
                return false;
            }
            
            // Check if the target position is passable
            // This would typically check with GridManager if the tile is passable
            
            // Move the unit to the target position
            unit.SetGridPosition(targetPosition);
            
            return true;
        }
    }
} 