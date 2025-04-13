using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dokkaebi.Core.Networking.Commands
{
    /// <summary>
    /// Factory class for creating and deserializing Command objects
    /// </summary>
    public static class CommandFactory
    {
        // Register command types with their string identifiers
        private static readonly Dictionary<string, Type> CommandTypes = new Dictionary<string, Type>
        {
            { "move", typeof(MoveCommand) },
            { "ability", typeof(AbilityCommand) },
            { "endTurn", typeof(EndTurnCommand) },
            { "reposition", typeof(RepositionCommand) }
            // Add more command types as they are created
        };

        /// <summary>
        /// Create a command from serialized data
        /// </summary>
        public static ICommand CreateFromDictionary(Dictionary<string, object> data)
        {
            if (!data.TryGetValue("commandType", out object commandTypeObj) || !(commandTypeObj is string commandType))
            {
                Debug.LogError("Invalid command data: missing or invalid commandType");
                return null;
            }

            if (!CommandTypes.TryGetValue(commandType, out Type type))
            {
                Debug.LogError($"Unknown command type: {commandType}");
                return null;
            }

            try
            {
                // Create instance via reflection
                ICommand command = (ICommand)Activator.CreateInstance(type);
                
                // Use a method on the concrete class to deserialize
                if (command is IDeserializable deserializable)
                {
                    deserializable.Deserialize(data);
                    return command;
                }
                
                Debug.LogError($"Command type {type.Name} does not implement IDeserializable");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create command of type {commandType}: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// Interface for commands that can be deserialized
    /// </summary>
    public interface IDeserializable
    {
        void Deserialize(Dictionary<string, object> data);
    }
} 
