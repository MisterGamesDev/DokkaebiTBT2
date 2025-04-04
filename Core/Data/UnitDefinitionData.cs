using System;
using UnityEngine;
using System.Collections.Generic;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Core.Data
{
    /// <summary>
    /// Defines a unit type with base stats and abilities
    /// </summary>
    [CreateAssetMenu(fileName = "New Unit Definition", menuName = "Dokkaebi/Data/Unit Definition")]
    public class UnitDefinitionData : ScriptableObject
    {
        [Header("Basic Information")]
        public string unitId;
        public string displayName;
        public string description;
        public Sprite icon;
        public GameObject unitPrefab;
        
        [Header("Identity")]
        public OriginData origin;
        public CallingData calling;
        
        [Header("Base Stats")]
        public int baseHealth = 100;
        public int baseAura = 10;
        public int baseMovement = 3;
        
        [Header("Abilities")]
        public List<AbilityData> abilities = new List<AbilityData>();
        
        [Header("Resistances/Vulnerabilities")]
        public Dictionary<DamageType, float> damageResistances = new Dictionary<DamageType, float>();
        
        [Header("Other Properties")]
        public bool isPlayerControlled = true;
        public bool canOverload = true;
        
        private void OnValidate()
        {
            // Ensure ID is not empty
            if (string.IsNullOrEmpty(unitId))
            {
                unitId = name;
            }
            
            // Validate base stats
            if (baseHealth <= 0)
            {
                Debug.LogWarning($"Unit {unitId} has invalid base health!");
                baseHealth = 1;
            }
            
            if (baseAura < 0)
            {
                Debug.LogWarning($"Unit {unitId} has invalid base aura!");
                baseAura = 0;
            }
            
            if (baseMovement <= 0)
            {
                Debug.LogWarning($"Unit {unitId} has invalid base movement!");
                baseMovement = 1;
            }
        }
    }
}