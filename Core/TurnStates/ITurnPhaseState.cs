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
    }

    /// <summary>
    /// Base class for all turn phase states
    /// </summary>
    public abstract class BaseTurnPhaseState : ITurnPhaseState
    {
        protected readonly TurnStateContext context;
        protected float stateTimer;
        protected float phaseTimeLimit = 30f;
        
        public abstract TurnPhase PhaseType { get; }
        
        protected BaseTurnPhaseState(TurnStateContext context)
        {
            this.context = context;
        }
        
        public virtual void Enter()
        {
            stateTimer = 0f;
            SmartLogger.Log($"Entering {PhaseType} phase", LogCategory.TurnSystem);
        }
        
        public virtual void Update(float deltaTime)
        {
            stateTimer += deltaTime;
            
            // Auto-advance when time expires
            if (stateTimer >= phaseTimeLimit && CanTransition())
            {
                SmartLogger.Log($"Phase time limit reached for {PhaseType}", LogCategory.TurnSystem);
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
            bool isAuraPhase = PhaseType == TurnPhase.AuraPhase1A || 
                              PhaseType == TurnPhase.AuraPhase1B || 
                              PhaseType == TurnPhase.AuraPhase2A || 
                              PhaseType == TurnPhase.AuraPhase2B;
            
            if (!isAuraPhase)
                return false;
                
            int activePlayer = GetActivePlayer();
            return (isPlayerOne && activePlayer == 1) || (!isPlayerOne && activePlayer == 2);
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
        
        public MovementPhaseState(TurnStateContext context) : base(context) { }
        
        public override ITurnPhaseState GetNextState()
        {
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
            base.Exit();
            
            // Signal that the turn is about to end and resolve turn effects
            // (This will trigger zone effects, etc. via TurnResolutionEnd event)
            context.TriggerTurnResolutionEnd();
            
            // Signal that we're about to start a new turn
            context.IncrementTurn();
        }
    }
}