using System;
using System.Collections.Generic;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Common
{
    /// <summary>
    /// Represents a direction on the grid
    /// </summary>
    public enum Direction
    {
        None = 0,
        
        // Cardinal directions (orthogonal)
        North = 1,
        East = 2,
        South = 3,
        West = 4,
        
        // Ordinal directions (diagonal)
        NorthEast = 5,
        SouthEast = 6,
        SouthWest = 7,
        NorthWest = 8
    }
    
    /// <summary>
    /// Extension methods for working with directions
    /// </summary>
    public static class DirectionExtensions
    {
        /// <summary>
        /// Get the cardinal directions (no diagonals)
        /// </summary>
        public static readonly Direction[] Cardinals = new[]
        {
            Direction.North,
            Direction.East,
            Direction.South, 
            Direction.West
        };
        
        /// <summary>
        /// Get the ordinal directions (diagonals)
        /// </summary>
        public static readonly Direction[] Ordinals = new[]
        {
            Direction.NorthEast,
            Direction.SouthEast,
            Direction.SouthWest,
            Direction.NorthWest
        };
        
        /// <summary>
        /// Get all directions (cardinal and ordinal)
        /// </summary>
        public static readonly Direction[] All = new[]
        {
            Direction.North,
            Direction.East,
            Direction.South,
            Direction.West,
            Direction.NorthEast,
            Direction.SouthEast,
            Direction.SouthWest,
            Direction.NorthWest
        };
        
        /// <summary>
        /// Mapping of directions to their opposite directions
        /// </summary>
        private static readonly Dictionary<Direction, Direction> OppositeDirections = new Dictionary<Direction, Direction>
        {
            { Direction.North, Direction.South },
            { Direction.East, Direction.West },
            { Direction.South, Direction.North },
            { Direction.West, Direction.East },
            { Direction.NorthEast, Direction.SouthWest },
            { Direction.SouthEast, Direction.NorthWest },
            { Direction.SouthWest, Direction.NorthEast },
            { Direction.NorthWest, Direction.SouthEast },
            { Direction.None, Direction.None }
        };
        
        /// <summary>
        /// Mapping of directions to their grid position offsets
        /// </summary>
        private static readonly Dictionary<Direction, GridPosition> DirectionOffsets = new Dictionary<Direction, GridPosition>
        {
            { Direction.North, new GridPosition(0, 1) },
            { Direction.East, new GridPosition(1, 0) },
            { Direction.South, new GridPosition(0, -1) },
            { Direction.West, new GridPosition(-1, 0) },
            { Direction.NorthEast, new GridPosition(1, 1) },
            { Direction.SouthEast, new GridPosition(1, -1) },
            { Direction.SouthWest, new GridPosition(-1, -1) },
            { Direction.NorthWest, new GridPosition(-1, 1) },
            { Direction.None, new GridPosition(0, 0) }
        };
        
        /// <summary>
        /// Get the opposite direction
        /// </summary>
        public static Direction GetOpposite(this Direction direction)
        {
            return OppositeDirections.ContainsKey(direction) ? OppositeDirections[direction] : Direction.None;
        }
        
        /// <summary>
        /// Get the grid position offset for a direction
        /// </summary>
        public static GridPosition GetOffset(this Direction direction)
        {
            return DirectionOffsets.ContainsKey(direction) ? DirectionOffsets[direction] : new GridPosition(0, 0);
        }
        
        /// <summary>
        /// Get the direction from one grid position to another
        /// </summary>
        public static Direction GetDirectionBetween(GridPosition from, GridPosition to)
{
    // Calculate offset component-wise instead of direct subtraction
    GridPosition offset = new GridPosition(to.x - from.x, to.z - from.z);

    // Normalize offset to -1, 0, or 1 in each dimension
    // Use lowercase .x and .z
    int x = Math.Sign(offset.x);
    int y = Math.Sign(offset.z); // Use .z for the second component

    if (x == 0 && y == 0) return Direction.None;
    if (x == 0 && y > 0) return Direction.North;
    // ... rest of the conditions remain the same ...
    if (x < 0 && y > 0) return Direction.NorthWest;

    return Direction.None;
}
        
        /// <summary>
        /// Check if a direction is a cardinal direction
        /// </summary>
        public static bool IsCardinal(this Direction direction)
        {
            return direction == Direction.North || 
                   direction == Direction.East || 
                   direction == Direction.South || 
                   direction == Direction.West;
        }
        
        /// <summary>
        /// Check if a direction is an ordinal (diagonal) direction
        /// </summary>
        public static bool IsOrdinal(this Direction direction)
        {
            return direction == Direction.NorthEast || 
                   direction == Direction.SouthEast || 
                   direction == Direction.SouthWest || 
                   direction == Direction.NorthWest;
        }
    }
} 