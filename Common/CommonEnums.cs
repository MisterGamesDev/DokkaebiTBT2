using System;

namespace Dokkaebi.Common
{
    /// <summary>
    /// Common enums for cross-module reference to avoid circular dependencies.
    /// </summary>
    
    /// <summary>
    /// Phases within a game turn
    /// </summary>
    public enum TurnPhase
    {
        Opening,            // Initial setup phase
        MovementPhase,      // Units can move
        AuraPhase1A,       // First aura phase for player 1
        AuraPhase1B,       // First aura phase for player 2
        AuraPhase2A,       // Second aura phase for player 1
        AuraPhase2B,       // Second aura phase for player 2
        Resolution,        // Turn cleanup phase
        EndTurn,          // End of turn phase
        GameOver          // Game has ended
    }

    /// <summary>
    /// Types of zones
    /// </summary>
    public enum ZoneType
    {
        Damage,
        Healing,
        SpeedBuff,
        SpeedDebuff,
        DamageBuff,
        DamageDebuff,
        Block,
        Teleport,
        Vision,
        Stealth,
        Trap
    }

    /// <summary>
    /// Team types for units
    /// </summary>
    public enum TeamType
    {
        Player1,
        Player2,
        Neutral,
        Environment
    }

    /// <summary>
    /// Types of targeting for abilities
    /// </summary>
    public enum TargetingType
    {
        Point,           // Target a specific point on the grid
        Unit,            // Target a specific unit
        Area,            // Target an area (like a circle or square)
        Line,            // Target a line from the caster
        Cone,            // Target a cone from the caster
        Self,            // Target the caster
        AllUnits,        // Target all units in the game
        AlliedUnits,     // Target all allied units
        EnemyUnits       // Target all enemy units
    }

    /// <summary>
    /// Targeting relationship requirements for abilities
    /// </summary>
    public enum TargetRelationship
    {
        Any,            // Can target any unit
        Ally,           // Can only target allied units
        Enemy,          // Can only target enemy units
        Self,           // Can only target self
        NotSelf         // Can target any unit except self
    }
} 