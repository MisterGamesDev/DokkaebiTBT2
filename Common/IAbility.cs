using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Common
{
    /// <summary>
    /// Interface for abilities to break cyclic dependencies
    /// </summary>
    public interface IAbility
    {
        /// <summary>
        /// Unique identifier for the ability
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Display name of the ability
        /// </summary>
        string DisplayName { get; }
        
        /// <summary>
        /// Type of ability (Primary, Secondary, Ultimate, etc.)
        /// </summary>
        AbilityType AbilityType { get; }
        
        /// <summary>
        /// Cost to use the ability in Aura points
        /// </summary>
        int AuraCost { get; }
        
        /// <summary>
        /// Cooldown in turns
        /// </summary>
        int Cooldown { get; }
        
        /// <summary>
        /// Range in grid cells
        /// </summary>
        int Range { get; }
        
        /// <summary>
        /// Icon for the ability
        /// </summary>
        Sprite Icon { get; }
        
        /// <summary>
        /// Brief description of the ability
        /// </summary>
        string Description { get; }
    }
    
    /// <summary>
    /// Interface for ability instances that can be used
    /// </summary>
    public interface IAbilityInstance
    {
        /// <summary>
        /// Reference to the ability data
        /// </summary>
        IAbility AbilityData { get; }
        
        /// <summary>
        /// The owner of this ability instance
        /// </summary>
        int OwnerUnitId { get; }
        
        /// <summary>
        /// Current cooldown remaining
        /// </summary>
        int CurrentCooldown { get; }
        
        /// <summary>
        /// Is the ability on cooldown
        /// </summary>
        bool IsOnCooldown { get; }
        
        /// <summary>
        /// Can the ability be used currently (considering MP, cooldown, etc.)
        /// </summary>
        bool CanUse { get; }
        
        /// <summary>
        /// Get valid target positions for this ability
        /// </summary>
        List<GridPosition> GetValidTargets(GridPosition userPosition);
    }
} 