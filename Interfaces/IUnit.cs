using UnityEngine;
using Dokkaebi.Grid;
using Dokkaebi.Common;

namespace Dokkaebi.Interfaces
{
    /// <summary>
    /// Core interface defining the fundamental properties and methods that any unit in the game must implement.
    /// This serves as the base interface for more specialized unit interfaces.
    /// </summary>
    public interface IUnit
    {
        /// <summary>
        /// The unique identifier for this unit instance
        /// </summary>
        int UnitId { get; }

        /// <summary>
        /// The display name of the unit
        /// </summary>
        string UnitName { get; }

        /// <summary>
        /// The current health points of the unit
        /// </summary>
        int CurrentHealth { get; }

        /// <summary>
        /// The maximum health points of the unit
        /// </summary>
        int MaxHealth { get; }

        /// <summary>
        /// Whether the unit is currently alive
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// The GameObject representing this unit in the scene
        /// </summary>
        GameObject GameObject { get; }

        /// <summary>
        /// The current grid position of the unit
        /// </summary>
        GridPosition CurrentPosition { get; }

        /// <summary>
        /// Modify the unit's health by the specified amount
        /// Positive values heal, negative values damage
        /// </summary>
        /// <param name="amount">Amount to modify health by</param>
        /// <param name="damageType">Type of damage being applied</param>
        void ModifyHealth(int amount, DamageType damageType = DamageType.Normal);

        /// <summary>
        /// Set the unit's position on the grid
        /// </summary>
        /// <param name="position">The new grid position</param>
        void SetGridPosition(GridPosition position);
    }
} 