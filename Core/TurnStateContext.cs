using System;
using UnityEngine;
using Dokkaebi.Common;
using Dokkaebi.Utilities;
using Dokkaebi.Core.TurnStates;

namespace Dokkaebi.Core
{
    /// <summary>
    /// Context class for managing turn phase states
    /// </summary>
    public class TurnStateContext
    {
        private ITurnPhaseState currentState;
        private int currentTurn = 1;
        private readonly DokkaebiTurnSystemCore turnSystem;
        
        public event Action<TurnPhase> OnPhaseChanged;
        public event Action<int> OnTurnChanged;
        public event Action OnMovementPhaseStart;
        public event Action OnMovementPhaseEnd;
        public event Action OnTurnResolutionEnd;
        
        public bool IsTransitionLocked { get; private set; }
        
        public DokkaebiTurnSystemCore GetTurnSystem() => turnSystem;
        
        public TurnStateContext(DokkaebiTurnSystemCore turnSystem)
        {
            this.turnSystem = turnSystem;
            // Initialize with opening phase
            currentState = new OpeningPhaseState(this);
            currentState.Enter();
        }
        
        public void Update(float deltaTime)
        {
            if (currentState != null)
            {
                currentState.Update(deltaTime);
            }
        }
        
        public void TransitionToNextState()
        {
            SmartLogger.Log($"[TurnStateContext.TransitionToNextState] Called. Current state: {currentState?.PhaseType}, IsTransitionLocked: {IsTransitionLocked}", LogCategory.TurnSystem);
            
            if (currentState == null || IsTransitionLocked)
            {
                SmartLogger.LogWarning($"[TurnStateContext.TransitionToNextState] Cannot transition: currentState is {(currentState == null ? "null" : "not null")}, IsTransitionLocked={IsTransitionLocked}", LogCategory.TurnSystem);
                return;
            }
            
            ITurnPhaseState nextState = currentState.GetNextState();
            if (nextState == null)
            {
                SmartLogger.LogError("[TurnStateContext.TransitionToNextState] GetNextState() returned null!", LogCategory.TurnSystem);
                return;
            }
            
            SmartLogger.Log($"[TurnStateContext.TransitionToNextState] Transitioning from {currentState.PhaseType} to {nextState.PhaseType}", LogCategory.TurnSystem);
            TransitionTo(nextState);
        }
        
        public void TransitionTo(ITurnPhaseState newState)
        {
            SmartLogger.Log($"[TurnStateContext.TransitionTo] Called with new state: {newState?.PhaseType}. Current state: {currentState?.PhaseType}, IsTransitionLocked: {IsTransitionLocked}", LogCategory.TurnSystem);
            
            if (currentState == null || IsTransitionLocked)
            {
                SmartLogger.LogWarning($"[TurnStateContext.TransitionTo] Cannot transition: currentState is {(currentState == null ? "null" : "not null")}, IsTransitionLocked={IsTransitionLocked}", LogCategory.TurnSystem);
                return;
            }
            
            TurnPhase oldPhase = currentState.PhaseType;
            
            SmartLogger.Log($"[TurnStateContext.TransitionTo] Calling Exit() on current state {oldPhase}", LogCategory.TurnSystem);
            currentState.Exit();
            
            SmartLogger.Log($"[TurnStateContext.TransitionTo] Setting new state {newState.PhaseType} and calling Enter()", LogCategory.TurnSystem);
            currentState = newState;
            currentState.Enter();
            
            if (oldPhase != currentState.PhaseType)
            {
                SmartLogger.Log($"[TurnStateContext.TransitionTo] Phase changed from {oldPhase} to {currentState.PhaseType}, firing events", LogCategory.TurnSystem);
                OnPhaseChanged?.Invoke(currentState.PhaseType);
                
                if (currentState.PhaseType == TurnPhase.MovementPhase)
                {
                    SmartLogger.Log("[TurnStateContext.TransitionTo] Movement phase started, firing OnMovementPhaseStart", LogCategory.TurnSystem);
                    OnMovementPhaseStart?.Invoke();
                }
                else if (oldPhase == TurnPhase.MovementPhase)
                {
                    SmartLogger.Log("[TurnStateContext.TransitionTo] Movement phase ended, firing OnMovementPhaseEnd", LogCategory.TurnSystem);
                    OnMovementPhaseEnd?.Invoke();
                }
            }
            else
            {
                SmartLogger.Log($"[TurnStateContext.TransitionTo] Phase remained the same: {currentState.PhaseType}", LogCategory.TurnSystem);
            }
        }
        
        /// <summary>
        /// Triggers the OnTurnResolutionEnd event without incrementing the turn
        /// </summary>
        public void TriggerTurnResolutionEnd()
        {
            SmartLogger.Log($"Resolving turn {currentTurn} effects", LogCategory.TurnSystem);
            OnTurnResolutionEnd?.Invoke();
        }
        
        public void IncrementTurn()
        {
            // We no longer trigger OnTurnResolutionEnd here since it's now called explicitly in AuraPhase2BState.Exit()
            
            currentTurn++;
            OnTurnChanged?.Invoke(currentTurn);
            SmartLogger.Log($"Turn incremented to {currentTurn}", LogCategory.TurnSystem);
        }
        
        public void LockTransition()
        {
            IsTransitionLocked = true;
            SmartLogger.Log("Phase transitions locked", LogCategory.TurnSystem);
        }
        
        public void UnlockTransition()
        {
            IsTransitionLocked = false;
            SmartLogger.Log("Phase transitions unlocked", LogCategory.TurnSystem);
        }
        
        public void ForceTransitionTo(TurnPhase targetPhase)
        {
            // Temporarily unlock transitions
            bool wasLocked = IsTransitionLocked;
            IsTransitionLocked = false;
            
            // Create the appropriate state
            ITurnPhaseState targetState = null;
            switch (targetPhase)
            {
                case TurnPhase.Opening:
                    targetState = new OpeningPhaseState(this);
                    break;
                case TurnPhase.MovementPhase:
                    targetState = new MovementPhaseState(this);
                    break;
                case TurnPhase.AuraPhase1A:
                    targetState = new AuraPhase1AState(this);
                    break;
                case TurnPhase.AuraPhase1B:
                    targetState = new AuraPhase1BState(this);
                    break;
                case TurnPhase.AuraPhase2A:
                    targetState = new AuraPhase2AState(this);
                    break;
                case TurnPhase.AuraPhase2B:
                    targetState = new AuraPhase2BState(this);
                    break;
            }
            
            // Transition to the target state
            if (targetState != null)
            {
                TransitionTo(targetState);
                SmartLogger.Log($"Forced transition to {targetPhase}", LogCategory.TurnSystem);
            }
            
            // Restore lock state
            IsTransitionLocked = wasLocked;
        }
        
        // Getters
        public TurnPhase GetCurrentPhase() => currentState?.PhaseType ?? TurnPhase.Opening;
        public int GetCurrentTurn() => currentTurn;
        public int GetActivePlayer() => currentState?.GetActivePlayer() ?? 0;
        public bool AllowsMovement() => currentState?.AllowsMovement() ?? false;
        public bool AllowsAuraActivation(bool isPlayerOne) => currentState?.AllowsAuraActivation(isPlayerOne) ?? false;
        
        /// <summary>
        /// Gets the remaining time in the current phase
        /// </summary>
        public float GetRemainingTime()
        {
            if (currentState == null) return 0f;
            return Mathf.Max(0f, currentState.GetPhaseTimeLimit() - currentState.GetStateTimer());
        }
        
        // Force a specific turn number (for debugging or save/load)
        public void SetTurn(int turnNumber)
        {
            if (turnNumber < 1)
                turnNumber = 1;
                
            if (currentTurn != turnNumber)
            {
                currentTurn = turnNumber;
                OnTurnChanged?.Invoke(currentTurn);
                SmartLogger.Log($"Turn set to {currentTurn}", LogCategory.TurnSystem);
            }
        }
    }
}