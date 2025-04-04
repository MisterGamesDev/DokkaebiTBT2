using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Dokkaebi.Core.Data;

namespace Dokkaebi.Core
{
    /// <summary>
    /// Manages loading and providing access to all ScriptableObject data assets.
    /// Implements singleton pattern for global access.
    /// </summary>
    public class DataManager : MonoBehaviour
    {
        private static DataManager instance;
        public static DataManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("DataManager");
                    instance = go.AddComponent<DataManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        [Header("Data Assets")]
        [SerializeField] private List<OriginData> originDataAssets;
        [SerializeField] private List<CallingData> callingDataAssets;
        [SerializeField] private List<AbilityData> abilityDataAssets;
        [SerializeField] private List<ZoneData> zoneDataAssets;
        [SerializeField] private List<StatusEffectData> statusEffectDataAssets;
        [SerializeField] private UnitSpawnData unitSpawnData;

        // Cached dictionaries for quick lookup
        private Dictionary<string, OriginData> originDataLookup;
        private Dictionary<string, CallingData> callingDataLookup;
        private Dictionary<string, AbilityData> abilityDataLookup;
        private Dictionary<string, ZoneData> zoneDataLookup;
        private Dictionary<string, StatusEffectData> statusEffectDataLookup;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            InitializeDataLookups();
        }

        private void InitializeDataLookups()
        {
            // Initialize origin data lookup
            originDataLookup = originDataAssets.ToDictionary(x => x.originId);
            
            // Initialize calling data lookup
            callingDataLookup = callingDataAssets.ToDictionary(x => x.callingId);
            
            // Initialize ability data lookup
            abilityDataLookup = abilityDataAssets.ToDictionary(x => x.abilityId);
            
            // Initialize zone data lookup
            zoneDataLookup = zoneDataAssets.ToDictionary(x => x.zoneId);
            
            // Initialize status effect data lookup
            statusEffectDataLookup = statusEffectDataAssets.ToDictionary(x => x.effectId);
        }

        #region Data Access Methods

        public OriginData GetOriginData(string originId)
        {
            if (originDataLookup.TryGetValue(originId, out var data))
                return data;
            
            Debug.LogError($"Origin data not found for ID: {originId}");
            return null;
        }

        public CallingData GetCallingData(string callingId)
        {
            if (callingDataLookup.TryGetValue(callingId, out var data))
                return data;
            
            Debug.LogError($"Calling data not found for ID: {callingId}");
            return null;
        }

        public AbilityData GetAbilityData(string abilityId)
        {
            if (abilityDataLookup.TryGetValue(abilityId, out var data))
                return data;
            
            Debug.LogError($"Ability data not found for ID: {abilityId}");
            return null;
        }

        public ZoneData GetZoneData(string zoneId)
        {
            if (zoneDataLookup.TryGetValue(zoneId, out var data))
                return data;
            
            Debug.LogError($"Zone data not found for ID: {zoneId}");
            return null;
        }

        public StatusEffectData GetStatusEffectData(string effectId)
        {
            if (statusEffectDataLookup.TryGetValue(effectId, out var data))
                return data;
            
            Debug.LogError($"Status effect data not found for ID: {effectId}");
            return null;
        }

        public UnitSpawnData GetUnitSpawnData() {
            Debug.Log($"[DataManager.GetUnitSpawnData] Returning UnitSpawnData: {(unitSpawnData != null ? unitSpawnData.name : "NULL")}");
            return unitSpawnData;
        }

        #endregion

        #region Validation Methods

        public bool ValidateOriginId(string originId) => originDataLookup.ContainsKey(originId);
        public bool ValidateCallingId(string callingId) => callingDataLookup.ContainsKey(callingId);
        public bool ValidateAbilityId(string abilityId) => abilityDataLookup.ContainsKey(abilityId);
        public bool ValidateZoneId(string zoneId) => zoneDataLookup.ContainsKey(zoneId);
        public bool ValidateStatusEffectId(string effectId) => statusEffectDataLookup.ContainsKey(effectId);

        #endregion
    }
}