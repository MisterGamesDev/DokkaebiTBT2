using UnityEngine;
using Dokkaebi.Core;
using Dokkaebi.Core.Data;
using Dokkaebi.Utilities;
using Dokkaebi.Units;
using Dokkaebi.Interfaces;
using Dokkaebi.Common;

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
                            SmartLogger.Log($"Spawning player: {unitInfo.unitDefinition.displayName} at {gridPosition}", LogCategory.Game, this);
                        }
                    }
                    
                    // Spawn enemy units
                    foreach (var unitInfo in spawnData.enemyUnitSpawns)
                    {
                        if (unitInfo.unitDefinition != null)
                        {
                            var gridPosition = GridPosition.FromVector2Int(unitInfo.spawnPosition);
                            unitManager.SpawnUnit(unitInfo.unitDefinition, gridPosition, false);
                            SmartLogger.Log($"Spawning enemy: {unitInfo.unitDefinition.displayName} at {gridPosition}", LogCategory.Game, this);
                        }
                    }
                }
                else
                {
                    SmartLogger.LogError("No UnitSpawnData assigned in DataManager!", LogCategory.Game, this);
                }

                // Subscribe to turn system events
                if (turnSystem != null)
                {
                    turnSystem.OnTurnResolutionEnd += CheckWinLossConditions;
                }

                // Subscribe to unit manager events
                if (unitManager != null)
                {
                    unitManager.OnUnitDefeated += HandleUnitDefeated;
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
                // Make sure unitManager reference is valid
                if (unitManager != null)
                {
                    SmartLogger.Log("[GameController.BeginGame] Calling UnitManager.SpawnUnitsFromConfiguration...", LogCategory.Game, this);
                    unitManager.SpawnUnitsFromConfiguration();
                }
                else
                {
                    SmartLogger.LogError("[GameController.BeginGame] Cannot spawn units: UnitManager reference is null!", LogCategory.Game, this);
                }

                // Register all units (This will now find the newly spawned units)
                var units = FindObjectsOfType<DokkaebiUnit>();
                SmartLogger.Log($"[GameController.BeginGame] Found {units.Length} units to register", LogCategory.Game, this);
                foreach (var unit in units) {
                    SmartLogger.Log($"[GameController.BeginGame] Registering unit {unit.GetUnitName()} (ID: {unit.UnitId})", LogCategory.Game, this);
                    turnSystem.RegisterUnit(unit);
                }

                // Log that the game has begun
                SmartLogger.Log("Game has begun!", LogCategory.TurnSystem, this);
            }
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
            
            // Check if each player has remaining units
            bool player1HasUnits = unitManager.HasRemainingUnits(true);
            bool player2HasUnits = unitManager.HasRemainingUnits(false);
            
            SmartLogger.Log($"Checking win/loss conditions - Player 1 has units: {player1HasUnits}, Player 2 has units: {player2HasUnits}", LogCategory.Game);
            
            // Check win conditions
            if (!player2HasUnits && player1HasUnits) {
                // Player 1 wins (all enemy units defeated)
                HandleGameOver(true);
            } 
            else if (!player1HasUnits && player2HasUnits) {
                // Player 2 wins (all player units defeated)
                HandleGameOver(false);
            }
            else if (!player1HasUnits && !player2HasUnits) {
                // Draw condition - both players have no units left
                SmartLogger.Log("GAME OVER - DRAW! Both players have no units remaining", LogCategory.Game);
                HandleGameOver(false); // Default to player 2 win for draw condition
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
            
            // Display win message with color
            SmartLogger.Log($"<color=yellow><b>GAME OVER - {winnerText} WINS!</b></color>", LogCategory.Game);
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

        private void SpawnPlayerUnit(UnitSpawnConfig unitInfo)
        {
            var gridPosition = unitInfo.spawnPosition;
            SmartLogger.Log($"Spawning player: {unitInfo.unitDefinition.displayName} at {gridPosition}", LogCategory.Game, this);
            UnitManager.Instance.SpawnUnit(unitInfo.unitDefinition, gridPosition, true);
        }

        private void SpawnEnemyUnit(UnitSpawnConfig unitInfo)
        {
            var gridPosition = unitInfo.spawnPosition;
            SmartLogger.Log($"Spawning enemy: {unitInfo.unitDefinition.displayName} at {gridPosition}", LogCategory.Game, this);
            UnitManager.Instance.SpawnUnit(unitInfo.unitDefinition, gridPosition, false);
        }

        private void SpawnUnits()
        {
            var spawnData = DataManager.Instance.GetUnitSpawnData();
            if (spawnData == null)
            {
                SmartLogger.LogError("No UnitSpawnData assigned in DataManager!", LogCategory.Game, this);
                return;
            }

            // Spawn all configured units
            foreach (var unitInfo in spawnData.playerUnitSpawns)
            {
                SpawnPlayerUnit(unitInfo);
            }

            foreach (var unitInfo in spawnData.enemyUnitSpawns)
            {
                SpawnEnemyUnit(unitInfo);
            }
        }

        public void InitializeGame()
        {
            SmartLogger.Log("[GameController.InitializeGame] Calling UnitManager.SpawnUnitsFromConfiguration...", LogCategory.Game, this);

            if (UnitManager.Instance == null)
            {
                SmartLogger.LogError("[GameController.InitializeGame] Cannot spawn units: UnitManager reference is null!", LogCategory.Game, this);
                return;
            }

            SpawnUnits();
            var units = UnitManager.Instance.GetAliveUnits();
            SmartLogger.Log($"[GameController.InitializeGame] Found {units.Count} units to register", LogCategory.Game, this);

            foreach (var unit in units)
            {
                SmartLogger.Log($"[GameController.InitializeGame] Registering unit {unit.GetUnitName()} (ID: {unit.UnitId})", LogCategory.Game, this);
                turnSystem.RegisterUnit(unit);
            }

            // Start the game by transitioning to the first phase
            turnSystem.NextPhase();
        }
    }
}
