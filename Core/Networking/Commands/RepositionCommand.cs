using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Grid;
using Dokkaebi.Units;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Core.Networking.Commands
{
    /// <summary>
    /// Command for tactically repositioning a unit outside the normal movement phase
    /// </summary>
    public class RepositionCommand : CommandBase
    {
        public int UnitId { get; private set; }
        public Vector2Int TargetPosition { get; private set; }

        // Required for deserialization
        public RepositionCommand() : base() { }

        public RepositionCommand(int unitId, Vector2Int targetPosition) : base()
        {
            UnitId = unitId;
            TargetPosition = targetPosition;
        }

        public override string CommandType => "reposition";

        public override Dictionary<string, object> Serialize()
        {
            var data = base.Serialize();
            data["unitId"] = UnitId;
            data["targetX"] = TargetPosition.x;
            data["targetY"] = TargetPosition.y;
            return data;
        }

        public override void Deserialize(Dictionary<string, object> data)
        {
            base.Deserialize(data);

            if (data.TryGetValue("unitId", out object unitIdObj))
            {
                if (unitIdObj is long unitIdLong)
                {
                    UnitId = (int)unitIdLong;
                }
                else if (unitIdObj is int unitIdInt)
                {
                    UnitId = unitIdInt;
                }
            }

            int x = 0, y = 0;
            if (data.TryGetValue("targetX", out object xObj))
            {
                if (xObj is long xLong)
                {
                    x = (int)xLong;
                }
                else if (xObj is int xInt)
                {
                    x = xInt;
                }
            }

            if (data.TryGetValue("targetY", out object yObj))
            {
                if (yObj is long yLong)
                {
                    y = (int)yLong;
                }
                else if (yObj is int yInt)
                {
                    y = yInt;
                }
            }

            TargetPosition = new Vector2Int(x, y);
        }

        public override bool Validate()
        {
            var unitManager = Object.FindObjectOfType<UnitManager>();
            if (unitManager == null)
            {
                DebugLog("Cannot validate: UnitManager not found");
                return false;
            }

            DokkaebiUnit unit = unitManager.GetUnitById(UnitId);
            if (unit == null)
            {
                DebugLog($"Cannot reposition: Unit {UnitId} not found");
                return false;
            }

            // Check if the player owns this unit
            if (!unit.IsPlayer())
            {
                DebugLog($"Cannot reposition: Unit {UnitId} not owned by player");
                return false;
            }

            // Check if it's a valid target position
            var gridManager = Object.FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                DebugLog("Cannot validate: GridManager not found");
                return false;
            }

            // Check if the position is a valid grid cell
            GridPosition targetGridPos = Dokkaebi.Interfaces.GridPosition.FromVector2Int(TargetPosition);
            if (!gridManager.IsPositionValid(targetGridPos))
            {
                DebugLog($"Cannot reposition: Position {TargetPosition} is not a valid grid position");
                return false;
            }

            // Check if the position is occupied
            if (gridManager.IsTileOccupied(targetGridPos))
            {
                DebugLog($"Cannot reposition: Position {TargetPosition} is already occupied");
                return false;
            }

            // Additional validation specific to tactical repositioning would go here
            // For example, checking if the player has enough "reposition tokens" or other resources

            return true;
        }

        public override void Execute()
        {
            var unitManager = Object.FindObjectOfType<UnitManager>();
            if (unitManager == null)
            {
                DebugLog("Cannot execute: UnitManager not found");
                return;
            }

            var gridManager = Object.FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                DebugLog("Cannot execute: GridManager not found");
                return;
            }

            DokkaebiUnit unit = unitManager.GetUnitById(UnitId);
            if (unit == null)
            {
                DebugLog($"Cannot execute: Unit {UnitId} not found");
                return;
            }

            // Get the current position
            GridPosition currentPos = unit.GetGridPosition();
            GridPosition targetGridPos = Dokkaebi.Interfaces.GridPosition.FromVector2Int(TargetPosition);

            // Update the grid (remove unit from old position, add to new position)
            gridManager.ClearUnitFromPreviousTile(unit);
            gridManager.SetTileOccupant(targetGridPos, unit);

            // Update the unit's position
            unit.SetGridPosition(targetGridPos);
            
            DebugLog($"Repositioned unit {UnitId} from {currentPos} to {targetGridPos}");
        }
    }
} 