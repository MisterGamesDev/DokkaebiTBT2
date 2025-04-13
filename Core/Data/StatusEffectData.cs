using System;
using UnityEngine;
using Dokkaebi.Common;
using Dokkaebi.Interfaces;
using System.Collections.Generic;

namespace Dokkaebi.Core.Data
{
    /// <summary>
    /// Defines a status effect that can be applied to units
    /// </summary>
    [CreateAssetMenu(fileName = "New Status Effect", menuName = "Dokkaebi/Data/Status Effect")]
    public class StatusEffectData : ScriptableObject, IStatusEffect
    {
        public string effectId;
        public string effectName;
        public string displayName;
        public string description;
        public Sprite icon;
        
        public StatusEffectType effectType;
        public bool isStackable = false;
        public int maxStacks = 1;
        public bool isPermanent = false;
        public int duration;
        public int potency;
        
        // Visual effect prefab to show on affected unit
        public GameObject visualEffect;
        
        // Audio
        public AudioClip applySound;
        public AudioClip tickSound;
        public AudioClip removeSound;
        
        [SerializeField]
        private Color effectColor = Color.white;
        
        // Stat modifiers
        [Header("Stat Modifiers")]
        [SerializeField]
        private Dictionary<UnitAttributeType, float> statModifiers = new Dictionary<UnitAttributeType, float>();
        
        // Periodic Effects
        [Header("Periodic Effects")]
        public bool hasDamageOverTime = false;
        public int damageOverTimeAmount = 0;
        public DamageType damageOverTimeType = DamageType.Normal;

        public bool hasHealingOverTime = false;
        public int healingOverTimeAmount = 0;

        // Immediate Effects
        [Header("Immediate Effects")]
        public bool hasImmediateDamage = false;
        public int immediateDamageAmount = 0;
        public DamageType immediateDamageType = DamageType.Normal;

        public bool hasImmediateHealing = false;
        public int immediateHealingAmount = 0;
        
        // IStatusEffect implementation
        string IStatusEffect.Id => effectId;
        string IStatusEffect.DisplayName => displayName;
        StatusEffectType IStatusEffect.EffectType => effectType;
        int IStatusEffect.DefaultDuration => duration;
        int IStatusEffect.Potency => potency;
        bool IStatusEffect.IsPermanent => isPermanent;
        Color IStatusEffect.EffectColor => effectColor;
        
        /// <summary>
        /// Check if this effect modifies a specific stat
        /// </summary>
        public bool HasStatModifier(UnitAttributeType statType)
        {
            return statModifiers.ContainsKey(statType);
        }
        
        /// <summary>
        /// Get the modifier value for a specific stat
        /// </summary>
        public float GetStatModifier(UnitAttributeType statType)
        {
            return statModifiers.TryGetValue(statType, out float value) ? value : 1.0f;
        }
        
        /// <summary>
        /// Set a stat modifier
        /// </summary>
        public void SetStatModifier(UnitAttributeType statType, float value)
        {
            statModifiers[statType] = value;
        }
        
        private void OnValidate()
        {
            // Auto-generate effectId if empty
            if (string.IsNullOrEmpty(effectId))
            {
                effectId = name;
            }
            
            // Auto-generate displayName if empty
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = effectName;
            }
        }
    }
}