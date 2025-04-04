using System.Collections.Generic;
using UnityEngine;

namespace Dokkaebi.Core.Networking
{
    /// <summary>
    /// Manages the game state and updates it based on server data
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        private ZoneManager zoneManager;

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
                    Debug.Log($"Removing zone {zone.ZoneId} as it no longer exists in server state");
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
                        Debug.LogWarning($"Failed to create ZoneInstance for new zone {zoneState.ZoneId}");
                        // Clean up the simple Zone object since we couldn't create an instance
                        zoneManager.RemoveZone(newZone.ZoneId);
                    }
                }
                else
                {
                    Debug.LogError($"Failed to create Zone for state data {zoneState.ZoneId}");
                }
            }
        }

        /// <summary>
        /// Update a single zone's state based on server data
        /// </summary>
        private void UpdateZoneState(Zone zone, ZoneStateData zoneState)
        {
            if (zone == null || zoneState == null) return;

            // Update position if different
            if (zone.Position != zoneState.Position)
            {
                zone.SetPosition(zoneState.Position);
            }
            
            // Update duration if different
            if (zone.RemainingDuration != zoneState.RemainingDuration)
            {
                zone.SetDuration(zoneState.RemainingDuration);
            }
        }
    }
} 