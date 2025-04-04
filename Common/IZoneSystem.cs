using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Common
{
    /// <summary>
    /// Interface for the zone system to break cyclic dependencies
    /// </summary>
    public interface IZoneSystem
    {
        /// <summary>
        /// Get all active zones in the game
        /// </summary>
        IEnumerable<IZone> GetAllZones();
        
        /// <summary>
        /// Get all zones at a specific grid position
        /// </summary>
        IEnumerable<IZone> GetZonesAtPosition(GridPosition position);
        
        /// <summary>
        /// Check if a grid position has a zone
        /// </summary>
        bool HasZoneAtPosition(GridPosition position);
        
        /// <summary>
        /// Check if a grid position has a zone of a specific type
        /// </summary>
        bool HasZoneOfTypeAtPosition(GridPosition position, ZoneType zoneType);
        
        /// <summary>
        /// Create a new zone at the specified positions
        /// </summary>
        IZone CreateZone(ZoneType zoneType, int ownerUnitId, IEnumerable<GridPosition> positions, int duration = -1);
        
        /// <summary>
        /// Remove a zone by its ID
        /// </summary>
        void RemoveZone(string zoneId);
    }
} 