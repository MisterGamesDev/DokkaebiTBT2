using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Dokkaebi.Grid;
using Dokkaebi.Units;
using Dokkaebi.Utilities;
using Dokkaebi.Interfaces;
using Dokkaebi.Common;
using Dokkaebi.Core.Data;
using Dokkaebi.Core;

namespace Dokkaebi.Zones
{
    /// <summary>
    /// Manages the creation, interaction, and lifecycle of all zones in the game.
    /// </summary>
    public class ZoneManager : MonoBehaviour
    {
        public static ZoneManager Instance { get; private set; }
        
        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private Transform zonesParent;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject zoneInstancePrefab;
        [SerializeField] private GameObject volatileZonePrefab;
        
        [Header("Settings")]
        [SerializeField] private int maxZonesPerTile = 4;
        [SerializeField] private int voidSpaceDuration = 2;
        
        // Track active zones by position
        private Dictionary<Interfaces.GridPosition, List<ZoneInstance>> zonesByPosition = new Dictionary<Interfaces.GridPosition, List<ZoneInstance>>();
        
        // Track void spaces
        private Dictionary<Interfaces.GridPosition, int> voidSpaces = new Dictionary<Interfaces.GridPosition, int>();
        
        // Track all active zones
        private Dictionary<string, Zone> activeZones = new Dictionary<string, Zone>();
        
        // Turn system reference
        private Interfaces.ITurnSystem turnSystem;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            if (zoneInstancePrefab == null)
            {
                Debug.LogError("ZoneManager: Zone instance prefab not assigned!");
            }
            
            // Create zones parent if it doesn't exist
            if (zonesParent == null)
            {
                zonesParent = new GameObject("Zones").transform;
                zonesParent.SetParent(transform);
            }
            
            // Get references if needed
            if (gridManager == null)
            {
                gridManager = FindObjectOfType<GridManager>();
            }
        }
        
        private void Start()
        {
            // Find GridManager if not assigned
            if (gridManager == null)
            {
                gridManager = FindObjectOfType<GridManager>();
                if (gridManager == null)
                {
                    Debug.LogWarning("ZoneManager: GridManager reference not found!");
                }
            }
            
            // Find turn system
            var turnSystemObj = FindObjectOfType<MonoBehaviour>() as Interfaces.ITurnSystem;
            if (turnSystemObj != null)
            {
                turnSystem = turnSystemObj;
                turnSystem.OnTurnResolutionEnd += HandleTurnResolutionEnd;
                SmartLogger.Log("ZoneManager subscribed to turn resolution end", LogCategory.Zone);
            }
            else
            {
                SmartLogger.LogWarning("ZoneManager: TurnSystem not found, zone effects may not be applied properly", LogCategory.Zone);
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from turn resolution end event
            if (turnSystem != null)
            {
                turnSystem.OnTurnResolutionEnd -= HandleTurnResolutionEnd;
            }
        }
        
        /// <summary>
        /// Handles the end of turn resolution when all actions are complete
        /// </summary>
        private void HandleTurnResolutionEnd()
        {
            SmartLogger.Log("ZoneManager: Processing all zone effects at turn end", LogCategory.Zone);
            
            // Apply effects for all active zones and update durations
            ProcessAllZoneEffectsAndDurations();
        }
        
        /// <summary>
        /// Process all active zones, apply their effects to units, and update durations
        /// </summary>
        private void ProcessAllZoneEffectsAndDurations()
        {
            if (zonesByPosition == null || zonesByPosition.Count == 0)
            {
                return;
            }
            
            SmartLogger.Log($"Processing effects for {zonesByPosition.Count} zone positions", LogCategory.Zone);
            
            // Process all zones
            Dictionary<Interfaces.GridPosition, List<ZoneInstance>> zonesCopy = new Dictionary<Interfaces.GridPosition, List<ZoneInstance>>(zonesByPosition);
            foreach (var positionZones in zonesCopy)
            {
                // First apply effects for all zones at this position
                foreach (ZoneInstance zone in positionZones.Value)
                {
                    if (zone != null && zone.IsActive)
                    {
                        // Apply zone effects to units at this position
                        zone.ApplyZoneEffects();
                    }
                }
                
                // Then process turn (duration reduction, etc.)
                for (int i = positionZones.Value.Count - 1; i >= 0; i--)
                {
                    ZoneInstance zone = positionZones.Value[i];
                    if (zone.IsActive)
                    {
                        zone.ProcessTurn();
                    }
                    else
                    {
                        positionZones.Value.RemoveAt(i);
                    }
                }
                
                // Remove empty lists
                if (positionZones.Value.Count == 0)
                {
                    zonesByPosition.Remove(positionZones.Key);
                }
            }
            
            // Process void spaces
            ProcessVoidSpaces();
        }
        
        /// <summary>
        /// Create a zone at the specified position with the given parameters
        /// </summary>
        public ZoneInstance CreateZone(
            Interfaces.GridPosition position,
            IZoneData zoneTypeData,
            DokkaebiUnit ownerUnit,
            int duration = -1)
        {
            // Check if position is valid
            if (!IsValidPosition(position))
            {
                Debug.LogWarning($"ZoneManager: Cannot create zone at invalid position {position}");
                return null;
            }
            
            // Check if the tile is in void space
            if (IsVoidSpace(position))
            {
                Debug.LogWarning($"ZoneManager: Cannot create zone in void space at {position}");
                return null;
            }
            
            // Get or create the list of zones at this position
            if (!zonesByPosition.TryGetValue(position, out var zonesAtPosition))
            {
                zonesAtPosition = new List<ZoneInstance>();
                zonesByPosition[position] = zonesAtPosition;
            }
            
            // Check for unstable resonance (too many zones)
            if (zonesAtPosition.Count >= maxZonesPerTile)
            {
                HandleUnstableResonance(position);
                return null;
            }
            
            // Instantiate the zone instance
            GameObject zoneObject = Instantiate(zoneInstancePrefab, Vector3.zero, Quaternion.identity);
            zoneObject.name = $"Zone_{zoneTypeData.DisplayName}_{position}";
            
            // Get and initialize the zone instance component
            ZoneInstance zoneInstance = zoneObject.GetComponent<ZoneInstance>();
            if (zoneInstance == null)
            {
                zoneInstance = zoneObject.AddComponent<ZoneInstance>();
            }
            
            // Extract the owner unit ID from the DokkaebiUnit
            int ownerUnitId = ownerUnit != null ? ownerUnit.UnitId : -1;
            
            // Initialize the zone with the provided parameters
            zoneInstance.Initialize(
                zoneTypeData,
                position,
                ownerUnitId,
                duration > 0 ? duration : zoneTypeData.DefaultDuration
            );
            
            // Add to the list of zones at this position
            zonesAtPosition.Add(zoneInstance);
            
            return zoneInstance;
        }
        
        /// <summary>
        /// Process the turn end for all zones
        /// </summary>
        public void ProcessTurn()
        {
            // Process all zones
            foreach (var positionZones in zonesByPosition)
            {
                for (int i = positionZones.Value.Count - 1; i >= 0; i--)
                {
                    ZoneInstance zone = positionZones.Value[i];
                    if (zone.IsActive)
                    {
                        zone.ProcessTurn();
                    }
                    else
                    {
                        positionZones.Value.RemoveAt(i);
                    }
                }
                
                // Remove empty lists
                if (positionZones.Value.Count == 0)
                {
                    zonesByPosition.Remove(positionZones.Key);
                }
            }
            
            // Process void spaces
            ProcessVoidSpaces();
        }
        
        /// <summary>
        /// Handle the case where too many zones are on a tile, causing unstable resonance
        /// </summary>
        private void HandleUnstableResonance(Interfaces.GridPosition position)
        {
            Debug.Log($"Unstable Resonance triggered at {position}");
            
            // Create a volatile zone (damaging zone) for 1 turn
            if (volatileZonePrefab != null)
            {
                GameObject volatileObj = Instantiate(volatileZonePrefab, DokkaebiGridConverter.GridToWorld(position), Quaternion.identity);
                volatileObj.name = $"VolatileZone_{position}";
                
                // TODO: Initialize volatile zone with appropriate effects
                // This would typically be done through some other means
            }
            
            // Clear all zones at this position
            if (zonesByPosition.TryGetValue(position, out var zonesAtPosition))
            {
                foreach (var zone in zonesAtPosition)
                {
                    if (zone != null && zone.IsActive)
                    {
                        zone.Deactivate();
                    }
                }
                
                zonesAtPosition.Clear();
                zonesByPosition.Remove(position);
            }
            
            // Mark this position as void space for a certain duration
            voidSpaces[position] = voidSpaceDuration;
        }
        
        /// <summary>
        /// Update the state of void spaces
        /// </summary>
        private void ProcessVoidSpaces()
        {
            List<Interfaces.GridPosition> expiredVoidSpaces = new List<Interfaces.GridPosition>();
            
            foreach (var kvp in voidSpaces)
            {
                int remainingDuration = kvp.Value - 1;
                
                if (remainingDuration <= 0)
                {
                    expiredVoidSpaces.Add(kvp.Key);
                }
                else
                {
                    voidSpaces[kvp.Key] = remainingDuration;
                }
            }
            
            // Remove expired void spaces
            foreach (var position in expiredVoidSpaces)
            {
                voidSpaces.Remove(position);
            }
        }
        
        /// <summary>
        /// Check for possible merging of the new zone with existing zones
        /// </summary>
        private void CheckForZoneMerging(ZoneInstance newZone, List<ZoneInstance> zonesAtPosition)
        {
            if (zonesAtPosition.Count <= 1) return;
            
            foreach (var existingZone in zonesAtPosition.ToArray())
            {
                if (existingZone != newZone && existingZone.IsActive && newZone.IsActive)
                {
                    if (newZone.CanMergeWith(existingZone))
                    {
                        newZone.MergeWith(existingZone);
                        
                        // Remove the merged zone from the list
                        zonesAtPosition.Remove(existingZone);
                    }
                }
            }
        }
        
        /// <summary>
        /// Find all zones at a specific position
        /// </summary>
        public List<ZoneInstance> GetZonesAtPosition(Interfaces.GridPosition position)
        {
            if (zonesByPosition.TryGetValue(position, out var zones))
            {
                return new List<ZoneInstance>(zones);
            }
            
            return new List<ZoneInstance>();
        }
        
        /// <summary>
        /// Check if a tile is in void space
        /// </summary>
        public bool IsVoidSpace(Interfaces.GridPosition position)
        {
            return voidSpaces.ContainsKey(position);
        }
        
        /// <summary>
        /// Check if a position is valid for zone placement
        /// </summary>
        private bool IsValidPosition(Interfaces.GridPosition position)
        {
            // Use GridManager if available
            if (gridManager != null)
            {
                return gridManager.IsPositionValid(position);
            }
            
            // Fallback to default grid size
            return position.x >= 0 && position.x < 10 && position.z >= 0 && position.z < 10;
        }
        
        /// <summary>
        /// Trigger a terrain shift on a zone (move it to a new position)
        /// </summary>
        public bool ShiftZone(ZoneInstance zone, Interfaces.GridPosition newPosition)
        {
            if (zone == null || !zone.IsActive || !IsValidPosition(newPosition))
            {
                return false;
            }
            
            // Get current position
            Interfaces.GridPosition oldPosition = zone.GetGridPosition();
            
            // Remove from old position
            if (zonesByPosition.TryGetValue(oldPosition, out var zonesAtOldPos))
            {
                zonesAtOldPos.Remove(zone);
                
                if (zonesAtOldPos.Count == 0)
                {
                    zonesByPosition.Remove(oldPosition);
                }
            }
            
            // Add to new position
            if (!zonesByPosition.TryGetValue(newPosition, out var zonesAtNewPos))
            {
                zonesAtNewPos = new List<ZoneInstance>();
                zonesByPosition[newPosition] = zonesAtNewPos;
            }
            
            zonesAtNewPos.Add(zone);
            
            // Move the zone visually
            GameObject zoneObject = zone.gameObject;
            zoneObject.transform.position = DokkaebiGridConverter.GridToWorld(newPosition);
            
            // Check for merging at new position
            CheckForZoneMerging(zone, zonesAtNewPos);
            
            return true;
        }
        
        /// <summary>
        /// Clear all zones
        /// </summary>
        public void ClearAllZones()
        {
            foreach (var positionZones in zonesByPosition.Values)
            {
                foreach (var zone in positionZones)
                {
                    if (zone != null)
                    {
                        Destroy(zone.gameObject);
                    }
                }
                
                positionZones.Clear();
            }
            
            zonesByPosition.Clear();
            voidSpaces.Clear();
        }
        
        /// <summary>
        /// Create a new zone at the specified position
        /// </summary>
        public Zone CreateZone(string zoneType, Vector2Int position, int size, int duration, string ownerUnitId = "")
        {
            // Convert Vector2Int to GridPosition using the utility
            var gridPosition = Dokkaebi.Interfaces.GridPosition.FromVector2Int(position);
            
            // Generate a unique ID for the zone
            string zoneId = System.Guid.NewGuid().ToString();
            
            // Determine which prefab to use based on zone type
            GameObject prefab = GetZonePrefabByType(zoneType);
            
            // Instantiate the zone
            GameObject zoneObj = Instantiate(prefab, Vector3.zero, Quaternion.identity, zonesParent);
            zoneObj.name = $"Zone_{zoneType}_{zoneId.Substring(0, 8)}";
            
            // Get and initialize zone component
            Zone zone = zoneObj.GetComponent<Zone>();
            if (zone == null)
            {
                zone = zoneObj.AddComponent<Zone>();
            }
            
            // Initialize zone data using Vector2Int
            zone.Initialize(zoneId, zoneType, position, size, duration, ownerUnitId);
            zone.SetPosition(position);
            
            // Add to active zones
            activeZones[zoneId] = zone;
            
            return zone;
        }
        
        /// <summary>
        /// Get the appropriate zone prefab based on zone type
        /// </summary>
        private GameObject GetZonePrefabByType(string zoneType)
        {
            // Use the existing zoneInstancePrefab or volatileZonePrefab
            switch (zoneType.ToLower())
            {
                case "damage":
                    return volatileZonePrefab != null ? volatileZonePrefab : zoneInstancePrefab;
                case "healing":
                    return volatileZonePrefab != null ? volatileZonePrefab : zoneInstancePrefab;
                default:
                    return zoneInstancePrefab;
            }
        }
        
        /// <summary>
        /// Create a ZoneInstance from a simple Zone object
        /// </summary>
        public ZoneInstance CreateInstanceFromZone(Zone zone)
        {
            if (zone == null)
            {
                SmartLogger.LogWarning("Cannot create ZoneInstance from null Zone", LogCategory.Zone);
                return null;
            }

            // Get zone data from DataManager based on zone type
            var zoneData = DataManager.Instance.GetZoneData(zone.ZoneType);
            if (zoneData == null)
            {
                SmartLogger.LogError($"Failed to find ZoneData for type {zone.ZoneType}", LogCategory.Zone);
                return null;
            }

            // Convert Vector2Int position to GridPosition
            var gridPosition = GridPosition.FromVector2Int(zone.Position);

            // Create zone instance
            GameObject zoneObject = Instantiate(zoneInstancePrefab, Vector3.zero, Quaternion.identity);
            zoneObject.name = $"ZoneInstance_{zoneData.DisplayName}_{zone.ZoneId}";

            // Get and initialize the zone instance component
            ZoneInstance zoneInstance = zoneObject.GetComponent<ZoneInstance>();
            if (zoneInstance == null)
            {
                zoneInstance = zoneObject.AddComponent<ZoneInstance>();
            }

            // Parse owner unit ID to int (or -1 if invalid)
            int ownerUnitId = -1;
            if (!string.IsNullOrEmpty(zone.OwnerUnitId))
            {
                int.TryParse(zone.OwnerUnitId, out ownerUnitId);
            }

            // Initialize the zone instance
            zoneInstance.Initialize(
                zoneData,
                gridPosition,
                ownerUnitId,
                zone.RemainingDuration
            );

            // Add to the list of zones at this position
            if (!zonesByPosition.TryGetValue(gridPosition, out var zonesAtPosition))
            {
                zonesAtPosition = new List<ZoneInstance>();
                zonesByPosition[gridPosition] = zonesAtPosition;
            }
            zonesAtPosition.Add(zoneInstance);

            return zoneInstance;
        }

        /// <summary>
        /// Create a simple Zone object from a ZoneInstance
        /// </summary>
        public Zone CreateZoneFromInstance(ZoneInstance instance)
        {
            if (instance == null)
            {
                SmartLogger.LogWarning("Cannot create Zone from null ZoneInstance", LogCategory.Zone);
                return null;
            }

            // Generate a unique ID for the zone
            string zoneId = System.Guid.NewGuid().ToString();

            // Get the zone type from the instance's data
            string zoneType = instance.DisplayName;

            // Convert GridPosition to Vector2Int
            Vector2Int position = instance.Position.ToVector2Int();

            // Create the zone object
            GameObject zoneObj = new GameObject($"Zone_{zoneType}_{zoneId.Substring(0, 8)}");
            zoneObj.transform.SetParent(zonesParent);

            // Add and initialize the Zone component
            Zone zone = zoneObj.AddComponent<Zone>();
            zone.Initialize(
                zoneId,
                zoneType,
                position,
                instance.Radius,
                instance.RemainingDuration,
                instance.OwnerUnitId.ToString()
            );

            // Add to active zones
            activeZones[zoneId] = zone;

            return zone;
        }

        /// <summary>
        /// Get a zone by ID
        /// </summary>
        public Zone GetZone(string zoneId)
        {
            if (activeZones.TryGetValue(zoneId, out Zone zone))
            {
                return zone;
            }
            return null;
        }
        
        /// <summary>
        /// Get all currently active zones
        /// </summary>
        public List<Zone> GetAllZones()
        {
            return new List<Zone>(activeZones.Values);
        }
        
        /// <summary>
        /// Get zones at a specific position
        /// </summary>
        public List<Zone> GetZonesAtPosition(Vector2Int position)
        {
            List<Zone> result = new List<Zone>();
            
            foreach (var zone in activeZones.Values)
            {
                if (zone.Position == position)
                {
                    result.Add(zone);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Remove a zone by ID
        /// </summary>
        public void RemoveZone(string zoneId)
        {
            if (activeZones.TryGetValue(zoneId, out Zone zone))
            {
                Destroy(zone.gameObject);
                activeZones.Remove(zoneId);
            }
        }
        
        /// <summary>
        /// Update zones at the end of a turn
        /// </summary>
        public void ProcessTurnEnd()
        {
            List<string> expiredZoneIds = new List<string>();
            
            // Decrement duration for all zones
            foreach (var zone in activeZones.Values)
            {
                zone.DecrementDuration();
                
                // Track expired zones
                if (zone.IsExpired())
                {
                    expiredZoneIds.Add(zone.ZoneId);
                }
            }
            
            // Remove expired zones
            foreach (string zoneId in expiredZoneIds)
            {
                RemoveZone(zoneId);
            }
        }
        
        /// <summary>
        /// Clear all active zones
        /// </summary>
        public void ClearAllActiveZones()
        {
            foreach (var zone in new List<Zone>(activeZones.Values))
            {
                Destroy(zone.gameObject);
            }
            
            activeZones.Clear();
        }
    }
} 