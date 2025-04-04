namespace Dokkaebi.Interfaces
{
    /// <summary>
    /// Interface for Zone instances to break cyclic dependencies.
    /// Contains essential properties needed by grid and other systems.
    /// </summary>
    public interface IZoneInstance
    {
        /// <summary>
        /// The grid position of the zone's center
        /// </summary>
        GridPosition Position { get; }
        
        /// <summary>
        /// Unique identifier for the zone
        /// </summary>
        int Id { get; }
        
        /// <summary>
        /// The radius of the zone in grid cells
        /// </summary>
        int Radius { get; }
        
        /// <summary>
        /// Whether the zone is currently active
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// The remaining duration of the zone in turns (-1 for permanent)
        /// </summary>
        int RemainingDuration { get; }
        
        /// <summary>
        /// The ID of the unit that created this zone (if any)
        /// </summary>
        int OwnerUnitId { get; }
        
        /// <summary>
        /// Whether a given grid position is within this zone's area of effect
        /// </summary>
        bool ContainsPosition(GridPosition position);
    }
} 