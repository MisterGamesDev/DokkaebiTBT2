using System;
using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Grid;
using Dokkaebi.Units;
using Dokkaebi.Zones;
using Dokkaebi.Common;
using Dokkaebi.Interfaces;
using System.Linq;
using Dokkaebi.Utilities;
using Dokkaebi.Core.Data;
using Dokkaebi.Core;
// using Dokkaebi.VFX; // Temporarily removed until VFXManager is implemented
// using Dokkaebi.Audio; // Temporarily removed until AudioManager is implemented

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
        // [SerializeField] private VFXManager vfxManager; // Temporarily commented out until VFXManager is implemented
        // [SerializeField] private AudioManager audioManager; // Temporarily commented out until AudioManager is implemented

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
            // if (vfxManager == null) vfxManager = FindObjectOfType<VFXManager>(); // Temporarily commented out until VFXManager is implemented
            // if (audioManager == null) audioManager = FindObjectOfType<AudioManager>(); // Temporarily commented out until AudioManager is implemented

            if (networkManager == null || turnSystem == null || unitManager == null || gridManager == null || zoneManager == null)
            {
                Debug.LogError("Required managers not found in scene!");
                return;
            }

            // if (vfxManager == null || audioManager == null)
            // {
            //     Debug.LogWarning("VFX or Audio manager not found. Visual/Audio effects will not play.");
            // }
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
            
            // Create lookup for unit state by ID for faster access
            Dictionary<string, UnitStateData> unitStates = new Dictionary<string, UnitStateData>();
            foreach (var unitState in gameState.Units)
            {
                unitStates[unitState.UnitId] = unitState;
            }
            
            // Get all active units
            var activeUnits = unitManager.GetUnitsByPlayer(true).Concat(unitManager.GetUnitsByPlayer(false));
            
            // Update existing units
            foreach (var unit in activeUnits)
            {
                if (unitStates.TryGetValue(unit.GetUnitId().ToString(), out UnitStateData unitState))
                {
                    // Update unit state
                    UpdateUnitState(unit, unitState);
                    
                    // Remove from dictionary to track which ones we've handled
                    unitStates.Remove(unit.GetUnitId().ToString());
                }
                else
                {
                    // Unit exists locally but not in server state - should be removed
                    SmartLogger.LogWarning($"Unit {unit.GetUnitId()} exists locally but not in server state", LogCategory.Unit);
                }
            }
        }

        /// <summary>
        /// Update a unit's state based on server data
        /// </summary>
        private void UpdateUnitState(DokkaebiUnit unit, UnitStateData unitState)
        {
            if (unit == null || unitState == null) return;

            // Store old values for comparison
            float oldHP = unit.CurrentHealth;
            var oldPosition = unit.CurrentGridPosition;
            // Get a copy of existing status effects to compare against later
            List<IStatusEffectInstance> oldStatusEffects = unit.GetStatusEffects()?.ToList() ?? new List<IStatusEffectInstance>();

            // Update basic properties
            unit.SetGridPosition(Interfaces.GridPosition.FromVector2Int(unitState.Position));
            unit.SetCurrentHealth(unitState.CurrentHP);
            unit.SetMaxHealth(unitState.MaxHP);
            unit.SetCurrentAura(Mathf.RoundToInt(unitState.CurrentMP));
            unit.SetMaxAura(Mathf.RoundToInt(unitState.MaxMP));
            unit.SetIsPlayerUnit(unitState.IsPlayerUnit);

            // Play effects based on state changes
            // Temporarily commented out until VFXManager and AudioManager are implemented
            /*
            if (vfxManager != null && audioManager != null)
            {
                Vector3 unitPosition = unit.transform.position;

                // Health changes
                if (unitState.CurrentHP < oldHP)
                {
                    // Damage taken
                    vfxManager.PlayDamageEffect(unitPosition);
                    vfxManager.ShowFloatingNumber(unitPosition, $"-{Mathf.RoundToInt(oldHP - unitState.CurrentHP)}", Color.red);
                    audioManager.PlayDamageSound();
                }
                else if (unitState.CurrentHP > oldHP)
                {
                    // Healing received
                    vfxManager.PlayHealEffect(unitPosition);
                    vfxManager.ShowFloatingNumber(unitPosition, $"+{Mathf.RoundToInt(unitState.CurrentHP - oldHP)}", Color.green);
                    audioManager.PlayHealSound();
                }

                // Status Effect changes - Compare new state list with old list
                List<IStatusEffectInstance> newStatusEffects = unitState.StatusEffects ?? new List<IStatusEffectInstance>();

                // Added Effects: Effects in new list that weren't in old list
                var addedEffects = newStatusEffects.Where(newEffect =>
                    newEffect != null && !oldStatusEffects.Any(oldEffect => 
                        oldEffect != null && oldEffect.StatusEffectType == newEffect.StatusEffectType)
                ).ToList();

                foreach (var addedEffect in addedEffects)
                {
                    vfxManager.PlayStatusEffectAddedEffect(unitPosition, addedEffect.StatusEffectType);
                    audioManager.PlayStatusEffectAddedSound(addedEffect.StatusEffectType);
                }

                // Removed Effects: Effects in old list that are not in new list
                var removedEffects = oldStatusEffects.Where(oldEffect =>
                    oldEffect != null && !newStatusEffects.Any(newEffect => 
                        newEffect != null && newEffect.StatusEffectType == oldEffect.StatusEffectType)
                ).ToList();

                foreach (var removedEffect in removedEffects)
                {
                    vfxManager.PlayStatusEffectRemovedEffect(unitPosition, removedEffect.StatusEffectType);
                    audioManager.PlayStatusEffectRemovedSound(removedEffect.StatusEffectType);
                }
            }
            */

            // Update unit state
            if (unit != null)
            {
                // Update position
                var gridPos = Interfaces.GridPosition.FromVector2Int(unitState.Position);
                if (gridPos.x != unit.GetGridPosition().x || gridPos.z != unit.GetGridPosition().z)
                {
                    unit.SetGridPosition(gridPos);
                }

                // Update health
                if (unitState.CurrentHP != unit.GetCurrentHealth())
                {
                    unit.SetCurrentHealth(unitState.CurrentHP);
                }

                // Update aura
                if (unitState.CurrentMP != unit.GetCurrentAura())
                {
                    unit.SetCurrentAura(Mathf.RoundToInt(unitState.CurrentMP));
                }

                // Update unit-specific aura
                if ((unitState.HasMoved ? 1 : 0) != unit.GetCurrentUnitAura())
                {
                    unit.SetCurrentUnitAura(unitState.HasMoved ? 1 : 0);
                }

                // Update movement state
                if (unitState.HasMoved != unit.HasPendingMovement())
                {
                    unit.SetPendingMovement(unitState.HasMoved);
                }
            }
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
                    // Update zone state
                    UpdateZoneState(zone, zoneState);
                    
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
        /// Update a zone's state based on server data
        /// </summary>
        private void UpdateZoneState(Zone zone, ZoneStateData zoneState)
        {
            if (zone == null || zoneState == null) return;

            // Update zone properties
            zone.SetDuration(zoneState.RemainingDuration);
            zone.SetPosition(zoneState.Position);
            
            // Update zone instance if it exists
            var gridPos = Interfaces.GridPosition.FromVector2Int(zone.Position);
            var zonesAtPosition = zoneManager.GetZonesAtPosition(gridPos);
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
