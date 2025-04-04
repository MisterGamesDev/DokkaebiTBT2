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

        // Events
        public event Action<bool, string> OnCommandResult; // Success, message

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
        /// Handle a click on the ground
        /// </summary>
        public void HandleGroundClick(Vector2Int gridPosition)
{
    Debug.Log("[PlayerActionManager] HandleGroundClick triggered."); // Existing is fine too
    var selectedUnit = unitManager.GetSelectedUnit();

    // ADD THIS LOG: Check if a unit is selected
    Debug.Log($"[PlayerActionManager] Selected unit found: {(selectedUnit != null ? selectedUnit.GetUnitName() : "NULL")}");

    if (selectedUnit == null)
    {
        Debug.Log("[PlayerActionManager] No unit selected, stopping ground click handling.");
        return;
    }

    // ADD THIS LOG: Check if the turn system allows this unit to move
    bool canMove = turnSystem.CanUnitMove(selectedUnit);
    Debug.Log($"[PlayerActionManager] Checking CanUnitMove for {selectedUnit.GetUnitName()}: {canMove}");

    if (!canMove)
    {
        Debug.Log($"[PlayerActionManager] Unit {selectedUnit.GetUnitId()} cannot move at this time, stopping.");
        return;
    }

    // If it can move, proceed to execute command
    Debug.Log($"[PlayerActionManager] Attempting to execute move command for unit {selectedUnit.GetUnitId()} to {gridPosition}");
    ExecuteMoveCommand(selectedUnit.GetUnitId(), gridPosition);
}

        /// <summary>
        /// Generic method to execute a command
        /// </summary>
        private void ExecuteCommand(ICommand command)
        {
            if (useLocalValidationFirst)
            {
                // Validate the command locally first
                if (!command.Validate())
                {
                    // Command failed local validation
                    DebugLog($"Command validation failed: {command.CommandType}");
                    OnCommandResult?.Invoke(false, $"Invalid {command.CommandType} command");
                    return;
                }
            }

            // If local execution is enabled, execute locally for immediate feedback
            if (enableLocalExecution)
            {
                command.Execute();
            }

            // Send to server
            SendCommand(command.CommandType, command.Serialize());
        }

        /// <summary>
        /// Send a command to the server for authoritative validation and execution
        /// </summary>
        private void SendCommand(string commandName, Dictionary<string, object> commandData)
        {
            // Check if NetworkingManager is available
            if (networkManager == null)
            {
                DebugLog("Cannot send command: NetworkingManager not found");
                OnCommandResult?.Invoke(false, "Network error");
                return;
            }

            // Check if we're in a match
            if (string.IsNullOrEmpty(networkManager.GetCurrentMatchId()))
            {
                DebugLog("Cannot send command: No active match");
                
                // For local testing without server, simulate success
                if (enableLocalExecution)
                {
                    DebugLog($"Local execution of {commandName}: success (simulated)");
                    OnCommandResult?.Invoke(true, "Command executed locally");
                }
                else
                {
                    OnCommandResult?.Invoke(false, "No active match");
                }
                
                return;
            }

            // Add match ID to the command data
            commandData["matchId"] = networkManager.GetCurrentMatchId();

            // Send the command to the server
            networkManager.ExecuteCommand(
                commandName,
                commandData,
                result =>
                {
                    // Command succeeded on server
                    DebugLog($"Server execution of {commandName}: success");
                    OnCommandResult?.Invoke(true, "Command executed successfully");
                },
                error =>
                {
                    // Command failed on server
                    DebugLog($"Server execution of {commandName} failed: {error}");
                    OnCommandResult?.Invoke(false, error);
                }
            );
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Log debug messages
        /// </summary>
        private void DebugLog(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerActionManager] {message}");
            }
        }

        #endregion
    }
} 