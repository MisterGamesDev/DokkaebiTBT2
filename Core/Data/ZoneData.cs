using UnityEngine;
using System.Collections.Generic;
using Dokkaebi.Common; // Ensure this namespace is included
using Dokkaebi.Interfaces;

namespace Dokkaebi.Core.Data
{
    /// <summary>
    /// Defines a zone type that can be created on the battlefield
    /// </summary>
    [CreateAssetMenu(fileName = "New Zone", menuName = "Dokkaebi/Data/Zone")]
    public class ZoneData : ScriptableObject, IZoneData // Interface declaration remains
    {
        [Header("Basic Information")]
        // Keep your existing public fields:
        public string zoneId;
        public string displayName;
        public string description;
        public Sprite icon;

        [Header("Properties")]
        public int defaultDuration = 3;
        public bool isPermanent = false;
        public bool blocksMovement = false;
        public bool blocksLineOfSight = false;
        public int radius = 1;

        [Header("Effects")]
        public int initialDamageAmount = 0;
        public DamageType initialDamageType = DamageType.Physical;
        public int damagePerTurn = 0;
        // Assuming DamageType enum is accessible (likely from Dokkaebi.Common or Dokkaebi.Core.Data)
        public DamageType damageType = DamageType.Physical;
        public int healPerTurn = 0;
        // Assuming StatusEffectData is accessible (likely from Dokkaebi.Core.Data)
        public StatusEffectData applyStatusEffect;
        public AllegianceTarget affects = AllegianceTarget.Any;

        [Header("Movement Modifiers")]
        public float movementCostMultiplier = 1.0f;
        public float abilityCostMultiplier = 1.0f;

        [Header("Visuals")]
        public GameObject zonePrefab;
        public GameObject zoneVisualPrefab;
        public float zoneScale = 1.0f;
        public Color zoneColor = Color.white;
        public ParticleSystem particleEffect;

        [Header("Audio")]
        public AudioClip createSound;
        public AudioClip zoneCreationSound;
        public AudioClip zoneTickSound;
        public AudioClip ambientSound;
        public AudioClip destroySound;
        public AudioClip zoneMergeSound;
        public AudioClip zoneResonanceSound;

        [Header("Zone Interaction")]
        // Keep your existing public fields for interaction:
        public bool canMerge = false;
        public int maxStacks = 1;
        public List<string> mergesWithZoneIds = new List<string>();
        // Assuming OriginData is accessible (likely from Dokkaebi.Core.Data)
        public List<OriginData> resonanceOrigins = new List<OriginData>();
        public float resonanceEffectMultiplier = 1.5f;

        // --- Implicit Implementation for IZoneData members ---
        // These properties now directly implement the interface members AND are accessible on ZoneData instances.
        public string Id => zoneId; // Implicitly implements IZoneData.Id using the public field
        public string DisplayName => displayName; // Implicitly implements IZoneData.DisplayName
        public int DefaultDuration => defaultDuration; // Implicitly implements IZoneData.DefaultDuration
        public int Radius => radius; // Implicitly implements IZoneData.Radius
        public Color ZoneColor => zoneColor; // Implicitly implements IZoneData.ZoneColor

        // These were the members causing the original CS0535 error.
        // They implicitly implement the interface members using the public fields above.
        public bool CanMerge => canMerge;
        public int MaxStacks => maxStacks;
        public IReadOnlyList<string> MergesWithZoneIds => mergesWithZoneIds.AsReadOnly();

        // Implementation for ResonanceOrigins, converting List<OriginData> to IReadOnlyList<string>
        public IReadOnlyList<string> ResonanceOrigins
        {
            get
            {
                // Convert List<OriginData> to List<string> containing IDs
                List<string> originIds = new List<string>();
                foreach (var origin in resonanceOrigins) // Using existing public resonanceOrigins field
                {
                    if (origin != null && !string.IsNullOrEmpty(origin.originId))
                    {
                        originIds.Add(origin.originId); // Assuming OriginData has an 'originId' field
                    }
                }
                return originIds.AsReadOnly();
            }
        }
        public float ResonanceEffectMultiplier => resonanceEffectMultiplier;


        // --- OnValidate Method ---
        // (Keep your existing OnValidate method or add it back if it was removed)
        private void OnValidate()
        {
            // Ensure ID is not empty
            if (string.IsNullOrEmpty(zoneId))
            {
                zoneId = name;
            }

            // Validate permanent zones
            if (isPermanent)
            {
                defaultDuration = -1; // Use -1 or another convention for permanent
            }
            else if (defaultDuration <= 0 && !isPermanent)
            {
                 Debug.LogWarning($"Zone {zoneId}: Non-permanent zones should have a positive duration.");
                 defaultDuration = 1; // Set a minimum duration
            }

            // Validate damage and heal values
            if (damagePerTurn < 0)
            {
                Debug.LogWarning($"Zone {zoneId}: Damage per turn cannot be negative");
                damagePerTurn = 0;
            }

            if (healPerTurn < 0)
            {
                Debug.LogWarning($"Zone {zoneId}: Heal per turn cannot be negative");
                healPerTurn = 0;
            }

            // Validate radius
             if (radius < 0)
             {
                  Debug.LogWarning($"Zone {zoneId}: Radius cannot be negative");
                  radius = 0; // Radius 0 usually means only the center tile
             }

            // Auto-assign sounds/prefabs if needed (example)
            if (zoneCreationSound == null && createSound != null)
            {
                zoneCreationSound = createSound;
            }
            if (zoneVisualPrefab == null && zonePrefab != null)
            {
                zoneVisualPrefab = zonePrefab;
            }

            // Validate stack settings
             if (maxStacks < 1)
             {
                 Debug.LogWarning($"Zone {zoneId}: Max Stacks must be at least 1.");
                 maxStacks = 1;
             }
             if (!canMerge && maxStacks > 1)
             {
                  Debug.LogWarning($"Zone {zoneId}: Max Stacks is > 1 but CanMerge is false. Stacking might not work as expected.");
             }
        }
    }
}
