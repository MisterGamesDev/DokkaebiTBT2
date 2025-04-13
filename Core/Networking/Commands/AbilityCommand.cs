using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Grid;
using Dokkaebi.Units;
using Dokkaebi.Interfaces;
using Dokkaebi.Core.Data;
using Dokkaebi.Utilities;
using Dokkaebi.Core;

namespace Dokkaebi.Core.Networking.Commands
{
    /// <summary>
    /// Command for planning a unit's ability use
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
            SmartLogger.Log($"[AbilityCommand.Validate] --- Start Validation --- Unit: {UnitId}, AbilityIdx: {AbilityIndex}, Target: {TargetPosition}", LogCategory.Ability);
            
            var unitManager = UnitManager.Instance;
            SmartLogger.Log($"[AbilityCommand.Validate] Checking UnitManager... Found: {unitManager != null}", LogCategory.Ability);
            
            if (unitManager == null)
            {
                SmartLogger.LogWarning("[AbilityCommand.Validate] FAILED: UnitManager not found", LogCategory.Ability);
                return false;
            }

            var unit = unitManager.GetUnitById(UnitId);
            SmartLogger.Log($"[AbilityCommand.Validate] Checking Unit... Found: {(unit != null ? unit.GetUnitName() : "NULL")}", LogCategory.Ability);
            
            if (unit == null)
            {
                SmartLogger.LogWarning($"[AbilityCommand.Validate] FAILED: Unit {UnitId} not found", LogCategory.Ability);
                return false;
            }

            SmartLogger.Log($"[AbilityCommand.Validate] Checking Ownership... IsPlayer: {unit.IsPlayer()}", LogCategory.Ability);
            
            if (!unit.IsPlayer())
            {
                SmartLogger.LogWarning($"[AbilityCommand.Validate] FAILED: Unit {UnitId} not owned by player", LogCategory.Ability);
                return false;
            }

            var turnSystemCore = DokkaebiTurnSystemCore.Instance;
            var canUseAuraCheck = turnSystemCore?.CanUnitUseAura(unit) ?? false;
            
            SmartLogger.Log($"[AbilityCommand.Validate] Checking Turn/Phase... turnSystemCore Found: {turnSystemCore != null}, CanUnitUseAura result: {canUseAuraCheck}", LogCategory.Ability);
            
            if (!canUseAuraCheck)
            {
                SmartLogger.LogWarning($"[AbilityCommand.Validate] FAILED: Not correct turn/phase or unit cannot use aura. Current Phase: {turnSystemCore?.CurrentPhase}", LogCategory.Ability);
                return false;
            }

            var abilities = unit.GetAbilities();
            var abilityIndexValid = AbilityIndex >= 0 && AbilityIndex < abilities.Count;
            SmartLogger.Log($"[AbilityCommand.Validate] Checking Ability Index... Index: {AbilityIndex}, Count: {abilities.Count}, Valid: {abilityIndexValid}", LogCategory.Ability);
            
            if (!abilityIndexValid)
            {
                SmartLogger.LogWarning($"[AbilityCommand.Validate] FAILED: Invalid ability index {AbilityIndex}", LogCategory.Ability);
                return false;
            }

            var ability = abilities[AbilityIndex];
            var isOnCooldownCheck = unit.IsOnCooldown(ability.abilityType);
            SmartLogger.Log($"[AbilityCommand.Validate] Checking Cooldown... AbilityType: {ability.abilityType}, IsOnCooldown: {isOnCooldownCheck}", LogCategory.Ability);
            
            if (isOnCooldownCheck)
            {
                SmartLogger.LogWarning($"[AbilityCommand.Validate] FAILED: Ability {ability.displayName} is on cooldown", LogCategory.Ability);
                return false;
            }

            // Convert positions and calculate grid-based distance
            GridPosition unitPos = unit.GetGridPosition();
            GridPosition targetGridPos = GridPosition.FromVector2Int(TargetPosition);
            int distance = GridPosition.GetManhattanDistance(unitPos, targetGridPos);
            bool rangeCheck = distance <= ability.range;
            SmartLogger.Log($"[AbilityCommand.Validate] Checking Range... Distance: {distance}, AbilityRange: {ability.range}, InRange: {rangeCheck}", LogCategory.Ability);
            
            if (!rangeCheck)
            {
                SmartLogger.LogWarning($"[AbilityCommand.Validate] FAILED: Target position {TargetPosition} is out of range (max range: {ability.range})", LogCategory.Ability);
                return false;
            }

            bool targetCheck = ValidateTarget(unit, ability, targetGridPos);
            SmartLogger.Log($"[AbilityCommand.Validate] Checking Target Validity... Valid: {targetCheck}", LogCategory.Ability);
            
            if (!targetCheck)
            {
                SmartLogger.LogWarning($"[AbilityCommand.Validate] FAILED: Invalid target for ability {ability.displayName}", LogCategory.Ability);
                return false;
            }

            SmartLogger.Log("[AbilityCommand.Validate] --- Validation PASSED ---", LogCategory.Ability);
            return true;
        }

        private bool ValidateTarget(DokkaebiUnit unit, AbilityData ability, GridPosition targetPos)
        {
            SmartLogger.Log($"[AbilityCommand.ValidateTarget] Starting target validation for ability {ability.displayName}", LogCategory.Ability);
            
            var unitManager = UnitManager.Instance;
            SmartLogger.Log($"[AbilityCommand.ValidateTarget] Checking UnitManager... Found: {unitManager != null}", LogCategory.Ability);
            
            if (unitManager == null)
            {
                SmartLogger.LogWarning("[AbilityCommand.ValidateTarget] FAILED: UnitManager not found", LogCategory.Ability);
                return false;
            }

            var targetUnit = unitManager.GetUnitAtPosition(targetPos);
            SmartLogger.Log($"[AbilityCommand.ValidateTarget] Target unit at position {targetPos}: {(targetUnit != null ? targetUnit.GetUnitName() : "NULL")}", LogCategory.Ability);

            var isSelfTarget = targetUnit == unit;
            var canTargetSelf = ability.targetsSelf;
            SmartLogger.Log($"[AbilityCommand.ValidateTarget] Self-targeting check - Is self: {isSelfTarget}, Can target self: {canTargetSelf}, Unit IDs match: {targetUnit?.GetUnitId() == unit?.GetUnitId()}", LogCategory.Ability);
            
            if (isSelfTarget && !canTargetSelf)
            {
                SmartLogger.LogWarning("[AbilityCommand.ValidateTarget] FAILED: Cannot target self with this ability", LogCategory.Ability);
                return false;
            }

            var isGroundTarget = targetUnit == null;
            var canTargetGround = ability.targetsGround;
            SmartLogger.Log($"[AbilityCommand.ValidateTarget] Ground-targeting check - Is ground: {isGroundTarget}, Can target ground: {canTargetGround}, Grid position valid: {GridManager.Instance?.IsValidGridPosition(targetPos) ?? false}", LogCategory.Ability);
            
            if (isGroundTarget && !canTargetGround)
            {
                SmartLogger.LogWarning("[AbilityCommand.ValidateTarget] FAILED: Cannot target ground with this ability", LogCategory.Ability);
                return false;
            }

            if (targetUnit != null)
            {
                var isAlly = targetUnit.IsPlayer() == unit.IsPlayer();
                SmartLogger.Log($"[AbilityCommand.ValidateTarget] Ally/Enemy check - Is ally: {isAlly}, Can target ally: {ability.targetsAlly}, Can target enemy: {ability.targetsEnemy}, Source IsPlayer: {unit.IsPlayer()}, Target IsPlayer: {targetUnit.IsPlayer()}", LogCategory.Ability);
                
                if (isAlly && !ability.targetsAlly)
                {
                    SmartLogger.LogWarning("[AbilityCommand.ValidateTarget] FAILED: Cannot target allies with this ability", LogCategory.Ability);
                    return false;
                }
                
                if (!isAlly && !ability.targetsEnemy)
                {
                    SmartLogger.LogWarning("[AbilityCommand.ValidateTarget] FAILED: Cannot target enemies with this ability", LogCategory.Ability);
                    return false;
                }
            }

            SmartLogger.Log("[AbilityCommand.ValidateTarget] Target validation PASSED", LogCategory.Ability);
            return true;
        }

        public override void Execute()
        {
            var unitManager = UnitManager.Instance;
            var abilityManager = UnityEngine.Object.FindObjectOfType<AbilityManager>();
            
            if (unitManager == null || abilityManager == null)
            {
                SmartLogger.LogError("[AbilityCommand.Execute] UnitManager or AbilityManager not found", LogCategory.Ability);
                return;
            }

            var unit = unitManager.GetUnitById(UnitId);
            if (unit == null)
            {
                SmartLogger.LogError($"[AbilityCommand.Execute] Unit {UnitId} not found", LogCategory.Ability);
                return;
            }

            // Get ability data
            var abilities = unit.GetAbilities();
            if (AbilityIndex < 0 || AbilityIndex >= abilities.Count)
            {
                SmartLogger.LogError($"[AbilityCommand.Execute] Invalid ability index {AbilityIndex}", LogCategory.Ability);
                return;
            }
            var abilityData = abilities[AbilityIndex];

            // Convert Vector2Int to GridPosition for target
            GridPosition targetGridPos = new GridPosition(TargetPosition.x, TargetPosition.y);
            var targetUnit = unitManager.GetUnitAtPosition(targetGridPos);

            // Determine if ability should be overloaded
            bool isOverload = unit.GetCurrentMP() >= 7 && abilityData.requiresOverload;

            SmartLogger.Log($"[AbilityCommand.Execute] Executing ability {abilityData.displayName} from unit {unit.GetDisplayName()} targeting position {targetGridPos} (Unit: {targetUnit?.GetDisplayName() ?? "None"})", LogCategory.Ability);
            
            abilityManager.ExecuteAbility(abilityData, unit, targetGridPos, targetUnit as DokkaebiUnit, isOverload);
        }
    }
} 