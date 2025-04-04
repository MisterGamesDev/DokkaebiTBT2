using System;
using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Grid;
using Dokkaebi.Units;
using Dokkaebi.Zones;
using Dokkaebi.Common;
using System.Linq;
using Dokkaebi.Utilities;

namespace Dokkaebi.Core.Networking
{
    /// <summary>
    /// Manages the local game state based on authoritative updates from the server
    /// Applies state changes to corresponding game objects and components
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private NetworkingManager networkManager;
        [SerializeField] private DokkaebiTurnSystemCore turnSystem;
        [SerializeField] private UnitManager unitManager;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private ZoneManager zoneManager;

        // The most recent game state from the server
        private GameStateData currentGameState;
        
        // Events
        public event Action<GameStateData> OnGameStateUpdated;
        public event Action<Dictionary<string, object>> OnRawGameStateUpdated;

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

            // Get component references
            if (networkManager == null) networkManager = FindObjectOfType<NetworkingManager>();
            if (turnSystem == null) turnSystem = FindObjectOfType<DokkaebiTurnSystemCore>();
            if (unitManager == null) unitManager = FindObjectOfType<UnitManager>();
            if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
            if (zoneManager == null) zoneManager = FindObjectOfType<ZoneManager>();

            if (networkManager == null || turnSystem == null || unitManager == null || gridManager == null || zoneManager == null)
            {
                Debug.LogError("Required managers not found in scene!");
                return;
            }
        }

        private void Start()
        {
            // Subscribe to network state updates
            if (networkManager != null)
            {
                networkManager.OnGameStateUpdated += HandleRawGameStateUpdate;
            }
            else
            {
                Debug.LogError("NetworkingManager not found. Game state synchronization will not work.");
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (networkManager != null)
            {
                networkManager.OnGameStateUpdated -= HandleRawGameStateUpdate;
            }
        }

        /// <summary>
        /// Handle raw game state update from the server
        /// </summary>
        private void HandleRawGameStateUpdate(Dictionary<string, object> rawGameState)
        {
            // Forward the raw state to any listeners
            OnRawGameStateUpdated?.Invoke(rawGameState);

            // Parse the raw game state into typed objects
            GameStateData newState = GameStateData.FromDictionary(rawGameState);
            
            // Cache the current state
            currentGameState = newState;
            
            // Apply the new state to the game
            ApplyGameState(newState);
            
            // Notify listeners about the typed state update
            OnGameStateUpdated?.Invoke(newState);
        }

        /// <summary>
        /// Apply game state received from the server to the local game objects
        /// </summary>
        private void ApplyGameState(GameStateData gameState)
        {
            // Update turn state
            UpdateTurnState(gameState);
            
            // Update units
            UpdateUnits(gameState);
            
            // Update zones
            UpdateZones(gameState);
        }

        /// <summary>
        /// Update the turn state based on server data
        /// </summary>
        private void UpdateTurnState(GameStateData gameState)
        {
            // Update turn system state
            if (turnSystem != null)
            {
                // Set the state in the turn system
                turnSystem.SetState(gameState.CurrentTurn, gameState.CurrentPhase, gameState.IsPlayer1Turn ? 1 : 0);
            }
            else
            {
                Debug.LogError("No turn system found. Unable to update turn state!");
            }
        }

        /// <summary>
        /// Update unit states based on server data
        /// </summary>
        private void UpdateUnits(GameStateData gameState)
        {
            if (unitManager == null || gameState.Units == null) return;
            
            // TEMPORARY - Commented out code that uses missing methods/properties
            /*
            // Create lookup for unit state by ID for faster access
            Dictionary<string, UnitStateData> unitStates = new Dictionary<string, UnitStateData>();
            foreach (var unitState in gameState.Units)
            {
                unitStates[unitState.UnitId] = unitState;
            }
            
            // Get all active units
            var activeUnits = unitManager.GetAllUnits();
            
            // Update existing units
            foreach (var unit in activeUnits)
            {
                if (unitStates.TryGetValue(unit.UnitId, out UnitStateData unitState))
                {
                    // Update unit state
                    UpdateUnitState(unit, unitState);
                    
                    // Remove from dictionary to track which ones we've handled
                    unitStates.Remove(unit.UnitId);
                }
                else
                {
                    // Unit exists locally but not in server state - should be removed
                    // This would be implemented in a production version, but for prototype
                    // we'll just log a warning
                    Debug.LogWarning($"Unit {unit.UnitId} exists locally but not in server state");
                }
            }
            
            // Handle units that exist in server state but not locally
            // For the prototype, we'll just log this
            foreach (var unitState in unitStates.Values)
            {
                Debug.LogWarning($"Unit {unitState.UnitId} exists in server state but not locally");
                // In a full implementation, we would instantiate these units
            }
            */
            
            // Temporary stub for compilation
            Debug.Log($"Would update {gameState.Units.Count} units based on server state");
        }

        /// <summary>
        /// Update a single unit's state based on server data
        /// </summary>
        private void UpdateUnitState(DokkaebiUnit unit, UnitStateData unitState)
        {
            // TEMPORARY - Commented out code that uses missing properties/methods
            /*
            // Update position if different
            if (unit.GridPosition != unitState.Position)
            {
                unit.SetPosition(unitState.Position);
            }
            
            // Update HP if different
            if (unit.CurrentHP != unitState.CurrentHP)
            {
                unit.SetHP(unitState.CurrentHP);
            }
            
            // Update MP if different
            if (Mathf.Abs(unit.CurrentMP - unitState.CurrentMP) > 0.01f)
            {
                unit.SetMP(unitState.CurrentMP);
            }
            
            // Update action states
            unit.SetActionState(unitState.HasMoved, unitState.HasUsedAbility);
            
            // Update abilities and cooldowns
            if (unitState.Abilities != null)
            {
                for (int i = 0; i < unitState.Abilities.Count && i < unit.Abilities.Count; i++)
                {
                    var ability = unit.Abilities[i];
                    var abilityState = unitState.Abilities[i];
                    
                    // Update cooldown
                    ability.SetCooldown(abilityState.CurrentCooldown);
                }
            }
            */
            
            // Temporary stub for compilation
            Debug.Log($"Would update unit {unit.name} with state data");
            
            // Update status effects
            // In a full implementation, we would update status effects here
        }

        /// <summary>
        /// Update zones based on server data
        /// </summary>
        private void UpdateZones(GameStateData gameState)
        {
            if (zoneManager == null || gameState.Zones == null) return;
            
            // Create lookup for zone state by ID for faster access
            Dictionary<string, ZoneStateData> zoneStates = new Dictionary<string, ZoneStateData>();
            foreach (var zoneState in gameState.Zones)
            {
                zoneStates[zoneState.ZoneId] = zoneState;
            }
            
            // Get all active zones
            var activeZones = zoneManager.GetAllZones();
            
            // Update existing zones
            foreach (var zone in activeZones)
            {
                if (zoneStates.TryGetValue(zone.ZoneId, out ZoneStateData zoneState))
                {
                    // Convert Vector2Int position to GridPosition
                    var gridPos = Dokkaebi.Interfaces.GridPosition.FromVector2Int(zone.Position);
                    
                    // Get all ZoneInstances at this position
                    var zonesAtPosition = zoneManager.GetZonesAtPosition(gridPos);
                    
                    // Find the matching ZoneInstance
                    var targetInstance = zonesAtPosition.FirstOrDefault(instance => 
                        instance != null && 
                        instance.Id.ToString() == zone.ZoneId && 
                        instance.IsActive);
                    
                    if (targetInstance != null)
                    {
                        // Re-initialize the instance with updated data
                        targetInstance.Initialize(
                            DataManager.Instance.GetZoneData(zoneState.ZoneType),
                            gridPos,
                            int.TryParse(zoneState.OwnerUnitId, out int ownerId) ? ownerId : -1,
                            zoneState.RemainingDuration
                        );
                        
                        SmartLogger.Log($"Updated ZoneInstance {targetInstance.Id} duration to {zoneState.RemainingDuration}", LogCategory.Zone);
                    }
                    else
                    {
                        SmartLogger.LogWarning($"Could not find matching ZoneInstance for Zone ID {zone.ZoneId} at position {gridPos}", LogCategory.Zone);
                    }
                    
                    // Update the simple Zone object as well
                    zone.SetDuration(zoneState.RemainingDuration);
                    zone.SetPosition(zoneState.Position);
                    
                    // Remove from dictionary to track which ones we've handled
                    zoneStates.Remove(zone.ZoneId);
                }
                else
                {
                    // Zone exists locally but not in server state - should be removed
                    zoneManager.RemoveZone(zone.ZoneId);
                    SmartLogger.Log($"Removing zone {zone.ZoneId} as it no longer exists in server state", LogCategory.Zone);
                }
            }
            
            // Create new zones that exist in server state but not locally
            foreach (var zoneState in zoneStates.Values)
            {
                // Create a new Zone object from the state data
                var newZone = zoneManager.CreateZone(
                    zoneState.ZoneType,
                    zoneState.Position,
                    zoneState.Size,
                    zoneState.RemainingDuration,
                    zoneState.OwnerUnitId
                );

                if (newZone != null)
                {
                    // Convert the simple Zone to a gameplay ZoneInstance
                    var zoneInstance = zoneManager.CreateInstanceFromZone(newZone);
                    
                    if (zoneInstance == null)
                    {
                        SmartLogger.LogWarning($"Failed to create ZoneInstance for new zone {zoneState.ZoneId}", LogCategory.Zone);
                        // Clean up the simple Zone object since we couldn't create an instance
                        zoneManager.RemoveZone(newZone.ZoneId);
                    }
                }
                else
                {
                    SmartLogger.LogError($"Failed to create Zone for state data {zoneState.ZoneId}", LogCategory.Zone);
                }
            }
        }

        /// <summary>
        /// Get the current game state
        /// </summary>
        public GameStateData GetCurrentGameState()
        {
            return currentGameState;
        }

        /// <summary>
        /// Manually request a game state update from the server
        /// </summary>
        public void RequestGameStateUpdate()
        {
            if (networkManager != null)
            {
                networkManager.GetGameState();
            }
        }
    }
} 