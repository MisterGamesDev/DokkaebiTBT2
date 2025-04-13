using System;
using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Common
{
    /// <summary>
    /// Extended interface for Dokkaebi units with additional functionality
    /// </summary>
    public interface IExtendedDokkaebiUnit
    {
        /// <summary>
        /// Unique identifier for this unit instance
        /// </summary>
        int UnitId { get; }
        
        /// <summary>
        /// Definition ID for this unit type
        /// </summary>
        string DefinitionId { get; }
        
        /// <summary>
        /// Display name of the unit
        /// </summary>
        string DisplayName { get; }
        
        /// <summary>
        /// ID of the player who controls this unit
        /// </summary>
        int OwnerId { get; }
        
        /// <summary>
        /// Current grid position
        /// </summary>
        GridPosition GridPosition { get; }
        
        /// <summary>
        /// Current health points
        /// </summary>
        int CurrentHP { get; }
        
        /// <summary>
        /// Maximum health points
        /// </summary>
        int MaxHP { get; }
        
        /// <summary>
        /// Current MP (used for abilities)
        /// </summary>
        float CurrentMP { get; }
        
        /// <summary>
        /// Maximum MP
        /// </summary>
        float MaxMP { get; }
        
        /// <summary>
        /// Movement range in grid cells
        /// </summary>
        int MovementRange { get; }
        
        /// <summary>
        /// Apply damage to this unit
        /// </summary>
        void TakeDamage(int amount, DamageType damageType);
        
        /// <summary>
        /// Heal this unit
        /// </summary>
        void Heal(int amount);
        
        /// <summary>
        /// Check if this unit is dead
        /// </summary>
        bool IsDead { get; }
        
        /// <summary>
        /// Get all abilities this unit has
        /// </summary>
        IEnumerable<IAbilityInstance> GetAbilities();
        
        /// <summary>
        /// Get all status effects on this unit
        /// </summary>
        IEnumerable<IStatusEffectInstance> GetStatusEffects();
        
        /// <summary>
        /// Check if this unit has a specific status effect
        /// </summary>
        bool HasStatusEffect(StatusEffectType statusEffectType);
        
        /// <summary>
        /// Apply a status effect to this unit
        /// </summary>
        void ApplyStatusEffect(StatusEffectType statusEffectType, int duration, int ownerUnitId);
        
        /// <summary>
        /// Remove a status effect from this unit
        /// </summary>
        void RemoveStatusEffect(StatusEffectType statusEffectType);
    }
} 