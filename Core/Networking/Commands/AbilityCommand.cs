using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Grid;
using Dokkaebi.Units;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Core.Networking.Commands
{
    /// <summary>
    /// Command for using a unit's ability
    /// </summary>
    public class AbilityCommand : CommandBase
    {
        public int UnitId { get; private set; }
        public int AbilityIndex { get; private set; }
        public Vector2Int TargetPosition { get; private set; }

        // Required for deserialization
        public AbilityCommand() : base() { }

        public AbilityCommand(int unitId, int abilityIndex, Vector2Int targetPosition) : base()
        {
            UnitId = unitId;
            AbilityIndex = abilityIndex;
            TargetPosition = targetPosition;
        }

        public override string CommandType => "ability";

        public override Dictionary<string, object> Serialize()
        {
            var data = base.Serialize();
            data["unitId"] = UnitId;
            data["abilityIndex"] = AbilityIndex;
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

            if (data.TryGetValue("abilityIndex", out object abilityIndexObj))
            {
                if (abilityIndexObj is long abilityIndexLong)
                {
                    AbilityIndex = (int)abilityIndexLong;
                }
                else if (abilityIndexObj is int abilityIndexInt)
                {
                    AbilityIndex = abilityIndexInt;
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
                DebugLog($"Cannot use ability: Unit {UnitId} not found");
                return false;
            }

            // Check if the player owns this unit
            if (!unit.IsPlayer())
            {
                DebugLog($"Cannot use ability: Unit {UnitId} not owned by player");
                return false;
            }

            // Check if it's the aura phase
            var turnSystemCore = Object.FindObjectOfType<DokkaebiTurnSystemCore>();
            if (turnSystemCore != null && !turnSystemCore.CanUnitUseAura(unit))
            {
                DebugLog($"Cannot use ability: Not in aura phase or not unit's turn");
                return false;
            }

            // Check if the unit has already used an ability
            if (unit.HasPendingAbility())
            {
                DebugLog($"Cannot use ability: Unit {UnitId} has already used an ability");
                return false;
            }

            // Get the ability
            var abilities = unit.GetAbilities();
            if (AbilityIndex < 0 || AbilityIndex >= abilities.Count)
            {
                DebugLog($"Cannot use ability: Invalid ability index {AbilityIndex}");
                return false;
            }

            var ability = abilities[AbilityIndex];

            // Check if the ability is on cooldown
            if (unit.IsOnCooldown(ability.abilityType))
            {
                DebugLog($"Cannot use ability: Ability {ability.displayName} is on cooldown");
                return false;
            }

            // Check range
            GridPosition unitPos = unit.GetGridPosition();
            GridPosition targetGridPos = DokkaebiGridConverter.Vector2IntToGrid(TargetPosition);
            int distance = GridPosition.GetManhattanDistance(unitPos, targetGridPos);

            if (distance > ability.range)
            {
                DebugLog($"Cannot use ability: Target position {TargetPosition} is out of range (max range: {ability.range})");
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

            var abilityManager = Object.FindObjectOfType<AbilityManager>();
            if (abilityManager == null)
            {
                DebugLog("Cannot execute: AbilityManager not found");
                return;
            }

            DokkaebiUnit unit = unitManager.GetUnitById(UnitId);
            if (unit == null)
            {
                DebugLog($"Cannot execute: Unit {UnitId} not found");
                return;
            }

            // Get the ability
            var abilities = unit.GetAbilities();
            if (AbilityIndex < 0 || AbilityIndex >= abilities.Count)
            {
                DebugLog($"Cannot execute: Invalid ability index {AbilityIndex}");
                return;
            }

            var ability = abilities[AbilityIndex];
            GridPosition targetGridPos = DokkaebiGridConverter.Vector2IntToGrid(TargetPosition);

            // Set the pending ability for the unit
            unit.PlanAbilityUse(AbilityIndex, targetGridPos);
            
            DebugLog($"Set pending ability {ability.displayName} for unit {UnitId} targeting position {TargetPosition}");
        }
    }
} 