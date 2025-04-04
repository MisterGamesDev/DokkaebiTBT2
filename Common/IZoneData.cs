using UnityEngine;
using System.Collections.Generic;

namespace Dokkaebi.Common
{
    /// <summary>
    /// Interface for zone data to break cyclic dependencies.
    /// Contains only essential properties needed across modules.
    /// </summary>
    public interface IZoneData
    {
        /// <summary>
        /// Unique identifier for the zone type
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Display name of the zone
        /// </summary>
        string DisplayName { get; }
        
        /// <summary>
        /// Default duration in turns
        /// </summary>
        int DefaultDuration { get; }
        
        /// <summary>
        /// Radius of the zone in grid cells
        /// </summary>
        int Radius { get; }
        
        /// <summary>
        /// Color used for visual representation
        /// </summary>
        Color ZoneColor { get; }

        /// <summary>
        /// Whether this zone can merge with other zones
        /// </summary>
        bool CanMerge { get; }

        /// <summary>
        /// Maximum number of stacks this zone can have
        /// </summary>
        int MaxStacks { get; }

        /// <summary>
        /// List of zone IDs this zone can merge with
        /// </summary>
        IReadOnlyList<string> MergesWithZoneIds { get; }

        /// <summary>
        /// List of origins this zone can resonate with
        /// </summary>
        IReadOnlyList<string> ResonanceOrigins { get; }

        /// <summary>
        /// Multiplier for zone effects when resonating
        /// </summary>
        float ResonanceEffectMultiplier { get; }
    }
} 