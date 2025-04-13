using UnityEngine;
using System.Collections.Generic;
using Dokkaebi.Interfaces;
using Dokkaebi.Common;

namespace Dokkaebi.Core.Data
{
    /// <summary>
    /// Defines a character's origin (cultural/magical background)
    /// </summary>
    [CreateAssetMenu(fileName = "New Origin", menuName = "Dokkaebi/Data/Origin")]
    public class OriginData : ScriptableObject
    {
        [Header("Basic Information")]
        public string originId;
        public string displayName;
        public string description;
        public Sprite icon;
        
        [Header("Stats Modifiers")]
        public int healthModifier;
        public int auraModifier;
        public int movementModifier;
        
        [Header("Damage Modifiers")]
        public Dictionary<DamageType, float> damageResistances = new Dictionary<DamageType, float>();
        
        [Header("Abilities")]
        public List<AbilityData> innateAbilities;
        
        [Header("Theme")]
        public Color themeColor = Color.white;
        
        private void OnValidate()
        {
            // Ensure ID is not empty
            if (string.IsNullOrEmpty(originId))
            {
                originId = name;
            }
        }
    }
}