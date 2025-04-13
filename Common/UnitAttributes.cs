namespace Dokkaebi.Common
{
    /// <summary>
    /// Types of unit attributes that can be modified by status effects
    /// </summary>
    public enum UnitAttributeType
    {
        None,
        Health,
        Damage,
        MovementSpeed,
        AttackSpeed,
        Defense,
        CriticalChance,
        CriticalDamage,
        HealingReceived,
        DamageDealt,
        DamageTaken,
        AbilityCooldown,
        AbilityPower,
        ResourceGeneration,
        ResourceCost
    }
} 