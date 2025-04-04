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
        
        public event Action<TurnPhase> OnPhaseChanged;
        public event Action<int> OnTurnChanged;
        public event Action OnMovementPhaseStart;
        public event Action OnMovementPhaseEnd;
        public event Action OnTurnResolutionEnd;
        
        public bool IsTransitionLocked { get; private set; }
        
        public TurnStateContext()
        {
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
            if (currentState == null || IsTransitionLocked)
                return;
                
            if (!currentState.CanTransition())
                return;
                
            ITurnPhaseState nextState = currentState.GetNextState();
            if (nextState != null)
            {
                TransitionTo(nextState);
            }
        }
        
        public void TransitionTo(ITurnPhaseState newState)
        {
            if (currentState == null || IsTransitionLocked)
                return;
                
            TurnPhase oldPhase = currentState.PhaseType;
            
            // Exit current state
            currentState.Exit();
            
            // Enter new state
            currentState = newState;
            currentState.Enter();
            
            // Fire phase changed event
            if (oldPhase != currentState.PhaseType)
            {
                OnPhaseChanged?.Invoke(currentState.PhaseType);
                
                // Special handling for movement phase
                if (currentState.PhaseType == TurnPhase.MovementPhase)
                {
                    OnMovementPhaseStart?.Invoke();
                }
                else if (oldPhase == TurnPhase.MovementPhase)
                {
                    OnMovementPhaseEnd?.Invoke();
                }
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