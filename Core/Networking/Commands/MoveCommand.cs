using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Grid;
using Dokkaebi.Units;
using Dokkaebi.Interfaces;
using Dokkaebi.Utilities;
using Dokkaebi.Common;

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
            Debug.Log($"[MoveCommand.Validate] Starting validation for Unit {UnitId} to position {TargetPosition}");
            
            var unitManager = Object.FindObjectOfType<UnitManager>();
            if (unitManager == null)
            {
                Debug.LogError("[MoveCommand.Validate] Cannot validate: UnitManager not found");
                return false;
            }

            DokkaebiUnit unit = unitManager.GetUnitById(UnitId);
            if (unit == null)
            {
                Debug.LogError($"[MoveCommand.Validate] Cannot move: Unit {UnitId} not found");
                return false;
            }

            // Check if the player owns this unit
            if (!unit.IsPlayer())
            {
                Debug.LogError($"[MoveCommand.Validate] Cannot move: Unit {UnitId} not owned by player");
                return false;
            }

            // Check 1: Check if it's the movement phase
            var turnSystemCore = Object.FindObjectOfType<DokkaebiTurnSystemCore>();
            bool canUnitMove = turnSystemCore != null && turnSystemCore.CanUnitMove(unit);
            Debug.Log($"[MoveCommand.Validate] Check 1: turnSystemCore.CanUnitMove({unit?.GetUnitName() ?? "NULL"}) = {canUnitMove}. Current Phase: {turnSystemCore?.CurrentPhase ?? TurnPhase.GameOver}");
            if (!canUnitMove)
            {
                Debug.LogError($"[MoveCommand.Validate] Validation failed at Check 1: CanUnitMove returned false.");
                return false;
            }

            // Check 2: Check if the unit has already moved
            bool hasPendingMove = unit.HasPendingMovement();
            Debug.Log($"[MoveCommand.Validate] Check 2: unit.HasPendingMovement() = {hasPendingMove}");
            if (hasPendingMove)
            {
                Debug.LogError($"[MoveCommand.Validate] Validation failed at Check 2: HasPendingMovement returned true.");
                return false;
            }

            // Check 3: Get and Log Valid Moves List
            var validMoves = unit.GetValidMovePositions();
            Debug.Log($"[MoveCommand.Validate] IMMEDIATELY after call: validMoves variable is {(validMoves == null ? "NULL" : "NOT NULL")}, Count = {(validMoves?.Count.ToString() ?? "N/A")}");
            GridPosition targetGridPos = DokkaebiGridConverter.Vector2IntToGrid(TargetPosition);
            System.Text.StringBuilder sb = StringBuilderPool.Get();
            sb.Append($"[MoveCommand.Validate] Check 3: Checking Target {targetGridPos} against list ({validMoves.Count} positions):");
            foreach(var pos in validMoves) { sb.Append($" {pos}"); }
            //Debug.Log(StringBuilderPool.GetStringAndReturn(sb));

            // Check 4: Contains Check
            bool targetIsValid = validMoves.Contains(targetGridPos);
            Debug.Log($"[MoveCommand.Validate] Check 4: validMoves.Contains(targetGridPos) = {targetIsValid}");
            if (!targetIsValid)
            {
                Debug.LogError($"[MoveCommand.Validate] Validation failed at Check 4: Target {targetGridPos} not in valid moves list.");
                return false;
            }

            Debug.Log($"[MoveCommand.Validate] Validation PASSED for Unit {UnitId} moving to {targetGridPos}.");
            return true;
        }

        public override void Execute()
        {
            // --- ADD LOG ---
            Debug.Log($"[MoveCommand.Execute] START - UnitID: {UnitId}, TargetPos: {TargetPosition}");

            // --- Log FindObjectOfType ---
            Debug.Log("[MoveCommand.Execute] Finding UnitManager...");
            var unitManager = Object.FindObjectOfType<UnitManager>();
            Debug.Log($"[MoveCommand.Execute] Found UnitManager? {(unitManager != null)}");
            if (unitManager == null)
            {
                // Using Debug.LogError for clarity on potential exit
                Debug.LogError("[MoveCommand.Execute] Cannot execute: UnitManager not found!");
                return;
            }

            // --- Log GetUnitById ---
            Debug.Log($"[MoveCommand.Execute] Getting Unit {UnitId} from UnitManager...");
            DokkaebiUnit unit = unitManager.GetUnitById(UnitId);
            Debug.Log($"[MoveCommand.Execute] Found Unit? {(unit != null)}. Unit Name: {(unit?.GetUnitName() ?? "N/A")}");
            if (unit == null)
            {
                // Using Debug.LogError for clarity
                Debug.LogError($"[MoveCommand.Execute] Cannot execute: Unit {UnitId} not found!");
                return;
            }

            // --- Log Conversion ---
            Debug.Log($"[MoveCommand.Execute] Converting TargetPosition {TargetPosition} to GridPosition...");
            GridPosition targetGridPos = DokkaebiGridConverter.Vector2IntToGrid(TargetPosition);
            Debug.Log($"[MoveCommand.Execute] Conversion result: {targetGridPos}");

            // --- Log SetTargetPosition Call ---
            Debug.Log($"[MoveCommand.Execute] Calling unit.SetTargetPosition({targetGridPos})...");
            try // Add try-catch for safety
            {
                unit.SetTargetPosition(targetGridPos);
                Debug.Log("[MoveCommand.Execute] unit.SetTargetPosition() completed.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MoveCommand.Execute] EXCEPTION during unit.SetTargetPosition(): {ex.Message}\n{ex.StackTrace}");
                return; // Stop execution if SetTargetPosition fails
            }

            // --- Log Final DebugLog ---
            // Keep the original DebugLog call using the base class method for comparison
            Debug.Log("[MoveCommand.Execute] Calling internal DebugLog...");
            DebugLog($"Set pending movement for unit {UnitId} to position {TargetPosition}"); // Uses internal DebugLog
            Debug.Log("[MoveCommand.Execute] FINISHED");
            // --- END ADD LOGS ---
        }
    }
} 