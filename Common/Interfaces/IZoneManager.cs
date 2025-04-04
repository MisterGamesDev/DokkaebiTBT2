using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Common
{
    /// <summary>
    /// Interface for zone management systems.
    /// This abstraction allows Grid code to interact with zones
    /// without directly depending on the Zones namespace.
    /// </summary>
    public interface IZoneManager
    {
        /// <summary>
        /// Check if a position has any zones on it
        /// </summary>
        bool HasZonesAt(GridPosition position);
        
        /// <summary>
        /// Get all zones at a specific position
        /// </summary>
        List<IZone> GetZonesAt(GridPosition position);
        
        /// <summary>
        /// Check if a position is in void space (a special type of zone that prevents
        /// other zones from being placed there)
        /// </summary>
        bool IsVoidSpace(GridPosition position);
        
        /// <summary>
        /// Get the movement cost multiplier for a position based on all zones at that position
        /// </summary>
        float GetMovementCostMultiplier(GridPosition position);
        
        /// <summary>
        /// Check if a position is valid for zone placement
        /// </summary>
        bool IsValidZonePosition(GridPosition position);
    }
} 