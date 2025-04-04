using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Units;
using Dokkaebi.Utilities;
using Dokkaebi.Core.TurnStates;
using Dokkaebi.Common;

namespace Dokkaebi.Core
{
    /// <summary>
    /// Tracks unit states and phase actions for the turn system.
    /// </summary>
    public class UnitStateManager : MonoBehaviour, DokkaebiUpdateManager.IUpdateObserver
    {
        [Header("Player Units")]
        [SerializeField] private int requiredPlayer1Moves = 4;
        [SerializeField] private int requiredPlayer2Moves = 4;
        [SerializeField] private int maxAurasPerPhase = 2;
        
        // State tracking
        private int player1UnitsMoved = 0;
        private int player2UnitsMoved = 0;
        private int player1AurasActivated = 0;
        private int player2AurasActivated = 0;
        
        // Unit lists
        private List<DokkaebiUnit> player1Units = new List<DokkaebiUnit>();
        private List<DokkaebiUnit> player2Units = new List<DokkaebiUnit>();
        
        private DokkaebiTurnSystemCore turnSystem;
        
        private void Awake()
        {
            turnSystem = GetComponent<DokkaebiTurnSystemCore>();
        }
        
        private void Start()
        {
            // Register with update manager
            DokkaebiUpdateManager.Instance.RegisterUpdateObserver(this);
            
            // Subscribe to turn system events if available
            if (turnSystem != null)
            {
                turnSystem.OnPhaseChanged += HandlePhaseChanged;
            }
            
            // Find all units in the scene
            RefreshUnitLists();
        }
        
        private void OnDestroy()
        {
            // Unregister from update manager
            DokkaebiUpdateManager.Instance.UnregisterUpdateObserver(this);
            
            // Unsubscribe from turn system events
            if (turnSystem != null)
            {
                turnSystem.OnPhaseChanged -= HandlePhaseChanged;
            }
        }
        
        public void CustomUpdate(float deltaTime)
        {
            // Any custom update logic would go here
            // This replaces the standard Unity Update method
        }
        
        /// <summary>
        /// Checks if a player has reached their maximum auras for the current phase
        /// </summary>
        public bool HasReachedMaxAuras(bool isPlayer)
        {
            int currentAuras = isPlayer ? player1AurasActivated : player2AurasActivated;
            return currentAuras >= maxAurasPerPhase;
        }
        
        /// <summary>
        /// Checks if a player has reached their maximum movement for the turn
        /// </summary>
        public bool HasReachedMaxMoves(bool isPlayer)
        {
            int currentMoves = isPlayer ? player1UnitsMoved : player2UnitsMoved;
            int requiredMoves = isPlayer ? requiredPlayer1Moves : requiredPlayer2Moves;
            return currentMoves >= requiredMoves;
        }
        
        /// <summary>
        /// Reset aura counters for a new aura phase
        /// </summary>
        public void ResetAuraCounters()
        {
            using (new PerformanceScope("ResetAuraCounters"))
            {
                player1AurasActivated = 0;
                player2AurasActivated = 0;
                SmartLogger.Log("Aura counters reset", LogCategory.Ability);
            }
        }
        
        /// <summary>
        /// Find all units in the scene and categorize them by player
        /// </summary>
        public void RefreshUnitLists()
        {
            using (new PerformanceScope("RefreshUnitLists"))
            {
                player1Units.Clear();
                player2Units.Clear();
                
                DokkaebiUnit[] allUnits = FindObjectsByType<DokkaebiUnit>(FindObjectsSortMode.None);
                foreach (DokkaebiUnit unit in allUnits)
                {
                    if (unit.IsPlayer())
                    {
                        player1Units.Add(unit);
                    }
                    else
                    {
                        player2Units.Add(unit);
                    }
                }
                
                SmartLogger.Log($"UnitStateManager found {player1Units.Count} player units and {player2Units.Count} AI units", LogCategory.TurnSystem);
            }
        }
        
        /// <summary>
        /// Handle phase change events to reset counters
        /// </summary>
        private void HandlePhaseChanged(TurnPhase newPhase)
        {
            // Reset counters at the start of a new turn
            if (newPhase == TurnPhase.MovementPhase)
            {
                ResetCounters();
            }
            
            // Reset aura counters at the start of each aura phase
            if (newPhase == TurnPhase.AuraPhase1A || newPhase == TurnPhase.AuraPhase2A)
            {
                player1AurasActivated = 0;
                player2AurasActivated = 0;
            }
        }
        
        /// <summary>
        /// Reset all unit action counters
        /// </summary>
        public void ResetCounters()
        {
            player1UnitsMoved = 0;
            player2UnitsMoved = 0;
            player1AurasActivated = 0;
            player2AurasActivated = 0;
            
            SmartLogger.Log("Unit action counters reset", LogCategory.TurnSystem);
        }
        
        /// <summary>
        /// Register a unit's movement
        /// </summary>
        public void RegisterUnitMoved(bool isPlayer)
        {
            if (isPlayer)
            {
                // Only increment if below the limit
                if (player1UnitsMoved < requiredPlayer1Moves)
                    player1UnitsMoved++;
            }
            else
            {
                // Only increment if below the limit
                if (player2UnitsMoved < requiredPlayer2Moves)
                    player2UnitsMoved++;
            }
            
            SmartLogger.Log($"{(isPlayer ? "Player" : "AI")} unit moved. Total: {(isPlayer ? player1UnitsMoved : player2UnitsMoved)}/{(isPlayer ? requiredPlayer1Moves : requiredPlayer2Moves)}", LogCategory.Movement);
        }
        
        /// <summary>
        /// Register an aura activation
        /// </summary>
        public void RegisterAuraActivated(bool isPlayer)
        {
            if (isPlayer)
            {
                player1AurasActivated++;
            }
            else
            {
                player2AurasActivated++;
            }
            
            SmartLogger.Log($"{(isPlayer ? "Player" : "AI")} used aura. Total: {(isPlayer ? player1AurasActivated : player2AurasActivated)}/{maxAurasPerPhase}", LogCategory.Ability);
        }
        
        /// <summary>
        /// Get current player auras activated based on turn phase
        /// </summary>
        public int GetCurrentPlayerAurasActivated()
        {
            if (turnSystem == null)
                return 0;
                
            bool isPlayerTurn = turnSystem.GetActivePlayer() == 1;
            return isPlayerTurn ? player1AurasActivated : player2AurasActivated;
        }
        
        // Accessor methods
        public int GetPlayer1UnitsMoved() => player1UnitsMoved;
        public int GetPlayer2UnitsMoved() => player2UnitsMoved;
        public int GetRequiredPlayer1Moves() => requiredPlayer1Moves;
        public int GetRequiredPlayer2Moves() => requiredPlayer2Moves;
        public int GetPlayer1AurasActivated() => player1AurasActivated;
        public int GetPlayer2AurasActivated() => player2AurasActivated;
        public int GetMaxAurasPerPhase() => maxAurasPerPhase;
        public List<DokkaebiUnit> GetPlayer1Units() => player1Units;
        public List<DokkaebiUnit> GetPlayer2Units() => player2Units;
        
        /// <summary>
        /// Get total required moves for both players combined
        /// </summary>
        public int GetTotalRequiredMoves()
        {
            return requiredPlayer1Moves + requiredPlayer2Moves;
        }
    }
}