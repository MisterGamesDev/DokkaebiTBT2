using UnityEngine;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Common
{
    /// <summary>
    /// Interface for status effects to break cyclic dependencies.
    /// Contains only essential properties needed across modules.
    /// </summary>
    public interface IStatusEffect
    {
        /// <summary>
        /// Unique identifier for the status effect
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Display name of the status effect
        /// </summary>
        string DisplayName { get; }
        
        /// <summary>
        /// Type of status effect
        /// </summary>
        StatusEffectType EffectType { get; }
        
        /// <summary>
        /// How many turns the effect lasts by default
        /// </summary>
        int DefaultDuration { get; }
        
        /// <summary>
        /// How potent the effect is (damage/healing amount, etc.)
        /// </summary>
        int Potency { get; }
        
        /// <summary>
        /// Whether the effect is permanent until dispelled
        /// </summary>
        bool IsPermanent { get; }
        
        /// <summary>
        /// Color used for visual representation of the effect
        /// </summary>
        Color EffectColor { get; }
    }
} 