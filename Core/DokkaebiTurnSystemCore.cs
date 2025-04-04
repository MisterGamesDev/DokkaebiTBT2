using System;
using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Grid;
using Dokkaebi.Units;
using Dokkaebi.Utilities;
using Dokkaebi.Core.TurnStates;
using Dokkaebi.Common;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Core
{
    /// <summary>
    /// Core implementation of the Dokkaebi Turn Flow System (DTFS).
    /// Handles turn progression, movement, and Aura activation.
    /// </summary>
    public class DokkaebiTurnSystemCore : MonoBehaviour, DokkaebiUpdateManager.IUpdateObserver, ITurnSystem
    {
        // Singleton pattern
        public static DokkaebiTurnSystemCore Instance { get; private set; }
        
        // Core components
        [SerializeField] private UnitStateManager unitStateManager;
        
        // Turn state management
        private TurnStateContext turnStateContext;
        
        // Turn settings
        [Header("Turn Settings")]
        [SerializeField] private float phaseTransitionDelay = 0.3f;
        
        // Events
        public event Action<TurnPhase> OnPhaseChanged;
        public event Action<int> OnTurnChanged;
        public event Action<int> OnActivePlayerChanged;
        public event Action OnMovementPhaseStart;
        public event Action OnMovementPhaseEnd;
        public event Action OnTurnResolutionEnd;
        
        // Unit management
        private Dictionary<DokkaebiUnit, GridPosition> pendingMoves = new Dictionary<DokkaebiUnit, GridPosition>();
        private HashSet<DokkaebiUnit> unitsActedThisPhase = new HashSet<DokkaebiUnit>();
        private List<DokkaebiUnit> registeredUnits = new List<DokkaebiUnit>();
        
        // Movement tracking
        private bool isExecutingMoves = false;
        private int totalMovesMade = 0;
        private int requiredMoves = 4; // Default value
        
        // Debug flags
        [Header("Debug")]
        [SerializeField] private bool debugLogTurns = false;
        
        // Properties required by ITurnSystem
        public int CurrentTurn => turnStateContext != null ? turnStateContext.GetCurrentTurn() : 1;
        public TurnPhase CurrentPhase => turnStateContext != null ? turnStateContext.GetCurrentPhase() : TurnPhase.Opening;
        public int ActivePlayerId => turnStateContext != null ? turnStateContext.GetActivePlayer() : 0;

        public int GetActivePlayer() => ActivePlayerId;
        public TurnPhase GetCurrentPhase() => CurrentPhase;
        public int GetCurrentTurn() => CurrentTurn;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize turn state context
            turnStateContext = new TurnStateContext();
            turnStateContext.OnPhaseChanged += HandlePhaseChanged;
            turnStateContext.OnTurnChanged += HandleTurnChanged;
            turnStateContext.OnMovementPhaseStart += () => OnMovementPhaseStart?.Invoke();
            turnStateContext.OnMovementPhaseEnd += () => OnMovementPhaseEnd?.Invoke();
            turnStateContext.OnTurnResolutionEnd += () => OnTurnResolutionEnd?.Invoke();

            // Register with update manager
            DokkaebiUpdateManager.Instance.RegisterUpdateObserver(this);
        }

        private void OnDestroy()
        {
            if (turnStateContext != null)
            {
                turnStateContext.OnPhaseChanged -= HandlePhaseChanged;
                turnStateContext.OnTurnChanged -= HandleTurnChanged;
            }

            if (DokkaebiUpdateManager.Instance != null)
            {
                DokkaebiUpdateManager.Instance.UnregisterUpdateObserver(this);
            }
        }

        private void HandlePhaseChanged(TurnPhase newPhase)
        {
            OnPhaseChanged?.Invoke(newPhase);
            
            if (debugLogTurns)
            {
                SmartLogger.Log($"Turn phase changed to {newPhase}", LogCategory.TurnSystem);
            }
        }

        private void HandleTurnChanged(int newTurn)
        {
            OnTurnChanged?.Invoke(newTurn);
            OnActivePlayerChanged?.Invoke(turnStateContext.GetActivePlayer());
            
            if (debugLogTurns)
            {
                SmartLogger.Log($"Turn changed to {newTurn}", LogCategory.TurnSystem);
            }
        }

        public void CustomUpdate(float deltaTime)
        {
            if (turnStateContext != null)
            {
                turnStateContext.Update(deltaTime);
            }
        }

        // Unit movement methods
        private bool CanUnitMove(DokkaebiUnit unit)
        {
            if (turnStateContext == null || !turnStateContext.AllowsMovement())
                return false;

            return !unitsActedThisPhase.Contains(unit);
        }

        private bool CanUnitUseAura(DokkaebiUnit unit)
        {
            if (turnStateContext == null)
                return false;

            return turnStateContext.AllowsAuraActivation(unit.IsPlayer());
        }

        private void QueueMove(DokkaebiUnit unit, GridPosition targetPosition)
        {
            if (!CanUnitMove(unit))
            {
                Debug.LogWarning($"Unit {unit.UnitId} cannot move in the current phase");
                return;
            }

            pendingMoves[unit] = targetPosition;
            unitsActedThisPhase.Add(unit);
        }

        public void NextPhase()
        {
            if (turnStateContext != null)
            {
                turnStateContext.TransitionToNextState();
            }
            else
            {
                Debug.LogError("TurnStateContext is null in DokkaebiTurnSystemCore.NextPhase");
            }
        }

        public void NextTurn()
        {
            if (turnStateContext != null)
            {
                turnStateContext.IncrementTurn();
            }
            else
            {
                Debug.LogError("TurnStateContext is null in DokkaebiTurnSystemCore.NextTurn");
            }
        }

        /// <summary>
        /// Register a unit with the turn system
        /// </summary>
        public void RegisterUnit(DokkaebiUnit unit)
        {
            if (unit != null && !registeredUnits.Contains(unit))
            {
                registeredUnits.Add(unit);
                SmartLogger.Log($"Unit {unit.GetUnitName()} registered with turn system", LogCategory.TurnSystem);
            }
        }
        
        /// <summary>
        /// Unregister a unit from the turn system
        /// </summary>
        public void UnregisterUnit(DokkaebiUnit unit)
        {
            if (unit != null && registeredUnits.Contains(unit))
            {
                registeredUnits.Remove(unit);
                
                // Also remove from pending actions if present
                if (pendingMoves.ContainsKey(unit))
                {
                    pendingMoves.Remove(unit);
                }
                
                if (unitsActedThisPhase.Contains(unit))
                {
                    unitsActedThisPhase.Remove(unit);
                }
            }
        }
        
        /// <summary>
        /// Check if all required movement has been completed
        /// </summary>
        private void CheckMovementCompletion()
        {
            TurnPhase currentPhase = GetCurrentPhase();
            if (currentPhase != TurnPhase.MovementPhase || isExecutingMoves)
            {
                return;
            }
            
            bool allMovesComplete = false;
            bool bothPlayersComplete = false;
            
            // Check if all required moves have been completed
            if (totalMovesMade >= requiredMoves)
            {
                allMovesComplete = true;
            }
            
            // Check if both players have reached their individual move limits
            if (unitStateManager != null)
            {
                bool p1MaxReached = unitStateManager.HasReachedMaxMoves(true);
                bool p2MaxReached = unitStateManager.HasReachedMaxMoves(false);
                bothPlayersComplete = p1MaxReached && p2MaxReached;
            }
            
            // If either condition is met, trigger the phase advance
            if ((allMovesComplete || bothPlayersComplete) && !turnStateContext.IsTransitionLocked)
            {
                SmartLogger.Log("All required moves completed!", LogCategory.Movement);
                
                // Execute all pending moves and advance phase
                ExecuteAllPendingMoves();
                turnStateContext.TransitionToNextState();
            }
        }
        
        /// <summary>
        /// Queue an Aura ability usage with strict limit enforcement
        /// </summary>
        public bool QueueAura(DokkaebiUnit unit)
        {
            using (new PerformanceScope("QueueAura"))
            {
                LogTurnSystemState(); // Log turn state for debugging
                
                if (unit == null)
                {
                    SmartLogger.LogError("Cannot queue Aura for null unit!", LogCategory.Ability);
                    return false;
                }
                
                // Check if we're in an Aura phase
                TurnPhase currentPhase = GetCurrentPhase();
                bool isAuraPhase = currentPhase == TurnPhase.AuraPhase1A || 
                                  currentPhase == TurnPhase.AuraPhase1B || 
                                  currentPhase == TurnPhase.AuraPhase2A || 
                                  currentPhase == TurnPhase.AuraPhase2B;
                if (!isAuraPhase)
                {
                    SmartLogger.LogWarning($"Cannot use Aura for {unit.GetUnitName()} - not in Aura phase", LogCategory.Ability);
                    return false;
                }
                
                // Check if it's the right player's turn
                bool isUnitPlayer1 = unit.IsPlayer();
                if (!turnStateContext.AllowsAuraActivation(isUnitPlayer1))
                {
                    SmartLogger.LogWarning($"Not {unit.GetUnitName()}'s turn to use Aura. Active player: {GetActivePlayer()}", LogCategory.Ability);
                    return false;
                }
                
                // Check if unit already used an ability this phase
                if (HasUnitActedThisPhase(unit))
                {
                    SmartLogger.LogWarning($"{unit.GetUnitName()} has already used an ability this phase", LogCategory.Ability);
                    return false;
                }
                
                // Check if player has reached their aura limit for this phase
                if (unitStateManager != null)
                {
                    int maxAuras = unitStateManager.GetMaxAurasPerPhase();
                    bool isPlayer1 = unit.IsPlayer();
                    int currentAuras = isPlayer1 ? unitStateManager.GetPlayer1AurasActivated() : unitStateManager.GetPlayer2AurasActivated();
                    
                    if (currentAuras >= maxAuras)
                    {
                        SmartLogger.LogWarning($"{unit.GetUnitName()} has reached the maximum aura activations for this phase", LogCategory.Ability);
                        return false;
                    }
                }
                
                // Mark unit as having acted this phase
                unitsActedThisPhase.Add(unit);
                
                // Update aura activation count
                if (unitStateManager != null)
                {
                    unitStateManager.RegisterAuraActivated(unit.IsPlayer());
                }
                
                return true;
            }
        }
        
        /// <summary>
        /// Check if we should advance to the next phase based on aura usage
        /// </summary>
        private void CheckAuraCompletion()
        {
            TurnPhase currentPhase = GetCurrentPhase();
            bool isAuraPhase = currentPhase == TurnPhase.AuraPhase1A || 
                              currentPhase == TurnPhase.AuraPhase1B || 
                              currentPhase == TurnPhase.AuraPhase2A || 
                              currentPhase == TurnPhase.AuraPhase2B;
            if (!isAuraPhase || turnStateContext.IsTransitionLocked)
            {
                return;
            }
            
            if (unitStateManager != null)
            {
                int activePlayer = GetActivePlayer();
                int maxAuras = unitStateManager.GetMaxAurasPerPhase();
                
                int currentAuras = 0;
                if (activePlayer == 1)
                {
                    currentAuras = unitStateManager.GetPlayer1AurasActivated();
                }
                else if (activePlayer == 2)
                {
                    currentAuras = unitStateManager.GetPlayer2AurasActivated();
                }
                
                // Auto-advance if max auras used
                if (currentAuras >= maxAuras)
                {
                    SmartLogger.Log($"Player {activePlayer} used all {currentAuras}/{maxAuras} auras", LogCategory.Ability);
                    turnStateContext.TransitionToNextState();
                }
                else
                {
                    SmartLogger.Log($"Player {activePlayer} used {currentAuras}/{maxAuras} auras - not advancing yet", LogCategory.Ability);
                }
            }
        }
        
        /// <summary>
        /// Check if a unit has already acted in the current phase
        /// </summary>
        public bool HasUnitActedThisPhase(DokkaebiUnit unit)
        {
            return unit != null && unitsActedThisPhase.Contains(unit);
        }
        
        /// <summary>
        /// Log the current turn system state
        /// </summary>
        public void LogTurnSystemState()
        {
            TurnPhase phase = GetCurrentPhase();
            int turn = GetCurrentTurn();
            int activePlayer = GetActivePlayer();
            
            SmartLogger.Log($"TURN SYSTEM STATE: Turn {turn}, Phase {phase}, Active Player: {activePlayer}", LogCategory.TurnSystem);
            
            if (unitStateManager != null)
            {
                int p1Auras = unitStateManager.GetPlayer1AurasActivated();
                int p2Auras = unitStateManager.GetPlayer2AurasActivated();
                int maxAuras = unitStateManager.GetMaxAurasPerPhase();
                SmartLogger.Log($"P1 Auras: {p1Auras}/{maxAuras}, P2 Auras: {p2Auras}/{maxAuras}", LogCategory.TurnSystem);
                
                int p1Moves = unitStateManager.GetPlayer1UnitsMoved();
                int p2Moves = unitStateManager.GetPlayer2UnitsMoved();
                int p1Required = unitStateManager.GetRequiredPlayer1Moves();
                int p2Required = unitStateManager.GetRequiredPlayer2Moves();
                SmartLogger.Log($"P1 Moves: {p1Moves}/{p1Required}, P2 Moves: {p2Moves}/{p2Required}", LogCategory.TurnSystem);
            }
        }
        
        /// <summary>
        /// Execute all pending move actions
        /// </summary>
        private void ExecuteAllPendingMoves()
        {
            using (new PerformanceScope("ExecuteAllPendingMoves"))
            {
                if (pendingMoves.Count == 0)
                {
                    SmartLogger.Log("No pending moves to execute", LogCategory.Movement);
                    return;
                }
                
                if (isExecutingMoves)
                {
                    SmartLogger.Log("Already executing moves", LogCategory.Movement);
                    return;
                }
                    
                isExecutingMoves = true;
                
                // Get a copy of all pending moves to avoid collection modification issues
                var movesToExecute = new Dictionary<DokkaebiUnit, GridPosition>(pendingMoves);
                pendingMoves.Clear();
                
                // Log the moves being executed
                foreach (var kvp in movesToExecute)
                {
                    DokkaebiUnit unit = kvp.Key;
                    GridPosition targetPosition = kvp.Value;
                    
                    if (unit == null) continue;
                    
                    // Get current position for logging
                    GridPosition currentPos = unit.GetGridPosition();
                    SmartLogger.Log($"Processing move for {unit.GetUnitName()} from {currentPos} to {targetPosition}", LogCategory.Movement);
                }
                
                // Mark execution as complete
                isExecutingMoves = false;
            }
        }
        
        /// <summary>
        /// Enable or disable debug logging
        /// </summary>
        public void ToggleDebugMode(bool enabled)
        {
            debugLogTurns = enabled;
            SmartLogger.SetCategoryEnabled(LogCategory.TurnSystem, enabled);
            SmartLogger.Log($"Debug mode {(enabled ? "enabled" : "disabled")}", LogCategory.TurnSystem);
        }
        
        // Getters that use the turn state context
        public bool IsPlayerTurn() => GetActivePlayer() == 1 || GetActivePlayer() == 0;
        public bool IsExecutingMoves() => isExecutingMoves;
        public bool IsPhaseAdvancementLocked() => turnStateContext?.IsTransitionLocked ?? false;

        // ITurnSystem implementation
        public bool CanUnitMove(IDokkaebiUnit unit)
        {
            if (unit is DokkaebiUnit dokkaebiUnit)
            {
                return CanUnitMove(dokkaebiUnit);
            }
            return false;
        }
        
        public bool CanUnitUseAura(IDokkaebiUnit unit)
        {
            if (unit is DokkaebiUnit dokkaebiUnit)
            {
                return CanUnitUseAura(dokkaebiUnit);
            }
            return false;
        }
        
        public void QueueMove(IDokkaebiUnit unit, GridPosition targetPosition)
        {
            if (unit is DokkaebiUnit dokkaebiUnit)
            {
                QueueMove(dokkaebiUnit, targetPosition);
            }
        }
        
        public void EndMovementPhase()
        {
            if (turnStateContext != null && turnStateContext.GetCurrentPhase() == Common.TurnPhase.MovementPhase)
            {
                NextPhase();
            }
        }

        public void ForceTransitionTo(TurnPhase phase)
        {
            if (turnStateContext != null)
            {
                turnStateContext.ForceTransitionTo(phase);
            }
        }

        public bool RequestMovement(DokkaebiUnit unit, GridPosition targetPosition)
        {
            if (!unit || !turnStateContext.AllowsMovement() || isExecutingMoves)
            {
                return false;
            }
            
            // Add to pending moves
            pendingMoves[unit] = targetPosition;
            return true;
        }
        
        public bool RequestAuraActivation(DokkaebiUnit unit, bool isPlayer1)
        {
            if (!unit || !turnStateContext.AllowsAuraActivation(isPlayer1))
            {
                return false;
            }
            
            // Add to units that acted this phase
            unitsActedThisPhase.Add(unit);
            return true;
        }

        /// <summary>
        /// Sets the turn system state based on network data
        /// </summary>
        /// <param name="turnNumber">The turn number to set</param>
        /// <param name="phase">The phase to transition to</param>
        /// <param name="activePlayer">The active player (1 for player 1, 0 for player 2)</param>
        public void SetState(int turnNumber, TurnPhase phase, int activePlayer)
        {
            if (turnStateContext == null)
            {
                Debug.LogError("Cannot set state: TurnStateContext is null");
                return;
            }

            // Set turn number first
            turnStateContext.SetTurn(turnNumber);

            // Force transition to the target phase
            turnStateContext.ForceTransitionTo(phase);

            // Log the state change
            if (debugLogTurns)
            {
                SmartLogger.Log($"Turn system state set: Turn {turnNumber}, Phase {phase}, Active Player {activePlayer}", LogCategory.TurnSystem);
            }
        }
    }
}