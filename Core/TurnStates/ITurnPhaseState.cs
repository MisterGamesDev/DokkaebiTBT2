using System;
using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Utilities;
using Dokkaebi.Common;

namespace Dokkaebi.Core.TurnStates
{
    /// <summary>
    /// Base interface for all turn phase states
    /// </summary>
    public interface ITurnPhaseState
    {
        TurnPhase PhaseType { get; }
        void Enter();
        void Update(float deltaTime);
        void Exit();
        ITurnPhaseState GetNextState();
        bool CanTransition();
        int GetActivePlayer();
        bool AllowsMovement();
        bool AllowsAuraActivation(bool isPlayerOne);
        float GetStateTimer();
        float GetPhaseTimeLimit();
    }

    /// <summary>
    /// Base class for all turn phase states
    /// </summary>
    public abstract class BaseTurnPhaseState : ITurnPhaseState
    {
        protected readonly TurnStateContext context;
        protected float stateTimer;
        protected float phaseTimeLimit;
        
        public abstract TurnPhase PhaseType { get; }
        
        protected BaseTurnPhaseState(TurnStateContext context)
        {
            this.context = context;
            // Set the phase time limit based on the phase type
            phaseTimeLimit = GetPhaseTimeLimit();
        }
        
        protected virtual float GetPhaseTimeLimit()
        {
            var turnSystem = context.GetTurnSystem();
            switch (PhaseType)
            {
                case TurnPhase.Opening:
                    return turnSystem.OpeningPhaseDuration;
                case TurnPhase.MovementPhase:
                    return turnSystem.MovementPhaseDuration;
                case TurnPhase.AuraPhase1A:
                case TurnPhase.AuraPhase1B:
                case TurnPhase.AuraPhase2A:
                case TurnPhase.AuraPhase2B:
                    return turnSystem.AuraChargingPhaseDuration;
                default:
                    return 30f; // Default fallback
            }
        }
        
        public virtual void Enter()
        {
            stateTimer = 0f;
            SmartLogger.Log($"Entering {PhaseType} phase", LogCategory.TurnSystem);
        }
        
        public virtual void Update(float deltaTime)
        {
            stateTimer += deltaTime;
            
            // Log timer state periodically (every second)
            if (Mathf.FloorToInt(stateTimer) > Mathf.FloorToInt(stateTimer - deltaTime))
            {
                SmartLogger.Log($"[{PhaseType}] Phase timer: {stateTimer:F2}/{phaseTimeLimit:F2}, CanTransition: {CanTransition()}", LogCategory.TurnSystem);
            }
            
            // Auto-advance when time expires and phase limit is positive
            if (phaseTimeLimit > 0 && stateTimer >= phaseTimeLimit && CanTransition())
            {
                SmartLogger.Log($"[{PhaseType}] Phase time limit reached ({stateTimer:F2} >= {phaseTimeLimit:F2}). CanTransition()={CanTransition()}. Attempting TransitionToNextState().", LogCategory.TurnSystem, context.GetTurnSystem() as MonoBehaviour);
                context.TransitionToNextState();
            }
        }
        
        public virtual void Exit()
        {
            SmartLogger.Log($"Exiting {PhaseType} phase", LogCategory.TurnSystem);
        }
        
        public abstract ITurnPhaseState GetNextState();
        
        public virtual bool CanTransition()
        {
            return !context.IsTransitionLocked;
        }
        
        public virtual int GetActivePlayer()
        {
            // For AuraPhase1A and AuraPhase2A, player 1 is active
            if (PhaseType == TurnPhase.AuraPhase1A || PhaseType == TurnPhase.AuraPhase2A)
                return 1;
            // For AuraPhase1B and AuraPhase2B, player 2 is active
            if (PhaseType == TurnPhase.AuraPhase1B || PhaseType == TurnPhase.AuraPhase2B)
                return 2;
            // For movement phase and opening, both players are active
            return 0;
        }
        
        public virtual bool AllowsMovement()
        {
            return PhaseType == TurnPhase.MovementPhase;
        }
        
        public virtual bool AllowsAuraActivation(bool isPlayerOne)
        {
            // Only allow aura activation in the appropriate phase for each player
            if (isPlayerOne)
            {
                return PhaseType == TurnPhase.AuraPhase1A || PhaseType == TurnPhase.AuraPhase2A;
            }
            else
            {
                return PhaseType == TurnPhase.AuraPhase1B || PhaseType == TurnPhase.AuraPhase2B;
            }
        }

        public float GetStateTimer()
        {
            return stateTimer;
        }

        float ITurnPhaseState.GetPhaseTimeLimit()
        {
            return phaseTimeLimit;
        }
    }

    /// <summary>
    /// Opening phase - initial game setup
    /// </summary>
    public class OpeningPhaseState : BaseTurnPhaseState
    {
        public override TurnPhase PhaseType => TurnPhase.Opening;
        
        public OpeningPhaseState(TurnStateContext context) : base(context) { }
        
        public override ITurnPhaseState GetNextState()
        {
            return new MovementPhaseState(context);
        }
        
        public override int GetActivePlayer()
        {
            return 0; // Both players active
        }
        
        public override bool AllowsMovement()
        {
            return true; // Special case for initial placement
        }
    }

    /// <summary>
    /// Movement phase - both players move simultaneously
    /// </summary>
    public class MovementPhaseState : BaseTurnPhaseState
    {
        public override TurnPhase PhaseType => TurnPhase.MovementPhase;
        
        public MovementPhaseState(TurnStateContext context) : base(context) 
        {
            SmartLogger.Log($"[MovementPhaseState] Initialized with phaseTimeLimit: {phaseTimeLimit:F2}", LogCategory.TurnSystem);
        }
        
        public override void Enter()
        {
            base.Enter();
            SmartLogger.Log("[MovementPhaseState] Entered movement phase", LogCategory.TurnSystem);
        }
        
        public override void Exit()
        {
            SmartLogger.Log("[MovementPhaseState] Exiting movement phase, executing pending moves", LogCategory.TurnSystem);
            base.Exit();
            
            // Execute any remaining pending moves before transitioning
            var turnSystem = context.GetTurnSystem();
            if (turnSystem is DokkaebiTurnSystemCore core)
            {
                SmartLogger.Log("[MovementPhaseState] Executing any remaining pending moves before exiting movement phase", LogCategory.TurnSystem);
                core.ExecuteAllPendingMoves();
            }
            else
            {
                SmartLogger.LogWarning("[MovementPhaseState] TurnSystem is not DokkaebiTurnSystemCore, skipping pending moves execution", LogCategory.TurnSystem);
            }
        }
        
        public override ITurnPhaseState GetNextState()
        {
            SmartLogger.Log("[MovementPhaseState] Creating next state (AuraPhase1A)", LogCategory.TurnSystem);
            return new AuraPhase1AState(context);
        }
        
        public override int GetActivePlayer()
        {
            return 0; // Both players active in movement phase
        }
    }

    /// <summary>
    /// First aura phase for the first player
    /// </summary>
    public class AuraPhase1AState : BaseTurnPhaseState
    {
        public override TurnPhase PhaseType => TurnPhase.AuraPhase1A;
        
        public AuraPhase1AState(TurnStateContext context) : base(context) { }
        
        public override ITurnPhaseState GetNextState()
        {
            return new AuraPhase1BState(context);
        }

        public override void Exit()
        {
            SmartLogger.Log("[AuraPhase1AState.Exit] Exiting Phase 1A", LogCategory.TurnSystem);
            base.Exit();
        }
    }

    /// <summary>
    /// First aura phase for the second player
    /// </summary>
    public class AuraPhase1BState : BaseTurnPhaseState
    {
        public override TurnPhase PhaseType => TurnPhase.AuraPhase1B;
        
        public AuraPhase1BState(TurnStateContext context) : base(context) { }
        
        public override ITurnPhaseState GetNextState()
        {
            return new AuraPhase2AState(context);
        }

        public override void Exit()
        {
            SmartLogger.Log("[AuraPhase1BState.Exit] Exiting Phase 1B", LogCategory.TurnSystem);
            base.Exit();
        }
    }

    /// <summary>
    /// Second aura phase for the first player
    /// </summary>
    public class AuraPhase2AState : BaseTurnPhaseState
    {
        public override TurnPhase PhaseType => TurnPhase.AuraPhase2A;
        
        public AuraPhase2AState(TurnStateContext context) : base(context) { }
        
        public override ITurnPhaseState GetNextState()
        {
            return new AuraPhase2BState(context);
        }

        public override void Exit()
        {
            SmartLogger.Log("[AuraPhase2AState.Exit] Exiting Phase 2A", LogCategory.TurnSystem);
            base.Exit();
        }
    }

    /// <summary>
    /// Second aura phase for the second player
    /// </summary>
    public class AuraPhase2BState : BaseTurnPhaseState
    {
        public override TurnPhase PhaseType => TurnPhase.AuraPhase2B;
        
        public AuraPhase2BState(TurnStateContext context) : base(context) { }
        
        public override ITurnPhaseState GetNextState()
        {
            // After phase 2B, we go back to movement phase of the next turn
            return new MovementPhaseState(context);
        }

        public override void Exit()
        {
            SmartLogger.Log("[AuraPhase2BState.Exit START] Beginning turn resolution phase", LogCategory.TurnSystem);
            base.Exit();
            
            SmartLogger.Log("[AuraPhase2BState.Exit] Triggering turn resolution end", LogCategory.TurnSystem);
            SmartLogger.Log("[AuraPhase2BState.Exit] About to trigger TurnResolutionEnd event - this should trigger zone effects", LogCategory.TurnSystem);
            // Signal that the turn is about to end and resolve turn effects
            // (This will trigger zone effects, etc. via TurnResolutionEnd event)
            context.TriggerTurnResolutionEnd();
            
            SmartLogger.Log("[AuraPhase2BState.Exit] Incrementing turn counter", LogCategory.TurnSystem);
            // Signal that we're about to start a new turn
            context.IncrementTurn();
            
            SmartLogger.Log("[AuraPhase2BState.Exit END] Turn resolution phase complete", LogCategory.TurnSystem);
        }
    }
}