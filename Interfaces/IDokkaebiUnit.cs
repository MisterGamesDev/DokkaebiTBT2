using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Common;
using Dokkaebi.Core.Data;
using System;

namespace Dokkaebi.Interfaces
{
    /// <summary>
    /// Interface defining the minimum properties and methods required for a unit
    /// to interact with the grid system. This avoids direct dependencies between
    /// the grid system and the unit implementation.
    /// </summary>
    public interface IDokkaebiUnit
    {
        /// <summary>
        /// Event fired when the unit moves to a new position
        /// </summary>
        event Action<IDokkaebiUnit, GridPosition, GridPosition> OnUnitMoved;

        /// <summary>
        /// The unique identifier for this unit
        /// </summary>
        int UnitId { get; }
        
        /// <summary>
        /// The current grid position of the unit
        /// </summary>
        GridPosition CurrentGridPosition { get; }
        
        /// <summary>
        /// Whether the unit is currently alive
        /// </summary>
        bool IsAlive { get; }
        
        /// <summary>
        /// The team that this unit belongs to
        /// </summary>
        int TeamId { get; }
        
        /// <summary>
        /// The GameObject representing this unit
        /// </summary>
        GameObject GameObject { get; }
        
        /// <summary>
        /// Whether this unit is controlled by the player
        /// </summary>
        bool IsPlayerControlled { get; }
        
        /// <summary>
        /// The maximum movement range of the unit
        /// </summary>
        int MovementRange { get; }
        
        /// <summary>
        /// Current health points of the unit
        /// </summary>
        int CurrentHealth { get; }

        /// <summary>
        /// The display name of the unit
        /// </summary>
        string DisplayName { get; }
        
        /// <summary>
        /// Move the unit to a new grid position
        /// </summary>
        void MoveToGridPosition(GridPosition newPosition);
        
        /// <summary>
        /// Updates the unit's internally stored grid position state.
        /// Typically called by the movement system itself upon changing cells.
        /// </summary>
        void SetGridPosition(GridPosition newPosition);
        
        /// <summary>
        /// Gets the display name of the unit.
        /// </summary>
        string GetUnitName();

        /// <summary>
        /// Get all valid positions that the unit can move to
        /// </summary>
        List<GridPosition> GetValidMovePositions();

        /// <summary>
        /// Get all status effects currently applied to the unit
        /// </summary>
        List<IStatusEffectInstance> GetStatusEffects();

        /// <summary>
        /// Add a status effect to this unit
        /// </summary>
        void AddStatusEffect(IStatusEffectInstance effect);

        /// <summary>
        /// Remove a status effect from this unit
        /// </summary>
        void RemoveStatusEffect(IStatusEffectInstance effect);

        /// <summary>
        /// Check if this unit has a specific status effect
        /// </summary>
        bool HasStatusEffect(StatusEffectType effectType);
    }
} 