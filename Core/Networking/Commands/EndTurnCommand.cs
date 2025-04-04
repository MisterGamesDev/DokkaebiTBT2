using System.Collections.Generic;
using UnityEngine;

namespace Dokkaebi.Core.Networking.Commands
{
    /// <summary>
    /// Command for ending the current turn phase
    /// </summary>
    public class EndTurnCommand : CommandBase
    {
        // Required for deserialization
        public EndTurnCommand() : base() { }

        public override string CommandType => "endTurn";

        public override Dictionary<string, object> Serialize()
        {
            return base.Serialize();
        }

        public override void Deserialize(Dictionary<string, object> data)
        {
            base.Deserialize(data);
        }

        public override bool Validate()
        {
            // Check if we have a turn system
            var turnSystem = Object.FindObjectOfType<DokkaebiTurnSystemCore>();
            if (turnSystem == null)
            {
                DebugLog("Cannot validate: Turn system not found");
                return false;
            }

            // Check if it's the player's turn
            if (!turnSystem.IsPlayerTurn())
            {
                DebugLog("Cannot end turn: Not player's turn");
                return false;
            }

            return true;
        }

        public override void Execute()
        {
            // Get the turn system
            var turnSystem = Object.FindObjectOfType<DokkaebiTurnSystemCore>();
            if (turnSystem == null)
            {
                DebugLog("Cannot execute: Turn system not found");
                return;
            }

            // End the current phase
            turnSystem.NextPhase();
        }
    }
} 