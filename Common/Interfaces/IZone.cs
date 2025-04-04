using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Common
{
    /// <summary>
    /// Interface for a zone instance.
    /// Provides the minimal representation of a zone for the Grid system.
    /// </summary>
    public interface IZone
    {
        /// <summary>
        /// Unique identifier for the zone
        /// </summary>
        string ZoneId { get; }
        
        /// <summary>
        /// Display name of the zone
        /// </summary>
        string ZoneName { get; }
        
        /// <summary>
        /// Grid position of the zone
        /// </summary>
        GridPosition Position { get; }
        
        /// <summary>
        /// Whether the zone affects movement costs
        /// </summary>
        bool AffectsMovement { get; }
        
        /// <summary>
        /// Movement cost multiplier for this zone
        /// </summary>
        float MovementCostMultiplier { get; }
        
        /// <summary>
        /// Whether the zone is still active
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// Get all grid positions covered by this zone
        /// </summary>
        IEnumerable<GridPosition> GetCoveredPositions();
        
        /// <summary>
        /// Check if this zone contains a specific grid position
        /// </summary>
        bool ContainsPosition(GridPosition position);
    }
} 