using Dokkaebi.Common;
using Dokkaebi.Core.Data;

namespace Dokkaebi.Interfaces
{
    /// <summary>
    /// Interface for status effect instances applied to units
    /// </summary>
    public interface IStatusEffectInstance
    {
        /// <summary>
        /// The type of status effect
        /// </summary>
        StatusEffectType StatusEffectType { get; }

        /// <summary>
        /// The base duration of the effect
        /// </summary>
        int Duration { get; }

        /// <summary>
        /// The remaining turns for this effect
        /// </summary>
        int RemainingTurns { get; }

        /// <summary>
        /// The data for this effect
        /// </summary>
        StatusEffectData Effect { get; }

        /// <summary>
        /// The unit that applied this effect
        /// </summary>
        int SourceUnitId { get; }

        /// <summary>
        /// The remaining duration in turns
        /// </summary>
        int RemainingDuration { get; set; }
    }
} 