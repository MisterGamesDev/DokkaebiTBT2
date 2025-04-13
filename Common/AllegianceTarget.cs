using UnityEngine;

namespace Dokkaebi.Common
{
    /// <summary>
    /// Defines which units a zone or ability can affect based on allegiance
    /// </summary>
    public enum AllegianceTarget
    {
        /// <summary>
        /// Can affect any unit regardless of allegiance
        /// </summary>
        Any,
        
        /// <summary>
        /// Can only affect allied units
        /// </summary>
        AllyOnly,
        
        /// <summary>
        /// Can only affect enemy units
        /// </summary>
        EnemyOnly
    }
} 