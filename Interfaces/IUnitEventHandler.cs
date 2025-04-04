using System;
using Dokkaebi.Common;

namespace Dokkaebi.Interfaces
{
    /// <summary>
    /// Interface for handling unit events like damage, healing, and status effects
    /// </summary>
    public interface IUnitEventHandler
    {
        /// <summary>
        /// Event fired when the unit takes damage
        /// </summary>
        event Action<int, DamageType> OnDamageTaken;

        /// <summary>
        /// Event fired when the unit receives healing
        /// </summary>
        event Action<int> OnHealingReceived;

        /// <summary>
        /// Event fired when a status effect is applied to the unit
        /// </summary>
        event Action<IStatusEffectInstance> OnStatusEffectApplied;

        /// <summary>
        /// Event fired when a status effect is removed from the unit
        /// </summary>
        event Action<IStatusEffectInstance> OnStatusEffectRemoved;
    }
} 