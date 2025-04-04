using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dokkaebi.Interfaces
{
    /// <summary>
    /// Interface for the ability system to break cyclic dependencies
    /// </summary>
    public interface IAbilitySystem
    {
        /// <summary>
        /// Execute an ability
        /// </summary>
        bool ExecuteAbility(IAbilityData ability, IDokkaebiUnit caster, GridPosition targetPosition, IDokkaebiUnit targetUnit, bool isOverload);
        
        /// <summary>
        /// Get valid target positions for an ability
        /// </summary>
        HashSet<GridPosition> GetValidTargets(IAbilityData ability, IDokkaebiUnit caster);
        
        /// <summary>
        /// Check if a unit can use an ability
        /// </summary>
        bool CanUseAbility(IDokkaebiUnit unit, IAbilityData ability);
        
        /// <summary>
        /// Event fired when an ability is executed
        /// </summary>
        event Action<IDokkaebiUnit, IAbilityData, GridPosition> OnAbilityExecuted;
    }
    
    /// <summary>
    /// Interface for ability data
    /// </summary>
    public interface IAbilityData
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
        /// Description of the ability
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Aura cost to use the ability
        /// </summary>
        int AuraCost { get; }
        
        /// <summary>
        /// Range of the ability in grid units
        /// </summary>
        int Range { get; }
        
        /// <summary>
        /// Whether the ability can target the caster
        /// </summary>
        bool TargetsSelf { get; }
        
        /// <summary>
        /// Whether the ability can target allies
        /// </summary>
        bool TargetsAlly { get; }
        
        /// <summary>
        /// Whether the ability can target enemies
        /// </summary>
        bool TargetsEnemy { get; }
        
        /// <summary>
        /// Whether the ability can target the ground
        /// </summary>
        bool TargetsGround { get; }
        
        /// <summary>
        /// The type of the ability for cooldown tracking
        /// </summary>
        AbilityType AbilityType { get; }
    }
} 