using UnityEngine;
using System.Collections.Generic;

namespace Dokkaebi.Core.Data
{
    /// <summary>
    /// Defines a character's calling (class/profession)
    /// </summary>
    [CreateAssetMenu(fileName = "New Calling", menuName = "Dokkaebi/Data/Calling")]
    public class CallingData : ScriptableObject
    {
        [Header("Basic Information")]
        public string callingId;
        public string displayName;
        public string description;
        public Sprite icon;
        
        [Header("Stats Modifiers")]
        public int healthModifier;
        public int auraModifier;
        public int movementModifier;
        
        [Header("Abilities")]
        public List<AbilityData> primaryAbilities;
        public List<AbilityData> secondaryAbilities;
        public AbilityData ultimateAbility;
        public AbilityData passiveAbility;
        
        [Header("Theme")]
        public Color themeColor = Color.white;
        public GameObject specialEffectPrefab;
        
        private void OnValidate()
        {
            // Ensure ID is not empty
            if (string.IsNullOrEmpty(callingId))
            {
                callingId = name;
            }
        }
    }
}