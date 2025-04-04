using UnityEngine;
using Dokkaebi.Core;
using Dokkaebi.Core.Data;
using Dokkaebi.Utilities;
using Dokkaebi.Units;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Core
{
    public class GameController : MonoBehaviour {
        // Game state
        public enum GameState {
            Playing,
            GameOver
        }
        
        private GameState currentState = GameState.Playing;
        private bool player1Wins = false;
        
        // Optional references
        [SerializeField] private UnitManager unitManager;
        [SerializeField] private DokkaebiTurnSystemCore turnSystem;
        
        // Events
        public System.Action<bool> OnGameOver; // bool parameter is true if player 1 wins
        
        void Start()
{
    // Find managers
    UnitManager unitManager = FindAnyObjectByType<UnitManager>();
    DataManager dataManager = FindAnyObjectByType<DataManager>();
    
    // Get spawn data and create units
    if (dataManager && unitManager)
    {
        UnitSpawnData spawnData = dataManager.GetUnitSpawnData();
        if (spawnData)
        {
            // Spawn player units
            foreach (var unitInfo in spawnData.playerUnitSpawns)
            {
                if (unitInfo.unitDefinition != null)
                {
                    var gridPosition = GridPosition.FromVector2Int(unitInfo.spawnPosition);
                    unitManager.SpawnUnit(unitInfo.unitDefinition, gridPosition, true);
                    Debug.Log($"Spawning player: {unitInfo.unitDefinition.displayName} at {gridPosition}");
                }
            }
            
            // Spawn enemy units
            foreach (var unitInfo in spawnData.enemyUnitSpawns)
            {
                if (unitInfo.unitDefinition != null)
                {
                    var gridPosition = GridPosition.FromVector2Int(unitInfo.spawnPosition);
                    unitManager.SpawnUnit(unitInfo.unitDefinition, gridPosition, false);
                    Debug.Log($"Spawning enemy: {unitInfo.unitDefinition.displayName} at {gridPosition}");
                }
            }
        }
        else
        {
            Debug.LogError("No UnitSpawnData assigned in DataManager!");
        }
    }
}
        
        void OnDestroy() {
            // Unsubscribe from events
            if (unitManager != null) {
                unitManager.OnUnitDefeated -= HandleUnitDefeated;
            }
            
            if (turnSystem != null) {
                turnSystem.OnTurnResolutionEnd -= CheckWinLossConditions;
            }
        }
        
        void BeginGame() {
        // Reference to the turn system
        if (turnSystem != null) {

            // --- ADD THIS SECTION ---
            // Make sure unitManager reference is valid
            if (unitManager != null)
            {
                Debug.Log("[GameController.BeginGame] Calling UnitManager.SpawnUnitsFromConfiguration..."); // Add this log too!
                unitManager.SpawnUnitsFromConfiguration();
            }
            else
            {
                 Debug.LogError("[GameController.BeginGame] Cannot spawn units: UnitManager reference is null!");
            }
            // --- END OF ADDED SECTION ---


            // Register all units (This will now find the newly spawned units)
            var units = FindObjectsOfType<DokkaebiUnit>();
            foreach (var unit in units) {
                turnSystem.RegisterUnit(unit);
            }

            // Log that the game has started
            SmartLogger.Log("Game has begun!", LogCategory.TurnSystem);
        }
        // Your manually added "Preparing..." log might be here or earlier.
    }
        
        /// <summary>
        /// Handle when a unit is defeated
        /// </summary>
        void HandleUnitDefeated(DokkaebiUnit unit) {
            SmartLogger.Log($"Unit defeated: {unit.GetUnitName()} (Player: {unit.IsPlayer()})", LogCategory.Game);
            
            // Check win/loss conditions whenever a unit is defeated
            CheckWinLossConditions();
        }
        
        /// <summary>
        /// Check if either player has won or lost
        /// </summary>
        public void CheckWinLossConditions() {
            // Skip if game is already over
            if (currentState == GameState.GameOver) return;
            
            if (unitManager == null) {
                SmartLogger.LogWarning("Cannot check win/loss conditions: UnitManager not found", LogCategory.Game);
                return;
            }
            
            // Get lists of alive units for each player
            var player1Units = unitManager.GetAliveUnitsByPlayer(true);
            var player2Units = unitManager.GetAliveUnitsByPlayer(false);
            
            SmartLogger.Log($"Checking win/loss conditions - Player 1 units: {player1Units.Count}, Player 2 units: {player2Units.Count}", LogCategory.Game);
            
            // Check win conditions
            if (player2Units.Count == 0 && player1Units.Count > 0) {
                // Player 1 wins (all enemy units defeated)
                HandleGameOver(true);
            } 
            else if (player1Units.Count == 0 && player2Units.Count > 0) {
                // Player 2 wins (all player units defeated)
                HandleGameOver(false);
            }
        }
        
        /// <summary>
        /// Handle the game over state
        /// </summary>
        private void HandleGameOver(bool player1Wins) {
            // Set game state
            currentState = GameState.GameOver;
            this.player1Wins = player1Wins;
            
            // Log the result
            string winnerText = player1Wins ? "Player 1" : "Player 2";
            SmartLogger.Log($"GAME OVER - {winnerText} Wins!", LogCategory.Game);
            
            // Pause the game
            Time.timeScale = 0;
            
            // Trigger event
            OnGameOver?.Invoke(player1Wins);
            
            // Display win message (simple implementation for now)
            Debug.Log($"<color=yellow><b>GAME OVER - {winnerText} WINS!</b></color>");
        }
        
        /// <summary>
        /// Return the current state of the game
        /// </summary>
        public GameState GetGameState() {
            return currentState;
        }
        
        /// <summary>
        /// Check if the game is over
        /// </summary>
        public bool IsGameOver() {
            return currentState == GameState.GameOver;
        }
        
        /// <summary>
        /// Get the winner of the game
        /// </summary>
        public int GetWinner() {
            if (currentState != GameState.GameOver) return 0;
            return player1Wins ? 1 : 2;
        }
        
        /// <summary>
        /// Reset the game
        /// </summary>
        public void ResetGame() {
            // Reset game state
            currentState = GameState.Playing;
            player1Wins = false;
            
            // Reset time scale
            Time.timeScale = 1;
            
            // Add additional reset logic as needed
        }
    }
}