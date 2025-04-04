using System;
using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Interfaces;
// DamageType is already in the Dokkaebi.Common namespace (defined in CommonEnums.cs)

namespace Dokkaebi.Common
{
    /// <summary>
    /// Interface for the unit system to break cyclic dependencies
    /// </summary>
    public interface IUnitSystem
    {
        /// <summary>
        /// Get all units in the game
        /// </summary>
        IEnumerable<IDokkaebiUnit> GetAllUnits();
        
        /// <summary>
        /// Get all units for a specific player
        /// </summary>
        IEnumerable<IDokkaebiUnit> GetUnitsForPlayer(int playerId);
        
        /// <summary>
        /// Get a unit by its ID
        /// </summary>
        IDokkaebiUnit GetUnitById(int unitId);
        
        /// <summary>
        /// Get a unit at a specific grid position
        /// </summary>
        IDokkaebiUnit GetUnitAtPosition(GridPosition position);
        
        /// <summary>
        /// Check if a grid position has a unit
        /// </summary>
        bool HasUnitAtPosition(GridPosition position);
        
        /// <summary>
        /// Create a unit at the specified position
        /// </summary>
        IDokkaebiUnit CreateUnit(string unitDefinitionId, int playerId, GridPosition position);
        
        /// <summary>
        /// Move a unit to a new position
        /// </summary>
        void MoveUnit(int unitId, GridPosition newPosition);
        
        /// <summary>
        /// Remove a unit from the game
        /// </summary>
        void RemoveUnit(int unitId);
        
        /// <summary>
        /// Event fired when a unit is created
        /// </summary>
        event Action<IDokkaebiUnit> OnUnitCreated;
        
        /// <summary>
        /// Event fired when a unit is moved
        /// </summary>
        event Action<IDokkaebiUnit, GridPosition, GridPosition> OnUnitMoved;
        
        /// <summary>
        /// Event fired when a unit takes damage
        /// </summary>
        event Action<IDokkaebiUnit, int, DamageType> OnUnitDamageTaken;
        
        /// <summary>
        /// Event fired when a unit dies
        /// </summary>
        event Action<IDokkaebiUnit> OnUnitDied;
    }
}