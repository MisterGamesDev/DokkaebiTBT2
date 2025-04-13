using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Dokkaebi.Common;
using Dokkaebi.Interfaces;
using Dokkaebi.Core;
using Dokkaebi.Utilities;
using Dokkaebi.Grid;
using Dokkaebi.Core.Data;
using Dokkaebi.Units;

namespace Dokkaebi.Zones
{
    /// <summary>
    /// Represents an active zone instance in the game world.
    /// Contains the runtime properties of the zone including its effects.
    /// </summary>
    public class ZoneInstance : MonoBehaviour, IZoneInstance
    {
        [SerializeField] private SpriteRenderer zoneVisual;
        [SerializeField] private float visualFadeSpeed = 2f;
        
        private IZoneData zoneData;
        private IStatusEffect statusEffect;
        private int remainingDuration;
        private int currentStacks = 1;
        private bool isActive = true;
        private GridPosition position;
        private int ownerUnitId = -1;
        private float currentAlpha = 1f;
        private bool isFading;
        
        // IZoneInstance implementation
        public GridPosition Position => position;
        public int Id => int.Parse(zoneData?.Id ?? "-1");
        public int Radius => zoneData?.Radius ?? 1;
        public bool IsActive => isActive;
        public int RemainingDuration => remainingDuration;
        public int OwnerUnitId => ownerUnitId;
        
        // Additional properties
        public string DisplayName => zoneData?.DisplayName ?? "Unknown Zone";
        public int CurrentStacks => currentStacks;
        public bool CanMerge => zoneData?.CanMerge ?? false;
        public int MaxStacks => zoneData?.MaxStacks ?? 1;
        public IReadOnlyList<string> MergesWithZoneIds => zoneData?.MergesWithZoneIds ?? new List<string>();
        public IReadOnlyList<string> ResonanceOrigins => zoneData?.ResonanceOrigins ?? new List<string>();
        public float ResonanceEffectMultiplier => zoneData?.ResonanceEffectMultiplier ?? 1f;
        
        public void Initialize(IZoneData data, GridPosition pos, int ownerUnit, int duration = -1)
        {
            zoneData = data;
            position = pos;
            ownerUnitId = ownerUnit;
            remainingDuration = duration >= 0 ? duration : data.DefaultDuration;
            
            SetupVisuals();
            Vector3 worldPosition = position.ToWorldPosition();
            worldPosition.y += 0.1f; // Raise zone slightly above ground
            SmartLogger.Log($"Creating zone '{DisplayName}' at position {worldPosition} (with Y offset)", LogCategory.Zone);
            transform.position = worldPosition;
        }
        
        private void SetupVisuals()
        {
            if (zoneVisual != null && zoneData != null)
            {
                var color = zoneData.ZoneColor;
                color.a = currentAlpha;
                zoneVisual.color = color;
            }
        }
        
        public void AddStack()
        {
            if (currentStacks < zoneData.MaxStacks)
            {
                currentStacks++;
            }
        }
        
        public void DecreaseDuration()
        {
            if (remainingDuration > 0)
            {
                remainingDuration--;
                if (remainingDuration <= 0)
                {
                    StartFade();
                }
            }
        }
        
        public void SetStatusEffect(IStatusEffect effect)
        {
            statusEffect = effect;
        }
        
        public IStatusEffect GetStatusEffect()
        {
            return statusEffect;
        }
        
        public bool ContainsPosition(GridPosition pos)
        {
            // Calculate Manhattan distance between positions
            int dx = Mathf.Abs(pos.x - position.x);
            int dz = Mathf.Abs(pos.z - position.z);
            return dx + dz <= Radius;
        }
        
        private void StartFade()
        {
            isFading = true;
            isActive = false;
        }
        
        /// <summary>
        /// Applies this zone's effects to units within its area.
        /// Called by ZoneManager during turn resolution.
        /// </summary>
        public void ApplyZoneEffects(IDokkaebiUnit targetUnit = null)
        {
            // Ensure we have valid zone data and the zone is active
            if (!IsActive || zoneData == null) 
            {
                SmartLogger.Log($"Zone '{DisplayName}' cannot apply effects: IsActive={IsActive}, zoneData={(zoneData == null ? "null" : "valid")}", LogCategory.Zone);
                return; 
            }

            // Cast zoneData to the concrete ZoneData type once
            ZoneData concreteZoneData = zoneData as ZoneData;
            if (concreteZoneData == null) 
            {
                SmartLogger.LogError($"Cannot apply effects: zoneData is not of type ZoneData for {DisplayName}", LogCategory.Zone);
                return; 
            }

            // Ensure UnitManager instance is available
            if (UnitManager.Instance == null)
            {
                SmartLogger.LogError("Cannot apply zone effects: UnitManager instance is null.", LogCategory.Zone);
                return;
            }

            // Log the start of effect application
            SmartLogger.Log($"Zone '{DisplayName}' at {position} beginning effect application", LogCategory.Zone);

            // If a specific target unit was provided, apply effects only to that unit
            if (targetUnit != null)
            {
                SmartLogger.Log($"Zone '{DisplayName}' applying effects to specific target unit: {targetUnit.GetUnitName()}", LogCategory.Zone);
                ApplyEffectsToUnit(targetUnit, concreteZoneData);
                return;
            }

            // Get the unit at this position using UnitManager
            var unitInZone = UnitManager.Instance.GetUnitAtPosition(position);
            if (unitInZone != null)
            {
                SmartLogger.Log($"Zone '{DisplayName}' found unit '{unitInZone.GetUnitName()}' at center position {position}", LogCategory.Zone);
                ApplyEffectsToUnit(unitInZone, concreteZoneData);
            }
            else
            {
                SmartLogger.Log($"Zone '{DisplayName}' at {position} has no unit at center to affect", LogCategory.Zone);
            }
        }

        private void ApplyEffectsToUnit(IDokkaebiUnit targetUnit, ZoneData concreteZoneData)
        {
            if (!(targetUnit is DokkaebiUnit dokkaebiUnit))
            {
                SmartLogger.LogWarning($"Zone '{DisplayName}': Target unit is not a DokkaebiUnit, cannot apply effects", LogCategory.Zone);
                return;
            }

            SmartLogger.Log($"Zone '{DisplayName}' applying effects to unit '{dokkaebiUnit.GetUnitName()}':", LogCategory.Zone);
            
            // Apply damage/healing if configured
            if (concreteZoneData.damagePerTurn != 0)
            {
                SmartLogger.Log($"- Applying {concreteZoneData.damagePerTurn} damage to {dokkaebiUnit.GetUnitName()}", LogCategory.Zone);
                dokkaebiUnit.TakeDamage(concreteZoneData.damagePerTurn, concreteZoneData.damageType);
            }
            
            if (concreteZoneData.healPerTurn != 0)
            {
                SmartLogger.Log($"- Applying {concreteZoneData.healPerTurn} healing to {dokkaebiUnit.GetUnitName()}", LogCategory.Zone);
                dokkaebiUnit.Heal(concreteZoneData.healPerTurn);
            }
            
            // Apply status effect if configured
            if (concreteZoneData.applyStatusEffect != null)
            {
                SmartLogger.Log($"- Applying status effect '{concreteZoneData.applyStatusEffect.displayName}' to {dokkaebiUnit.GetUnitName()}", LogCategory.Zone);
                var ownerUnit = ownerUnitId >= 0 ? UnitManager.Instance?.GetUnitById(ownerUnitId) : null;
                StatusEffectSystem.ApplyStatusEffect(dokkaebiUnit, concreteZoneData.applyStatusEffect, 1, ownerUnit);
            }
        }
        
        /// <summary>
        /// Processes turn-based logic for this zone
        /// </summary>
        public void ProcessTurn()
        {
            SmartLogger.Log($"Processing turn for zone {DisplayName} at {position}, remaining duration: {remainingDuration}", LogCategory.Zone, this);

            if (remainingDuration > 0)
            {
                remainingDuration--;
                if (remainingDuration <= 0)
                {
                    SmartLogger.Log($"Deactivating zone {DisplayName} at {position}", LogCategory.Zone, this);
                    Deactivate();
                }
            }
        }
        
        /// <summary>
        /// Deactivates this zone instance
        /// </summary>
        public void Deactivate()
        {
            // TODO: Implement any specific deactivation logic beyond fading
            isActive = false;
            StartFade();
        }
        
        /// <summary>
        /// Checks if this zone can merge with another zone
        /// </summary>
        public bool CanMergeWith(ZoneInstance otherZone)
        {
            // TODO: Implement logic to check if this zone can merge with another based on ZoneData
            if (zoneData == null || otherZone.zoneData == null || !CanMerge) return false;
            return MergesWithZoneIds.Contains(otherZone.zoneData.Id);
        }
        
        /// <summary>
        /// Merges this zone with another zone
        /// </summary>
        public void MergeWith(ZoneInstance otherZone)
        {
            SmartLogger.Log($"Merging zone {otherZone.DisplayName} into {DisplayName} at {position}", LogCategory.Zone, this);
            // Implementation of zone merging logic
        }
        
        /// <summary>
        /// Gets the grid position of this zone
        /// </summary>
        public GridPosition GetGridPosition()
        {
            return position;
        }
        
        /// <summary>
        /// Applies initial damage effects to units in a 3x3 area around the zone's position.
        /// Called immediately after zone creation.
        /// </summary>
        public void ApplyInitialEffects()
        {
            // Ensure we have valid zone data and the zone is active
            if (!IsActive || zoneData == null) 
            {
                return;
            }

            // Cast zoneData to the concrete ZoneData type once
            ZoneData concreteZoneData = zoneData as ZoneData;
            if (concreteZoneData == null || concreteZoneData.initialDamageAmount <= 0) 
            {
                return;
            }

            // Ensure UnitManager instance is available
            if (UnitManager.Instance == null)
            {
                SmartLogger.LogError("Cannot apply initial zone effects: UnitManager instance is null.", LogCategory.Zone);
                return;
            }

            // Iterate through all positions in a 3x3 square centered on the zone's position
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                for (int zOffset = -1; zOffset <= 1; zOffset++)
                {
                    GridPosition checkPos = new GridPosition(position.x + xOffset, position.z + zOffset);
                    
                    // Verify the position is valid
                    if (!GridManager.Instance.IsPositionValid(checkPos))
                    {
                        SmartLogger.Log($"Position {checkPos} is outside grid bounds, skipping", LogCategory.Zone);
                        continue;
                    }

                    var unit = UnitManager.Instance.GetUnitAtPosition(checkPos);
                    if (unit != null)
                    {
                        // Check if the unit should be affected based on allegiance
                        bool shouldAffect = concreteZoneData.affects switch
                        {
                            AllegianceTarget.Any => true,
                            AllegianceTarget.AllyOnly => unit.UnitId == ownerUnitId || unit.TeamId == UnitManager.Instance.GetUnitById(ownerUnitId)?.TeamId,
                            AllegianceTarget.EnemyOnly => unit.UnitId != ownerUnitId && unit.TeamId != UnitManager.Instance.GetUnitById(ownerUnitId)?.TeamId,
                            _ => false
                        };

                        if (shouldAffect)
                        {
                            // Apply initial damage
                            unit.TakeDamage(concreteZoneData.initialDamageAmount, concreteZoneData.initialDamageType);
                            SmartLogger.Log($"Applied initial zone damage {concreteZoneData.initialDamageAmount} to unit {unit.UnitId} at position {checkPos} (offset: {xOffset},{zOffset})", LogCategory.Zone);
                        }
                        else
                        {
                            SmartLogger.Log($"Unit {unit.UnitId} at position {checkPos} not affected due to allegiance rules", LogCategory.Zone);
                        }
                    }
                    else
                    {
                        SmartLogger.Log($"No unit found at position {checkPos} (offset: {xOffset},{zOffset})", LogCategory.Zone);
                    }
                }
            }
        }
        
        private void Update()
        {
            if (isFading)
            {
                currentAlpha = Mathf.MoveTowards(currentAlpha, 0f, Time.deltaTime * visualFadeSpeed);
                if (zoneVisual != null)
                {
                    var color = zoneVisual.color;
                    color.a = currentAlpha;
                    zoneVisual.color = color;
                }
                
                if (currentAlpha <= 0f)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
} 
