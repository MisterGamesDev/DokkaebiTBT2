using System.Collections.Generic;
using UnityEngine;

namespace Dokkaebi.Core.Networking.Commands
{
    /// <summary>
    /// Abstract base class for all commands
    /// Provides common functionality and serialization support
    /// </summary>
    public abstract class CommandBase : ICommand, IDeserializable
    {
        // Common command data
        protected string commandId;
        protected int playerId;

        /// <summary>
        /// Get the command type for serialization
        /// </summary>
        public abstract string CommandType { get; }

        protected CommandBase()
        {
            // Generate a unique ID for this command instance
            commandId = System.Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Set the player ID for this command
        /// </summary>
        public void SetPlayerId(int id)
        {
            playerId = id;
        }

        /// <summary>
        /// Serialize the command to a dictionary for network transmission
        /// </summary>
        public virtual Dictionary<string, object> Serialize()
        {
            var data = new Dictionary<string, object>
            {
                { "commandType", CommandType },
                { "commandId", commandId },
                { "playerId", playerId }
            };
            
            return data;
        }

        /// <summary>
        /// Validate the command before execution
        /// </summary>
        public abstract bool Validate();

        /// <summary>
        /// Execute the command locally
        /// </summary>
        public abstract void Execute();

        /// <summary>
        /// Deserialize the command from a dictionary
        /// </summary>
        public virtual void Deserialize(Dictionary<string, object> data)
        {
            if (data.TryGetValue("commandId", out object commandIdObj) && commandIdObj is string id)
            {
                commandId = id;
            }
            
            if (data.TryGetValue("playerId", out object playerIdObj))
            {
                if (playerIdObj is long playerIdLong)
                {
                    playerId = (int)playerIdLong;
                }
                else if (playerIdObj is int playerIdInt)
                {
                    playerId = playerIdInt;
                }
            }
        }

        /// <summary>
        /// Log debug information about this command
        /// </summary>
        protected void DebugLog(string message)
        {
            Debug.Log($"[{CommandType}] {message}");
        }
    }
} 
