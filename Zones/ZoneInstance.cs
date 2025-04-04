using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Dokkaebi.Common;
using Dokkaebi.Interfaces;

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
            transform.position = position.ToWorldPosition();
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
        /// Applies this zone's effects to units within it
        /// </summary>
        public void ApplyZoneEffects()
        {
            // TODO: Implement logic to apply this zone's effects to units within it
            Debug.Log($"Applying effects for zone {DisplayName} at {position}");
        }
        
        /// <summary>
        /// Processes turn-based logic for this zone
        /// </summary>
        public void ProcessTurn()
        {
            // TODO: Implement turn processing logic
            DecreaseDuration(); // Tick down duration each turn
            Debug.Log($"Processing turn for zone {DisplayName} at {position}, remaining duration: {remainingDuration}");
        }
        
        /// <summary>
        /// Deactivates this zone instance
        /// </summary>
        public void Deactivate()
        {
            // TODO: Implement any specific deactivation logic beyond fading
            isActive = false;
            StartFade();
            Debug.Log($"Deactivating zone {DisplayName} at {position}");
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
            // TODO: Implement logic to merge this zone's properties with another
            AddStack();
            Destroy(otherZone.gameObject);
            Debug.Log($"Merging zone {otherZone.DisplayName} into {DisplayName} at {position}");
        }
        
        /// <summary>
        /// Gets the grid position of this zone
        /// </summary>
        public GridPosition GetGridPosition()
        {
            return position;
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