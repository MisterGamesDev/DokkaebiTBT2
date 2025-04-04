using UnityEngine;
using Dokkaebi.Common;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Grid
{
    public class GridCell
    {
        public GridPosition Position { get; private set; }
        public bool IsOccupied { get; set; }
        public IDokkaebiUnit OccupyingUnit { get; set; }
        public bool IsWalkable { get; set; } = true;
        public float Height { get; set; }

        public GridCell(GridPosition position)
        {
            Position = position;
            IsOccupied = false;
            OccupyingUnit = null;
        }

        public void Clear()
        {
            IsOccupied = false;
            OccupyingUnit = null;
        }
    }
} 