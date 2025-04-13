using UnityEngine;
using Dokkaebi.Grid;
using Dokkaebi.Pathfinding;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System;
using Dokkaebi.Interfaces;
using Dokkaebi.Utilities;
using Dokkaebi.Core.Data;
using Dokkaebi.Common;
using Dokkaebi.Core;
using Dokkaebi.Units;
using System.Text;

namespace Dokkaebi.Units
{
    /// <summary>
    /// Base class for all Dokkaebi unit types, enhanced with pathfinding integration
    /// </summary>
    [RequireComponent(typeof(DokkaebiMovementHandler))]
    [RequireComponent(typeof(BoxCollider))] // Ensure there's always a collider for raycasting
    public class DokkaebiUnit : MonoBehaviour, IDokkaebiUnit, IUnitEventHandler, IUnit
    {
        [Header("Unit Properties")]
        private string unitName = "Dokkaebi";
        private bool isPlayerUnit = true;
        private int unitId = -1;
        private int movementRange = 3;
        private int teamId = 0;
        
        [Header("Stats")]
        private int maxHealth = 100;
        private int currentHealth;
        private int maxAura = 10;
        private float currentMP = 0f;
        
        // Add unit-specific Aura fields
        [Header("Unit Aura")]
        [SerializeField] private int currentUnitAura;
        [SerializeField] private int maxUnitAura;
        
        // Unit properties
        private GridPosition gridPosition;
        
        // Component references
        private DokkaebiMovementHandler movementHandler;
        
        // Turn-based action tracking
        private bool hasPendingMovement = false;
        public bool HasPendingMovementFlag => hasPendingMovement; // Public property for external state checks
        private GridPosition? targetPosition;
        private bool hasMovedThisTurn = false;
        
        // Interaction state
        private bool isInteractable = true;
        
        // Movement Points
        [SerializeField] private int maxMP = 4;
        
        // Events
        public event Action<int, DamageType> OnDamageTaken;
        public event Action<int> OnHealingReceived;
        public event Action OnUnitDefeated;
        public event Action<IStatusEffectInstance> OnStatusEffectApplied;
        public event Action<IStatusEffectInstance> OnStatusEffectRemoved;
        public event Action<IDokkaebiUnit, GridPosition, GridPosition> OnUnitMoved;
        
        // Add unit-specific Aura event
        public event Action<int, int> OnUnitAuraChanged; // (oldAura, newAura)
        
        // State tracking
        private bool isDefeated = false;

        // Ability cooldowns
        private Dictionary<string, int> abilityCooldowns = new Dictionary<string, int>();

        // Unit data
        private OriginData origin;
        private CallingData calling;
        private List<AbilityData> abilities = new List<AbilityData>();
        private List<IStatusEffectInstance> statusEffects = new List<IStatusEffectInstance>();

        #region IDokkaebiUnit Implementation
        public int UnitId => unitId;
        public GridPosition CurrentGridPosition => gridPosition;
        public bool IsAlive => !isDefeated;
        public int TeamId => teamId;
        public GameObject GameObject => gameObject;
        public bool IsPlayerControlled => isPlayerUnit;
        public int MovementRange => movementRange;
        public int CurrentHealth => currentHealth;
        public string UnitName => unitName;
        public string DisplayName => unitName;

        public List<IStatusEffectInstance> GetStatusEffects()
        {
            return statusEffects;
        }

        public void MoveToGridPosition(GridPosition newPosition)
        {
            if (movementHandler != null)
            {
                movementHandler.RequestPath(newPosition);
            }
            else
            {
                SetGridPosition(newPosition);
            }
        }

        public void AddStatusEffect(IStatusEffectInstance effect)
        {
            if (effect == null) return;
            statusEffects.Add(effect);
            RaiseStatusEffectApplied(effect);
        }

        public void RemoveStatusEffect(IStatusEffectInstance effect)
        {
            if (effect == null) return;
            if (statusEffects.Remove(effect))
            {
                RaiseStatusEffectRemoved(effect);
            }
        }

        public bool HasStatusEffect(StatusEffectType effectType)
        {
            return statusEffects.Any(effect => effect.StatusEffectType == effectType);
        }
        #endregion

        #region IUnit Implementation
        public int MaxHealth => maxHealth;
        public GridPosition CurrentPosition => gridPosition;

        public void ModifyHealth(int amount, DamageType damageType = DamageType.Normal)
        {
            if (amount < 0)
            {
                TakeDamage(-amount, damageType);
            }
            else if (amount > 0)
            {
                Heal(amount);
            }
        }
        #endregion

        private void Awake()
        {
            SmartLogger.Log($"[DokkaebiUnit] Awake called on {gameObject.name}", LogCategory.Unit, this);
            
            // Ensure proper layer setup
            if (gameObject.layer != LayerMask.NameToLayer("Unit"))
            {
                gameObject.layer = LayerMask.NameToLayer("Unit");
                SmartLogger.Log($"[DokkaebiUnit] Set layer to Unit for {gameObject.name}", LogCategory.Unit, this);
            }
            
            // Validate collider setup
            var collider = GetComponent<Collider>();
            if (collider == null)
            {
                SmartLogger.LogError($"[DokkaebiUnit] No Collider found on {gameObject.name}. Adding BoxCollider.", LogCategory.Unit, this);
                collider = gameObject.AddComponent<BoxCollider>();
            }
            
            // Ensure collider is enabled
            if (!collider.enabled)
            {
                collider.enabled = true;
                SmartLogger.Log($"[DokkaebiUnit] Enabled collider on {gameObject.name}", LogCategory.Unit, this);
            }
            
            // Initialize health
            currentHealth = maxHealth;
            SmartLogger.Log($"[DokkaebiUnit] Initialized health - Max: {maxHealth}, Current: {currentHealth}", LogCategory.Unit, this);
            
            // Initialize unit Aura
            SmartLogger.Log($"[DokkaebiUnit] Initialized unit Aura - Max: {maxUnitAura}, Current: {currentUnitAura}", LogCategory.Unit, this);
            
            // Get or add movement handler
            movementHandler = GetComponent<DokkaebiMovementHandler>();
            if (movementHandler == null)
            {
                movementHandler = gameObject.AddComponent<DokkaebiMovementHandler>();
                SmartLogger.Log("[DokkaebiUnit] Added DokkaebiMovementHandler component", LogCategory.Unit, this);
            }
        }
        
        private void Start()
        {
            SmartLogger.Log($"[DokkaebiUnit] Start called on {gameObject.name}", LogCategory.Unit, this);
            
            // Register with grid manager
            if (GridManager.Instance != null)
            {
                GridManager.Instance.SetTileOccupant(gridPosition, this);
                SmartLogger.Log($"[DokkaebiUnit] Registered with GridManager at position {gridPosition}", LogCategory.Unit, this);
            }
        }
        
        private void OnDestroy()
        {
            SmartLogger.Log($"[DokkaebiUnit.OnDestroy] Unit {unitName} (ID: {unitId}) is being destroyed", LogCategory.Unit, this);
            
            // Clear from GridManager
            if (GridManager.Instance != null)
            {
                GridManager.Instance.ClearUnitFromPreviousTile(this);
                SmartLogger.Log($"[DokkaebiUnit.OnDestroy] Clearing unit from previous tile in GridManager for unit {unitName}", LogCategory.Unit, this);
            }
        }

        private void OnDisable()
        {
            SmartLogger.Log($"[DokkaebiUnit.OnDisable] Unit {unitName} (ID: {unitId}) is being disabled", LogCategory.Unit, this);
        }

        private void OnEnable()
        {
            SmartLogger.Log($"[DokkaebiUnit.OnEnable] Unit {unitName} (ID: {unitId}) is being enabled", LogCategory.Unit, this);
        }

        #region Configuration Methods
        public void SetUnitId(int id) {
            unitId = id;
            SmartLogger.Log($"[DokkaebiUnit] SetUnitId called for {gameObject.name} (InstanceID: {gameObject.GetInstanceID()}). New ID: {id}", LogCategory.Unit, this);
        }
        public void SetUnitName(string name) => unitName = name;
        public void SetIsPlayerUnit(bool isPlayer) => isPlayerUnit = isPlayer;
        public void SetMovementRange(int range) => movementRange = range;
        public void SetMaxHealth(int max) => maxHealth = max;
        public void SetCurrentHealth(int current) => currentHealth = current;
        public void SetMaxAura(int max) => maxAura = max;

        // Add unit-specific Aura configuration methods
        public void SetMaxUnitAura(int max)
        {
            maxUnitAura = max;
            // Clamp current unit aura if it exceeds new max
            if (currentUnitAura > maxUnitAura)
            {
                ModifyUnitAura(maxUnitAura - currentUnitAura);
            }
        }

        public void SetCurrentUnitAura(int current)
        {
            int oldAura = currentUnitAura;
            currentUnitAura = Mathf.Clamp(current, 0, maxUnitAura);
            if (oldAura != currentUnitAura)
            {
                OnUnitAuraChanged?.Invoke(oldAura, currentUnitAura);
            }
        }
        #endregion
        
        // Public API
        public string GetUnitName() => unitName;
        public string GetDisplayName() => unitName ?? "Unknown";
        public bool IsPlayer() => isPlayerUnit;
        public int GetUnitId() => unitId;
        public int GetMovementRange() => movementRange;
        public int GetCurrentHealth() => currentHealth;
        public int GetMaxHealth() => maxHealth;
        public int GetCurrentAura() => AuraManager.Instance.GetCurrentAura(isPlayerUnit);
        public int GetMaxAura() => maxAura;
        public float GetCurrentMP() => currentMP;
        public int GetMaxMP() => maxMP;

        // Add unit-specific Aura getters
        public int GetCurrentUnitAura() => currentUnitAura;
        public int GetMaxUnitAura() => maxUnitAura;
        
        public GridPosition GetGridPosition() => gridPosition;
        
        /// <summary>
        /// Set the unit's grid position and update its world position
        /// </summary>
        public void SetGridPosition(GridPosition position)
        {
            if (position == gridPosition) return;
            
            // Update grid position
            var oldPosition = gridPosition;
            gridPosition = position;
            
            // TEMPORARILY DISABLED FOR DIAGNOSIS: Update world position
            // if (movementHandler != null)
            // {
            //     transform.position = GridManager.Instance.GridToWorld(position);
            // }
            // else
            // {
            //     transform.position = GridManager.Instance.GridToWorld(position);
            // }
            
            // --- ADD DIAGNOSTIC LOG ---
            SmartLogger.Log($"[SetGridPosition DIAGNOSTIC] Unit '{unitName}' internal gridPos set to {gridPosition}. Transform update SKIPPED.", LogCategory.Unit);
            // --- END DIAGNOSTIC LOG ---
            
            // Update grid manager
            if (GridManager.Instance != null)
            {
                GridManager.Instance.ClearUnitFromPreviousTile(this);
                GridManager.Instance.SetTileOccupant(position, this);
            }

            // Fire the OnUnitMoved event
            OnUnitMoved?.Invoke(this, oldPosition, position);
        }

        

        /// <summary>
        /// Update the unit's grid position based on its current world position
        /// </summary>
        public void UpdateGridPosition(GridPosition newPosition)
        {
            if (newPosition == gridPosition) return;
            
            // Update grid position
            var oldPosition = gridPosition;
            gridPosition = newPosition;
            
            // Update grid manager
            if (GridManager.Instance != null)
            {
                GridManager.Instance.ClearUnitFromPreviousTile(this);
                GridManager.Instance.SetTileOccupant(newPosition, this);
            }
        }

        /// <summary>
        /// Take damage from any source
        /// </summary>
        public void TakeDamage(int amount, DamageType damageType)
        {
            if (amount <= 0)
            {
                SmartLogger.Log($"[DokkaebiUnit.TakeDamage] Ignoring non-positive damage amount: {amount}", LogCategory.Ability);
                return;
            }

            SmartLogger.Log($"[DokkaebiUnit.TakeDamage START] Unit: {unitName}, Base Damage: {amount}, Type: {damageType}, Current Health: {currentHealth}/{maxHealth}", LogCategory.Ability);

            // Get and log all active status effects that might modify damage
            var activeEffects = GetStatusEffects();
            if (activeEffects.Any())
            {
                SmartLogger.Log($"[DokkaebiUnit.TakeDamage] Active status effects: {string.Join(", ", activeEffects.Select(e => e.StatusEffectType.ToString()))}", LogCategory.Ability);
            }

            // Apply damage modifiers based on status effects
            float damageMultiplier = StatusEffectSystem.GetStatModifier(this, UnitAttributeType.DamageTaken);
            SmartLogger.Log($"[DokkaebiUnit.TakeDamage] Status effect damage multiplier: {damageMultiplier:F2}", LogCategory.Ability);

            // Calculate modified damage
            int modifiedDamage = Mathf.RoundToInt(amount * damageMultiplier);
            SmartLogger.Log($"[DokkaebiUnit.TakeDamage] Final damage after modifiers: {modifiedDamage} (Base: {amount} x Multiplier: {damageMultiplier:F2})", LogCategory.Ability);

            // Apply damage
            int oldHealth = currentHealth;
            currentHealth = Mathf.Max(0, currentHealth - modifiedDamage);
            int actualDamage = oldHealth - currentHealth;

            // Log the actual damage taken and new health state
            SmartLogger.Log($"[DokkaebiUnit.TakeDamage] Actual damage taken: {actualDamage}, Health reduced from {oldHealth} to {currentHealth}", LogCategory.Ability);

            // Invoke damage taken event
            OnDamageTaken?.Invoke(modifiedDamage, damageType);
            SmartLogger.Log($"[DokkaebiUnit.TakeDamage] OnDamageTaken event invoked with damage: {modifiedDamage}, type: {damageType}", LogCategory.Ability);

            // Check for defeat
            if (currentHealth <= 0 && !isDefeated)
            {
                isDefeated = true;
                SmartLogger.Log($"[DokkaebiUnit.TakeDamage] Unit {unitName} has been defeated!", LogCategory.Ability);
                OnUnitDefeated?.Invoke();
            }

            SmartLogger.Log($"[DokkaebiUnit.TakeDamage END] Unit: {unitName}, Final Health: {currentHealth}/{maxHealth}, Defeated: {isDefeated}", LogCategory.Ability);
        }

        /// <summary>
        /// Heal the unit
        /// </summary>
        public void Heal(int amount)
        {
            if (amount <= 0) return;

            // Apply healing modifiers based on status effects
            float healingMultiplier = StatusEffectSystem.GetStatModifier(this, UnitAttributeType.HealingReceived);
            int modifiedHealing = Mathf.RoundToInt(amount * healingMultiplier);

            // Apply healing
            int oldHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + modifiedHealing);
            int actualHealing = currentHealth - oldHealth;

            // Invoke healing received event
            OnHealingReceived?.Invoke(actualHealing);

            SmartLogger.Log($"{unitName} healed for {actualHealing} HP (base: {amount}, multiplier: {healingMultiplier})", Dokkaebi.Utilities.LogCategory.General);
        }

        /// <summary>
        /// Apply a status effect to the unit for the specified duration
        /// </summary>
        public void ApplyStatusEffect(StatusEffectData effect, int duration)
        {
            StatusEffectSystem.ApplyStatusEffect(this, effect, duration);
        }

        /// <summary>
        /// Reduce all ability cooldowns by 1 at the start of a new turn
        /// </summary>
        public void ReduceCooldowns()
        {
            var cooldownKeys = abilityCooldowns.Keys.ToList();
            foreach (var abilityId in cooldownKeys)
            {
                if (abilityCooldowns[abilityId] > 0)
                {
                    abilityCooldowns[abilityId]--;
                    if (abilityCooldowns[abilityId] <= 0)
                    {
                        abilityCooldowns.Remove(abilityId);
                    }
                }
            }
        }

        /// <summary>
        /// Set a cooldown for a specific ability
        /// </summary>
        public void SetAbilityCooldown(string abilityId, int cooldown)
        {
            if (cooldown <= 0)
            {
                abilityCooldowns.Remove(abilityId);
            }
            else
            {
                abilityCooldowns[abilityId] = cooldown;
            }
        }

        /// <summary>
        /// Set a cooldown for an ability by its type
        /// </summary>
        public void SetAbilityCooldown(AbilityType abilityType, int cooldown)
        {
            var ability = abilities.FirstOrDefault(a => a.abilityType == abilityType);
            if (ability != null)
            {
                SetAbilityCooldown(ability.abilityId, cooldown);
            }
        }

        /// <summary>
        /// Get the remaining cooldown for a specific ability
        /// </summary>
        public int GetAbilityCooldown(string abilityId)
        {
            return abilityCooldowns.TryGetValue(abilityId, out int cooldown) ? cooldown : 0;
        }

        // Public API for unit data
        public void SetOrigin(OriginData origin) => this.origin = origin;
        public void SetCalling(CallingData calling) => this.calling = calling;
        public void SetAbilities(List<AbilityData> abilities) => this.abilities = abilities ?? new List<AbilityData>();
        public OriginData GetOrigin() => origin;
        public CallingData GetCalling() => calling;
        public List<AbilityData> GetAbilities() => abilities;

        // Movement and action state
        public void ResetMP() => currentMP = maxMP;
        public void EndTurn()
        {
            SmartLogger.Log($"[DokkaebiUnit.EndTurn] Unit {unitName} (ID: {unitId}) - Starting EndTurn", LogCategory.Unit, this);
            ClearPendingMovement();
            hasMovedThisTurn = false;
            SmartLogger.Log($"[DokkaebiUnit.EndTurn] Unit {unitName} (ID: {unitId}) - Completed EndTurn. Final hasPendingMovement: {hasPendingMovement}", LogCategory.Unit, this);
        }

        public void ResetActionState()
        {
            SmartLogger.Log($"[DokkaebiUnit.ResetActionState] Unit {unitName} (ID: {unitId}) - Setting hasPendingMovement=false. Previous value: {hasPendingMovement}", LogCategory.Unit, this);
            hasPendingMovement = false;
            hasMovedThisTurn = false;
            SmartLogger.Log($"[DokkaebiUnit.ResetActionState] Unit {unitName} (ID: {unitId}) - hasPendingMovement SET TO: {hasPendingMovement}, hasMovedThisTurn SET TO: {hasMovedThisTurn}", LogCategory.Unit, this);
        }

        public void SetInteractable(bool interactable) => isInteractable = interactable;

        public bool IsReady() => isInteractable && !hasPendingMovement;

        public void UpdateCooldowns()
        {
            var keys = new List<string>(abilityCooldowns.Keys);
            foreach (var key in keys)
            {
                if (abilityCooldowns[key] > 0)
                {
                    abilityCooldowns[key]--;
                    if (abilityCooldowns[key] <= 0)
                    {
                        abilityCooldowns.Remove(key);
                    }
                }
            }
        }

        public int GetRemainingCooldown(AbilityType type)
        {
            var ability = abilities.FirstOrDefault(a => a.abilityType == type);
            if (ability != null && abilityCooldowns.TryGetValue(ability.abilityId, out int cooldown))
            {
                return cooldown;
            }
            return 0;
        }

        public bool IsOnCooldown(AbilityType type)
        {
            return GetRemainingCooldown(type) > 0;
        }

        public bool HasPendingMovement() => hasPendingMovement;

        public List<GridPosition> GetValidMovePositions()
        {
            if (movementHandler == null)
            {
                SmartLogger.LogError($"[DUnit.GetValidMoves] MovementHandler reference is null for unit {UnitId}! Cannot get valid moves.", LogCategory.Unit, this);
                return new List<GridPosition>(); // Return empty list if handler is missing
            }
            // Call the movement handler's method which performs the BFS/pathfinding based check
            SmartLogger.Log($"[DUnit.GetValidMoves] Unit {UnitId} delegating to MovementHandler.GetValidMovePositions().", LogCategory.Unit, this);
            List<GridPosition> validPositions = movementHandler.GetValidMovePositions(); // Assuming this method exists on the handler

            // Log the result received from the handler
            using (var sb = new StringBuilderScope(out StringBuilder builder))
            {
                builder.AppendLine($"[DUnit.GetValidMoves] Received {validPositions.Count} positions from MovementHandler:");
                foreach(var pos in validPositions) { builder.AppendLine($"- {pos}"); }
                SmartLogger.LogWithBuilder(builder, LogCategory.Unit, this);
            }

            return validPositions;
        }

        public void SetTargetPosition(GridPosition targetPos)
        {
            SmartLogger.Log($"[DokkaebiUnit.SetTargetPosition] Unit {unitName} (ID: {unitId}) - Setting hasPendingMovement=true. Previous value: {hasPendingMovement}", LogCategory.Unit, this);
            targetPosition = targetPos;
            hasPendingMovement = true;
            SmartLogger.Log($"[DokkaebiUnit.SetTargetPosition] Unit {unitName} (ID: {unitId}) - hasPendingMovement SET TO: {hasPendingMovement}", LogCategory.Unit, this);
        }

        public void ClearPendingMovement()
        {
            SmartLogger.Log($"[DokkaebiUnit.ClearPendingMovement] Unit {unitName} (ID: {unitId}) - Setting hasPendingMovement=false. Previous value: {hasPendingMovement}", LogCategory.Unit, this);
            hasPendingMovement = false;
            targetPosition = null;
            SmartLogger.Log($"[DokkaebiUnit.ClearPendingMovement] Unit {unitName} (ID: {unitId}) - hasPendingMovement SET TO: {hasPendingMovement}", LogCategory.Unit, this);
        }

        public GridPosition GetPendingTargetPosition()
        {
            if (hasPendingMovement && targetPosition.HasValue)
            {
                return targetPosition.Value;
            }
            
            return gridPosition;
        }

        public GridPosition GetCurrentGridPosition()
        {
            if (GridManager.Instance == null)
            {
                SmartLogger.LogError("[DokkaebiUnit] GridManager not found when getting current grid position", LogCategory.Unit, this);
                return new GridPosition(-1, -1);
            }

            // Convert world position to grid position
            return GridManager.Instance.WorldToGrid(transform.position);
        }

        public bool CanMove()
        {
            SmartLogger.Log($"[DokkaebiUnit.CanMove] Checking {unitName} (ID: {unitId}). hasMovedThisTurn: {hasMovedThisTurn}, hasPendingMovement: {hasPendingMovement}", LogCategory.Unit, this);
            return !hasMovedThisTurn && !hasPendingMovement;
        }

        public bool HasMovedThisTurn => hasMovedThisTurn;

        public void SetHasMoved(bool moved)
        {
            hasMovedThisTurn = moved;
            SmartLogger.Log($"[DokkaebiUnit.SetHasMoved] Unit {UnitId} hasMovedThisTurn set to {moved}", LogCategory.Unit, this);
        }

        public void ModifyAura(int amount)
        {
            AuraManager.Instance.ModifyAura(isPlayerUnit, amount);
        }

        /// <summary>
        /// Modifies the unit's own Aura pool, clamps the value, and invokes the OnUnitAuraChanged event.
        /// </summary>
        /// <param name="amount">The amount to change Aura by (positive to add, negative to subtract)</param>
        /// <returns>The actual amount of Aura modified</returns>
        public int ModifyUnitAura(int amount)
        {
            if (!IsAlive) return 0;

            int oldAura = currentUnitAura;
            currentUnitAura = Mathf.Clamp(currentUnitAura + amount, 0, maxUnitAura);
            int actualChange = currentUnitAura - oldAura;

            if (actualChange != 0)
            {
                SmartLogger.Log($"[{unitName}] Unit Aura changed from {oldAura} to {currentUnitAura} (Amount: {amount}, Actual: {actualChange})", LogCategory.Ability);
                OnUnitAuraChanged?.Invoke(oldAura, currentUnitAura);
            }

            return actualChange;
        }

        /// <summary>
        /// Checks if the unit has enough unit-specific Aura for a cost
        /// </summary>
        /// <param name="cost">The Aura cost to check</param>
        /// <returns>True if the unit has enough Aura, false otherwise</returns>
        public bool HasEnoughUnitAura(int cost)
        {
            return currentUnitAura >= cost;
        }

        /// <summary>
        /// Resets the unit's Aura to its maximum value
        /// </summary>
        public void ResetUnitAura()
        {
            if (currentUnitAura != maxUnitAura)
            {
                ModifyUnitAura(maxUnitAura - currentUnitAura);
            }
        }

        /// <summary>
        /// Sets the unit's current aura directly, typically from authoritative state.
        /// </summary>
        /// <param name="targetAmount">The desired current aura amount.</param>
        public void SetCurrentAura(int targetAmount)
        {
            // Ensure AuraManager is available
            if (AuraManager.Instance == null)
            {
                SmartLogger.LogError($"Cannot SetCurrentAura for {GetUnitName()}: AuraManager instance is null.", LogCategory.Ability);
                return;
            }

            int currentAura = GetCurrentAura(); // Uses the existing getter that likely calls AuraManager
            int difference = targetAmount - currentAura;

            // Use the existing ModifyAura method which handles clamping and events via AuraManager
            ModifyAura(difference);

            // Optional: Add a log to confirm
            SmartLogger.Log($"{GetUnitName()} current Aura set to {targetAmount} (from state sync). Previous: {currentAura}, Diff: {difference}", LogCategory.StateSync);
        }

        /// <summary>
        /// Internally invokes the OnStatusEffectApplied event.
        /// Called by external systems after an effect is successfully added.
        /// </summary>
        public void RaiseStatusEffectApplied(IStatusEffectInstance instance)
        {
            if (instance is StatusEffectInstance effectInstance)
            {
                SmartLogger.Log($"Status Effect {effectInstance.effectData.displayName} applied to {DisplayName}", Dokkaebi.Utilities.LogCategory.General);
                OnStatusEffectApplied?.Invoke(instance);
            }
        }

        /// <summary>
        /// Internally invokes the OnStatusEffectRemoved event.
        /// Called by external systems after an effect is successfully removed.
        /// </summary>
        public void RaiseStatusEffectRemoved(IStatusEffectInstance instance)
        {
            if (instance is StatusEffectInstance effectInstance)
            {
                SmartLogger.Log($"Status Effect {effectInstance.effectData.displayName} removed from {DisplayName}", Dokkaebi.Utilities.LogCategory.General);
                OnStatusEffectRemoved?.Invoke(instance);
            }
        }

        public void SetPendingMovement(bool isPending)
        {
            SmartLogger.Log($"[DokkaebiUnit.SetPendingMovement] Unit {unitName} (ID: {unitId}) - Setting hasPendingMovement={isPending}. Previous value: {hasPendingMovement}", LogCategory.Unit, this);
            hasPendingMovement = isPending;
            SmartLogger.Log($"[DokkaebiUnit.SetPendingMovement] Unit {unitName} (ID: {unitId}) - hasPendingMovement SET TO: {hasPendingMovement}", LogCategory.Unit, this);
        }
    }
}
