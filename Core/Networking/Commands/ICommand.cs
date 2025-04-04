using System;
using System.Collections.Generic;

namespace Dokkaebi.Core.Networking.Commands
{
    /// <summary>
    /// Base interface for all commands in the Command pattern
    /// All commands must be serializable for network transmission
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Unique identifier for this command type
        /// Used for serialization/deserialization
        /// </summary>
        string CommandType { get; }
        
        /// <summary>
        /// Convert the command to a serializable dictionary for network transmission
        /// </summary>
        Dictionary<string, object> Serialize();
        
        /// <summary>
        /// Locally validate if the command can be executed
        /// This is a quick client-side check before sending to the server
        /// </summary>
        bool Validate();
        
        /// <summary>
        /// Execute the command locally (client-side prediction)
        /// Only used if prediction is enabled
        /// </summary>
        void Execute();
    }
} 