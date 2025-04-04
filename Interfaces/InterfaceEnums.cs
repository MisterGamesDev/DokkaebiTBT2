using System;

namespace Dokkaebi.Interfaces
{
    /// <summary>
    /// Types of abilities
    /// </summary>
    public enum AbilityType
    {
        Primary,
        Secondary,
        Ultimate,
        Passive,
        Reaction,
        Special
    }

    /// <summary>
    /// Types of status effects
    /// </summary>
    public enum StatusEffectType
    {
        None,
        Stun,
        Root,
        Silence,
        Blind,
        Invulnerable,
        DamageBoost,
        SpeedBoost,
        Poison,
        Burn,
        Shield,
        Invisible
    }

    /// <summary>
    /// Types of damage that can be applied to units
    /// </summary>
    public enum DamageType
    {
        Normal,
        Physical,
        Magical,
        True,
        Fire,
        Ice,
        Water,
        Earth,
        Air,
        Light,
        Shadow,
        Chaos,
        Poison,
        Electric,
        Lightning,
        Healing
    }
} 