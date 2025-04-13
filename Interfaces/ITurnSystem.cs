using System;
using Dokkaebi.Common;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Interfaces
{
    /// <summary>
    /// Interface for the turn system to break cyclic dependencies
    /// </summary>
    public interface ITurnSystem
    {
        /// <summary>
        /// Current turn number (starts at 1)
        /// </summary>
        int CurrentTurn { get; }
        
        /// <summary>
        /// Current phase of the turn
        /// </summary>
        TurnPhase CurrentPhase { get; }
        
        /// <summary>
        /// ID of the active player (index/position in the match)
        /// </summary>
        int ActivePlayerId { get; }
        
        /// <summary>
        /// Move to the next phase of the turn
        /// </summary>
        void NextPhase();
        
        /// <summary>
        /// Move to the next turn
        /// </summary>
        void NextTurn();
        
        /// <summary>
        /// Force transition to a specific phase
        /// </summary>
        void ForceTransitionTo(TurnPhase phase);
        
        /// <summary>
        /// Check if it's currently the player's turn
        /// </summary>
        bool IsPlayerTurn();
        
        /// <summary>
        /// Check if moves are currently being executed
        /// </summary>
        bool IsExecutingMoves();
        
        /// <summary>
        /// Check if phase advancement is locked
        /// </summary>
        bool IsPhaseAdvancementLocked();

        /// <summary>
        /// Gets the remaining time in the current phase
        /// </summary>
        float GetRemainingPhaseTime();

        /// <summary>
        /// Check if a unit can move in the current phase
        /// </summary>
        bool CanUnitMove(IDokkaebiUnit unit);
        
        /// <summary>
        /// Event fired when the turn phase changes
        /// </summary>
        event Action<TurnPhase> OnPhaseChanged;
        
        /// <summary>
        /// Event fired when the turn changes
        /// </summary>
        event Action<int> OnTurnChanged;
        
        /// <summary>
        /// Event fired when the active player changes
        /// </summary>
        event Action<int> OnActivePlayerChanged;
        
        /// <summary>
        /// Event fired when the movement phase starts
        /// </summary>
        event Action OnMovementPhaseStart;
        
        /// <summary>
        /// Event fired when the movement phase ends
        /// </summary>
        event Action OnMovementPhaseEnd;
        
        /// <summary>
        /// Event fired when turn resolution ends
        /// </summary>
        event Action OnTurnResolutionEnd;
    }
} 