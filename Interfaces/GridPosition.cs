using System;
using UnityEngine;

namespace Dokkaebi.Interfaces
{
    /// <summary>
    /// Represents a position on a 2D grid
    /// </summary>
    [System.Serializable]
    public struct GridPosition
    {
        // Direct properties
        public int x;
        public int z;

        // Special instance for invalid positions
        public static readonly GridPosition invalid = new GridPosition(-1, -1);

        public GridPosition(int x, int z)
        {
            this.x = x;
            this.z = z;
        }

        public static GridPosition FromVector2Int(Vector2Int vector)
        {
            return new GridPosition(vector.x, vector.y);
        }

        public Vector2Int ToVector2Int()
        {
            return new Vector2Int(x, z);
        }

        public Vector3 ToWorldPosition(float y = 0)
        {
            // Convert to world position with optional height
            return new Vector3(x, y, z);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GridPosition)) return false;
            GridPosition other = (GridPosition)obj;
            return x == other.x && z == other.z;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ (z.GetHashCode() << 2);
        }

        public static bool operator ==(GridPosition a, GridPosition b)
        {
            return a.x == b.x && a.z == b.z;
        }

        public static bool operator !=(GridPosition a, GridPosition b)
        {
            return a.x != b.x || a.z != b.z;
        }

        /// <summary>
        /// Calculate Manhattan distance between two grid positions
        /// </summary>
        public static int GetManhattanDistance(GridPosition a, GridPosition b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
        }

        public override string ToString()
        {
            return $"({x}, {z})";
        }
    }
} 