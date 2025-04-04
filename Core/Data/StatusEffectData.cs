using System;
using UnityEngine;
using Dokkaebi.Common;
using Dokkaebi.Interfaces;

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
        public StatusEffectType statusType => effectType;
        public int duration;
        public int potency;
        
        // Does this effect stack or just refresh duration?
        public bool canStack = false;
        public int maxStacks = 1;
        public bool isPermanent = false;
        
        // Visual effect prefab to show on affected unit
        public GameObject visualEffect;
        
        // Audio
        public AudioClip applySound;
        public AudioClip tickSound;
        public AudioClip removeSound;
        
        [SerializeField]
        private Color effectColor = Color.white;
        
        // IStatusEffect implementation
        string IStatusEffect.Id => effectId;
        string IStatusEffect.DisplayName => displayName;
        StatusEffectType IStatusEffect.EffectType => effectType;
        int IStatusEffect.DefaultDuration => duration;
        int IStatusEffect.Potency => potency;
        bool IStatusEffect.IsPermanent => isPermanent;
        Color IStatusEffect.EffectColor => effectColor;
        
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