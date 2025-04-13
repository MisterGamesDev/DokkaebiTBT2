/*
 * IMPLEMENTING PLAYERACTIONMANAGER - Adapted to match project's types and methods
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Core.Networking;
using Dokkaebi.Core.Networking.Commands;
using Dokkaebi.Grid;
using Dokkaebi.Units;
using Dokkaebi.Zones;
using Dokkaebi.Core.Data;
using Dokkaebi.Common;
using Dokkaebi.Interfaces;
using Dokkaebi.Utilities;
using Dokkaebi.Pathfinding;

namespace Dokkaebi.Core
{
    /// <summary>
    /// Manages player actions using the Command Pattern
    /// Validates commands locally for immediate feedback
    /// Sends commands to server via NetworkingManager for authoritative validation and execution
    /// </summary>
    public class PlayerActionManager : MonoBehaviour
    {
        // Singleton reference
        public static PlayerActionManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private NetworkingManager networkManager;
        [SerializeField] private DokkaebiTurnSystemCore turnSystem;
        [SerializeField] private UnitManager unitManager;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private ZoneManager zoneManager;

        [Header("Settings")]
        [SerializeField] private bool useLocalValidationFirst = true;
        [SerializeField] private bool enableLocalExecution = true;
        [SerializeField] private bool enableDebugLogs = true;

        // State tracking
        public enum ActionState
        {
            Idle,
            SelectingAbilityTarget
        }
        private ActionState currentState = ActionState.Idle;
        
        private AbilityData selectedAbility;
        private int selectedAbilityIndex;
        private DokkaebiUnit selectedUnit;

        // Events
        public event Action<bool, string> OnCommandResult; // Success, message
        public event Action<AbilityData> OnAbilityTargetingStarted;
        public event Action OnAbilityTargetingCancelled;

        private void Awake()
        {
            // Singleton setup
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Get component references
            if (turnSystem == null) turnSystem = FindObjectOfType<DokkaebiTurnSystemCore>();
            if (unitManager == null) unitManager = FindObjectOfType<UnitManager>();
            if (gridManager == null) gridManager = FindObjectOfType<GridManager>();

            if (turnSystem == null || unitManager == null || gridManager == null)
            {
                Debug.LogError("Required managers not found in scene!");
                return;
            }

            // Get references if needed
            if (networkManager == null) networkManager = FindObjectOfType<NetworkingManager>();
            if (zoneManager == null) zoneManager = FindObjectOfType<ZoneManager>();
        }

        #region Command Execution

        private void SetState(ActionState newState)
        {
            SmartLogger.Log($"[PAM.SetState] State transition: {currentState} -> {newState}", LogCategory.Ability);
            
            // Add stack trace for all state transitions
            if (newState == ActionState.Idle && currentState == ActionState.SelectingAbilityTarget)
            {
                SmartLogger.LogWarning($"[PAM.SetState] Transitioning from SelectingAbilityTarget to Idle. Stack trace:\n{System.Environment.StackTrace}", LogCategory.Ability);
            }
            
            currentState = newState;
        }

        /// <summary>
        /// Start ability targeting mode
        /// </summary>
        public void StartAbilityTargeting(DokkaebiUnit unit, int abilityIndex)
        {
            SmartLogger.Log($"[PAM.StartAbilityTargeting] ENTRY. Unit: {(unit ? unit.GetUnitName() : "NULL")}, AbilityIndex: {abilityIndex}, Current State: {currentState}", LogCategory.Ability);
            
            if (currentState == ActionState.SelectingAbilityTarget)
            {
                SmartLogger.LogWarning("[PAM.StartAbilityTargeting] Already in targeting state. Cancelling previous targeting.", LogCategory.Ability);
                CancelAbilityTargeting();
            }

            if (unit == null || !unit.IsAlive)
            {
                DebugLog("Cannot start ability targeting: Invalid unit");
                return;
            }

            var abilities = unit.GetAbilities();
            if (abilityIndex < 0 || abilityIndex >= abilities.Count)
            {
                DebugLog("Cannot start ability targeting: Invalid ability index");
                return;
            }

            selectedUnit = unit;
            selectedAbilityIndex = abilityIndex;
            selectedAbility = abilities[abilityIndex];
            SetState(ActionState.SelectingAbilityTarget);

            OnAbilityTargetingStarted?.Invoke(selectedAbility);
            Debug.Log($"[PAM.StartAbilityTargeting] State changed to: {currentState}, Selected Ability: {selectedAbility.displayName}");
        }

        /// <summary>
        /// Cancel ability targeting mode
        /// </summary>
        public void CancelAbilityTargeting()
        {
            SmartLogger.Log($"[PAM.CancelAbilityTargeting] ENTRY. Current state: {currentState}, Selected Ability: {(selectedAbility ? selectedAbility.displayName : "NULL")}", LogCategory.Ability);
            
            if (currentState == ActionState.SelectingAbilityTarget)
            {
                SetState(ActionState.Idle);
                selectedAbility = null;
                selectedUnit = null;

                // Add stack trace logging to identify what's triggering the cancellation
                SmartLogger.LogWarning($"[PAM.CancelAbilityTargeting] State forced back to IDLE. Stack trace:\n{System.Environment.StackTrace}", LogCategory.Ability);

                OnAbilityTargetingCancelled?.Invoke();
                Debug.Log($"[PAM.CancelAbilityTargeting] State changed to: {currentState}");
            }
            else
            {
                SmartLogger.LogWarning($"[PAM.CancelAbilityTargeting] Called while not in targeting state (Current: {currentState})", LogCategory.Ability);
            }
        }

        /// <summary>
        /// Handle a click on a unit
        /// </summary>
        public void HandleUnitClick(DokkaebiUnit targetUnit)
        {
            SmartLogger.Log($"[PAM.HandleUnitClick] ENTRY. Target: {(targetUnit ? targetUnit.GetUnitName() : "NULL")}, Current State: {currentState}, Selected Unit: {(selectedUnit ? selectedUnit.GetUnitName() : "NULL")}", LogCategory.Ability);

            if (currentState != ActionState.SelectingAbilityTarget)
            {
                SmartLogger.LogWarning($"[PAM.HandleUnitClick] Called while not in targeting state (Current: {currentState}). Ignoring.", LogCategory.Ability);
                return;
            }

            if (selectedAbility == null || selectedUnit == null)
            {
                SmartLogger.LogWarning("[PAM.HandleUnitClick] Null ability or caster, cancelling.", LogCategory.Ability);
                CancelAbilityTargeting();
                return;
            }

            bool isValid = IsValidAbilityTarget(targetUnit);
            SmartLogger.Log($"[PAM.HandleUnitClick] Target validation result: {isValid}", LogCategory.Ability);

            if (isValid)
            {
                var targetPos = targetUnit.GetGridPosition().ToVector2Int();
                SmartLogger.Log($"[PAM.HandleUnitClick] Target is Valid. Creating AbilityCommand with unitId: {selectedUnit.GetUnitId()}, abilityIndex: {selectedAbilityIndex}, targetPos: {targetPos}", LogCategory.Ability);
                
                // Store values before executing command as they'll be cleared
                var casterId = selectedUnit.GetUnitId();
                var abilityName = selectedAbility.displayName;
                var targetId = targetUnit.GetUnitId();
                
                ExecuteAbilityCommand(selectedUnit.GetUnitId(), selectedAbilityIndex, targetPos);
                SmartLogger.Log($"[PAM.HandleUnitClick] AbilityCommand created and executed. Caster: {casterId}, Ability: {abilityName}, Target: {targetId}", LogCategory.Ability);
                
                SetState(ActionState.Idle);
                selectedAbility = null;
                selectedUnit = null;
                SmartLogger.Log("[PAM.HandleUnitClick] State reset to Idle", LogCategory.Ability);
            }
            else
            {
                SmartLogger.LogWarning($"[PAM.HandleUnitClick] Target '{targetUnit?.GetUnitName()}' was invalid for ability '{selectedAbility?.displayName}'.", LogCategory.Ability);
            }
        }

        /// <summary>
        /// Handle a click on the ground
        /// </summary>
        public void HandleGroundClick(Vector2Int gridPosition)
        {
            Debug.Log($"[PAM.HandleGroundClick] ENTRY. TargetPos: {gridPosition}, Current State: {currentState}");

            if (currentState == ActionState.SelectingAbilityTarget)
            {
                Debug.Log($"[PAM.HandleGroundClick] In Targeting State. Caster: {(selectedUnit ? selectedUnit.GetUnitName() : "NULL")}, Ability: {(selectedAbility ? selectedAbility.displayName : "NULL")}");

                if (selectedAbility == null || selectedUnit == null)
                {
                    Debug.Log("[PAM.HandleGroundClick] Null ability or caster, cancelling targeting.");
                    CancelAbilityTargeting();
                    return;
                }

                // Check if ability can target ground
                if (!selectedAbility.targetsGround)
                {
                    Debug.Log($"[PAM.HandleGroundClick] Ability {selectedAbility.displayName} cannot target ground");
                    return;
                }

                // Check range
                var unitPos = selectedUnit.GetGridPosition();
                int distance = GridPosition.GetManhattanDistance(unitPos, GridPosition.FromVector2Int(gridPosition));
                Debug.Log($"[PAM.HandleGroundClick] Distance to target: {distance}, Ability range: {selectedAbility.range}");
                
                if (distance > selectedAbility.range)
                {
                    Debug.Log($"[PAM.HandleGroundClick] Target position out of range for ability {selectedAbility.displayName}");
                    return;
                }

                // Execute the ability command
                Debug.Log($"[PAM.HandleGroundClick] Executing ground-targeted ability {selectedAbility.displayName}");
                ExecuteAbilityCommand(selectedUnit.GetUnitId(), selectedAbilityIndex, gridPosition);
                SmartLogger.LogWarning($"[PAM.HandleGroundClick] About to reset state after ground ability. Stack trace:\n{System.Environment.StackTrace}", LogCategory.Ability);
                SetState(ActionState.Idle);
                selectedAbility = null;
                selectedUnit = null;
                Debug.Log("[PAM.HandleGroundClick] Completed ability execution via ground click. State reset to Idle.");
            }
            else
            {
                Debug.Log("[PAM.HandleGroundClick] In Idle State. Attempting to issue Move Command.");
                var currentSelectedUnit = unitManager.GetSelectedUnit();
                if (currentSelectedUnit == null)
                {
                    Debug.Log("[PAM.HandleGroundClick] No unit selected, cannot move.");
                    return;
                }

                bool canMove = turnSystem.CanUnitMove(currentSelectedUnit);
                Debug.Log($"[PAM.HandleGroundClick] Can unit {currentSelectedUnit.GetUnitName()} move? {canMove}");
                
                if (!canMove)
                {
                    Debug.Log($"[PAM.HandleGroundClick] Unit {currentSelectedUnit.GetUnitId()} cannot move at this time, stopping.");
                    return;
                }

                Debug.Log($"[PAM.HandleGroundClick] Issuing MoveCommand for Unit {currentSelectedUnit.GetUnitId()} to {gridPosition}");
                ExecuteMoveCommand(currentSelectedUnit.GetUnitId(), gridPosition);
            }
        }

        /// <summary>
        /// Execute a move command
        /// </summary>
        public void ExecuteMoveCommand(int unitId, Vector2Int targetPosition)
        {
            // Create the command
            var command = new MoveCommand(unitId, targetPosition);
            
            // Execute the command through our generic handler
            ExecuteCommand(command);
        }

        /// <summary>
        /// Execute an ability command
        /// </summary>
        public void ExecuteAbilityCommand(int unitId, int abilityIndex, Vector2Int targetPosition)
        {
            // Create the command
            var command = new AbilityCommand(unitId, abilityIndex, targetPosition);
            
            // Execute the command through our generic handler
            ExecuteCommand(command);
        }

        /// <summary>
        /// Check if a unit is a valid target for the currently selected ability
        /// </summary>
        private bool IsValidAbilityTarget(DokkaebiUnit targetUnit)
        {
            SmartLogger.Log($"[PAM.IsValidAbilityTarget] Starting validation for target: {targetUnit?.GetUnitName()}", LogCategory.Ability);

            if (selectedAbility == null || selectedUnit == null || targetUnit == null)
            {
                SmartLogger.LogWarning($"[PAM.IsValidAbilityTarget] FAILED: Null check failed. Ability: {selectedAbility != null}, Caster: {selectedUnit != null}, Target: {targetUnit != null}", LogCategory.Ability);
                return false;
            }

            // Declare variables once for all checks
            var targetPos = targetUnit.GetGridPosition();
            int distance = GridPosition.GetManhattanDistance(selectedUnit.GetGridPosition(), targetPos);
            bool isInRange = distance <= selectedAbility.range;

            // Check if the target position itself is out of range first
            if (!isInRange)
            {
                SmartLogger.LogWarning($"[PAM.IsValidAbilityTarget] FAILED: Target position {targetPos} is out of range ({distance} > {selectedAbility.range})", LogCategory.Ability);
                return false;
            }

            // If this is a ground-targeting ability, being in range is sufficient
            if (selectedAbility.targetsGround)
            {
                SmartLogger.Log($"[PAM.IsValidAbilityTarget] Ground-targeting ability check - Target is in range. PASSED.", LogCategory.Ability);
                return true;
            }

            // For non-ground targeting abilities, check targeting rules
            bool canTargetSelf = selectedAbility.targetsSelf && targetUnit.UnitId == selectedUnit.UnitId;
            bool canTargetAlly = selectedAbility.targetsAlly && targetUnit.TeamId == selectedUnit.TeamId && targetUnit.UnitId != selectedUnit.UnitId;
            bool canTargetEnemy = selectedAbility.targetsEnemy && targetUnit.TeamId != selectedUnit.TeamId;

            bool unitTypeIsValid = canTargetSelf || canTargetAlly || canTargetEnemy;

            SmartLogger.Log($"[PAM.IsValidAbilityTarget] Unit-targeting validation - InRange: {isInRange}, UnitTypeValid: {unitTypeIsValid} (Self:{canTargetSelf}, Ally:{canTargetAlly}, Enemy:{canTargetEnemy})", LogCategory.Ability);

            if (unitTypeIsValid)
            {
                SmartLogger.Log("[PAM.IsValidAbilityTarget] PASSED (Unit Allowed type and in range)", LogCategory.Ability);
                return true;
            }
            else
            {
                SmartLogger.LogWarning("[PAM.IsValidAbilityTarget] FAILED (Unit type not allowed)", LogCategory.Ability);
                return false;
            }
        }

        /// <summary>
        /// Execute an end turn command
        /// </summary>
        public void ExecuteEndTurnCommand()
        {
            // Create the command
            var command = new EndTurnCommand();
            
            // Execute the command through our generic handler
            ExecuteCommand(command);
        }

        /// <summary>
        /// Execute a tactical reposition command
        /// </summary>
        public void ExecuteRepositionCommand(int unitId, Vector2Int targetPosition)
        {
            // Create the command
            var command = new RepositionCommand(unitId, targetPosition);
            
            // Execute the command through our generic handler
            ExecuteCommand(command);
        }

        /// <summary>
        /// Execute a command through the command system
        /// </summary>
        private void ExecuteCommand(ICommand command)
        {
            if (command == null)
            {
                DebugLog("Cannot execute null command");
                OnCommandResult?.Invoke(false, "Invalid command");
                return;
            }

            DebugLog($"Executing command: {command.CommandType}");

            // Local validation if enabled
            if (useLocalValidationFirst)
            {
                bool isValid = command.Validate();
                if (!isValid)
                {
                    DebugLog($"Command validation failed: {command.CommandType}");
                    OnCommandResult?.Invoke(false, "Command validation failed");
                    return;
                }
            }

            // Local execution if enabled
            if (enableLocalExecution)
            {
                command.Execute();
                DebugLog($"Command executed locally: {command.CommandType}");
            }

            // Handle network execution
            if (networkManager == null)
            {
                // No network manager - assume local play and complete successfully
                DebugLog($"No network manager found. Completing command '{command.CommandType}' locally.");
                OnCommandResult?.Invoke(true, "Command executed successfully (local mode)");

                // Log state before potential reset
                SmartLogger.LogWarning($"[PAM ExecuteCommand LOCAL Success] Command '{command?.CommandType ?? "NULL"}' completed. Checking if state '{currentState}' requires reset via CancelAbilityTargeting.", LogCategory.Ability);

                if (currentState == ActionState.SelectingAbilityTarget)
                {
                    CancelAbilityTargeting();
                }
                return;
            }

            // Network execution
            DebugLog($"Sending command to network: {command.CommandType}");
            networkManager.ExecuteCommand(
                command.CommandType,
                command.Serialize(),
                result =>
                {
                    DebugLog($"Network execution succeeded: {command.CommandType}");
                    OnCommandResult?.Invoke(true, "Command executed successfully");

                    // Log state before potential reset
                    SmartLogger.LogWarning($"[PAM ExecuteCommand NETWORK Success] Command '{command?.CommandType ?? "NULL"}' completed. Checking if state '{currentState}' requires reset via CancelAbilityTargeting.", LogCategory.Ability);

                    if (currentState == ActionState.SelectingAbilityTarget)
                    {
                        CancelAbilityTargeting();
                    }
                },
                error =>
                {
                    DebugLog($"Network execution failed: {command.CommandType} - {error}");
                    OnCommandResult?.Invoke(false, error);
                }
            );
        }

        /// <summary>
        /// Get the currently selected unit
        /// </summary>
        public DokkaebiUnit GetSelectedUnit()
        {
            return selectedUnit;
        }

        /// <summary>
        /// Set the currently selected unit
        /// </summary>
        public void SetSelectedUnit(DokkaebiUnit unit)
        {
            selectedUnit = unit;
            DebugLog($"Selected unit: {(unit != null ? unit.GetUnitName() : "None")}");
        }

        /// <summary>
        /// Clear the currently selected unit
        /// </summary>
        public void ClearSelectedUnit()
        {
            selectedUnit = null;
            DebugLog("Cleared selected unit");
        }

        private void DebugLog(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerActionManager] {message}");
            }
        }

        public ActionState GetCurrentActionState() => currentState;

        #endregion
    }
} 
