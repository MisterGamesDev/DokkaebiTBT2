using UnityEngine;
using System.Collections.Generic;

namespace Dokkaebi.Core.Data
{
    /// <summary>
    /// Defines unit spawning configuration for a map or scenario
    /// </summary>
    [System.Serializable]
    public class UnitSpawnConfig
    {
        public UnitDefinitionData unitDefinition;
        public Vector2Int spawnPosition;
    }

    [CreateAssetMenu(fileName = "UnitSpawnData", menuName = "Dokkaebi/Data/UnitSpawnData")]
    public class UnitSpawnData : ScriptableObject
    {
        [Header("Player Units")]
        public List<UnitSpawnConfig> playerUnitSpawns = new List<UnitSpawnConfig>();
        
        [Header("Enemy Units")]
        public List<UnitSpawnConfig> enemyUnitSpawns = new List<UnitSpawnConfig>();
        
        [Header("Neutral Units")]
        public List<UnitSpawnConfig> neutralUnitSpawns = new List<UnitSpawnConfig>();
        
        [Header("Spawn Settings")]
        public bool randomizePositions = false;
        public bool respectDefaultFactions = true;
        
        [Header("Special Settings")]
        public bool applyStatusEffectsOnSpawn = false;
        public List<StatusEffectData> globalSpawnStatusEffects;
        
        private void OnValidate()
        {
            // Validate player units
            foreach (var unit in playerUnitSpawns)
            {
                if (unit.unitDefinition == null)
                {
                    Debug.LogWarning("Player unit missing definition!");
                }
            }
            
            // Validate enemy units
            foreach (var unit in enemyUnitSpawns)
            {
                if (unit.unitDefinition == null)
                {
                    Debug.LogWarning("Enemy unit missing definition!");
                }
            }
            
            // Validate neutral units
            foreach (var unit in neutralUnitSpawns)
            {
                if (unit.unitDefinition == null)
                {
                    Debug.LogWarning("Neutral unit missing definition!");
                }
            }
        }
    }
}
