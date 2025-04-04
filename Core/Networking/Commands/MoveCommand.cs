using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Grid;
using Dokkaebi.Units;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Core.Networking.Commands
{
    /// <summary>
    /// Command for moving a unit to a new position
    /// </summary>
    public class MoveCommand : CommandBase
    {
        public int UnitId { get; private set; }
        public Vector2Int TargetPosition { get; private set; }

        // Required for deserialization
        public MoveCommand() : base() { }

        public MoveCommand(int unitId, Vector2Int targetPosition) : base()
        {
            UnitId = unitId;
            TargetPosition = targetPosition;
        }

        public override string CommandType => "move";

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
                DebugLog($"Cannot move: Unit {UnitId} not found");
                return false;
            }

            // Check if the player owns this unit
            if (!unit.IsPlayer())
            {
                DebugLog($"Cannot move: Unit {UnitId} not owned by player");
                return false;
            }

            // Check if it's the movement phase
            var turnSystemCore = Object.FindObjectOfType<DokkaebiTurnSystemCore>();
            if (turnSystemCore != null && !turnSystemCore.CanUnitMove(unit))
            {
                DebugLog($"Cannot move: Not in movement phase or not unit's turn");
                return false;
            }

            // Check if the unit has already moved
            if (unit.HasPendingMovement())
            {
                DebugLog($"Cannot move: Unit {UnitId} has already moved");
                return false;
            }

            // Check if the position is within the valid range
            var validMoves = unit.GetValidMovePositions();
            GridPosition targetGridPos = DokkaebiGridConverter.Vector2IntToGrid(TargetPosition);
            
            if (!validMoves.Contains(targetGridPos))
            {
                DebugLog($"Cannot move: Position {TargetPosition} is not a valid move position");
                return false;
            }

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

            DokkaebiUnit unit = unitManager.GetUnitById(UnitId);
            if (unit == null)
            {
                DebugLog($"Cannot execute: Unit {UnitId} not found");
                return;
            }

            // Set the pending movement for the unit
            GridPosition targetGridPos = DokkaebiGridConverter.Vector2IntToGrid(TargetPosition);
            Debug.Log($"[MoveCommand] Execute: Received TargetPosition (Vector2Int)={TargetPosition}, Calculated targetGridPos (GridPosition)={targetGridPos}");
            unit.SetTargetPosition(targetGridPos);
            
            DebugLog($"Set pending movement for unit {UnitId} to position {TargetPosition}");
        }
    }
} 