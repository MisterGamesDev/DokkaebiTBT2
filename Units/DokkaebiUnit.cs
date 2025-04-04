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

namespace Dokkaebi.Units
{
    public class StatusEffectInstance : IStatusEffectInstance
    {
        public StatusEffectData Effect { get; set; }
        public int RemainingDuration { get; set; }
        public int SourceUnitId { get; set; }
        
        // Properties required by UnitStatusUI
        public StatusEffectType effectType => Effect?.effectType ?? StatusEffectType.None;
        public StatusEffectData effectData => Effect;
        public bool isPermanent => Effect?.isPermanent ?? false;
        public int remainingDuration => RemainingDuration;
        public int stacks { get; set; } = 1;
        
        // IStatusEffectInstance implementation
        public StatusEffectType StatusEffectType => Effect?.effectType ?? StatusEffectType.None;
        public int Duration => Effect?.duration ?? 0;
        public int RemainingTurns => RemainingDuration;
    }

    /// <summary>
    /// Base class for all Dokkaebi unit types, enhanced with pathfinding integration
    /// </summary>
    [RequireComponent(typeof(DokkaebiMovementHandler))]
    public class DokkaebiUnit : MonoBehaviour, IDokkaebiUnit
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
        private int currentAura = 0;
        private float currentMP = 0f;
        
        // Unit properties
        private GridPosition gridPosition;
        
        // Component references
        private DokkaebiMovementHandler movementHandler;
        
        // Turn-based action tracking
        private bool hasPendingMovement = false;
        private GridPosition targetPosition;
        private bool isMoving = false;
        private bool hasPendingAbility = false;
        private int pendingAbilityIndex = -1;
        private GridPosition pendingAbilityTarget;
        
        // Interaction state
        private bool isInteractable = true;
        
        // Movement Points
        [SerializeField] private int maxMP = 4;
        
        // Events
        public event Action<int, DamageType> OnDamageTaken;
        public event Action<int> OnHealingReceived;
        public event Action OnUnitDefeated;
        public event Action<StatusEffectData> OnStatusEffectApplied;
        public event Action<StatusEffectData> OnStatusEffectRemoved;
        
        // State tracking
        private bool isDefeated = false;

        // Ability cooldowns
        private Dictionary<string, int> abilityCooldowns = new Dictionary<string, int>();

        // Unit data
        private OriginData origin;
        private CallingData calling;
        private List<AbilityData> abilities = new List<AbilityData>();
        private List<StatusEffectInstance> statusEffects = new List<StatusEffectInstance>();

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
        public int MaxHealth => maxHealth;
        public GridPosition CurrentPosition => gridPosition;

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
        #endregion

        

        private void Awake()
        {
            // Initialize health
            currentHealth = maxHealth;
            
            // Get components
            movementHandler = GetComponent<DokkaebiMovementHandler>();
            
            // Ensure we have a DokkaebiMovementHandler
            if (movementHandler == null)
            {
                movementHandler = gameObject.AddComponent<DokkaebiMovementHandler>();
            }
        }
        
        private void Start()
        {
            // Register with movement handler events
            if (movementHandler != null)
            {
                movementHandler.OnPositionChanged += UpdateGridPosition;
            }
            
            // Set initial grid position based on world position
            gridPosition = GridPositionConverter.WorldToGrid(transform.position);
            
            // Register with GridManager
            if (GridManager.Instance != null)
            {
                GridManager.Instance.SetTileOccupant(gridPosition, this);
            }
        }
        
        private void OnDestroy()
        {
            // Unregister from movement handler events
            if (movementHandler != null)
            {
                movementHandler.OnPositionChanged -= UpdateGridPosition;
            }
            
            // Unregister from GridManager
            if (GridManager.Instance != null)
            {
                GridManager.Instance.ClearUnitFromPreviousTile(this);
            }
        }

        #region Configuration Methods
        // Inside DokkaebiUnit.cs
public void SetUnitId(int id) {
    unitId = id;
    Debug.Log($"SetUnitId called for {gameObject.name}. New ID: {unitId}"); // Add this line
    Debug.Log($"[DokkaebiUnit] SetUnitId called for {gameObject.name} (InstanceID: {gameObject.GetInstanceID()}). New ID: {id}");
unitId = id;
}
        public void SetUnitName(string name) => unitName = name;
        public void SetIsPlayerUnit(bool isPlayer) => isPlayerUnit = isPlayer;
        public void SetMovementRange(int range) => movementRange = range;
        public void SetMaxHealth(int max) => maxHealth = max;
        public void SetCurrentHealth(int current) => currentHealth = current;
        public void SetMaxAura(int max) => maxAura = max;
        public void SetCurrentAura(int current) => currentAura = current;
        #endregion
        
        // Public API
        public string GetUnitName() => unitName;
        public bool IsPlayer() => isPlayerUnit;
        public int GetUnitId() => unitId;
        public int GetMovementRange() => movementRange;
        public int GetCurrentHealth() => currentHealth;
        public int GetMaxHealth() => maxHealth;
        public int GetCurrentAura() => currentAura;
        public int GetMaxAura() => maxAura;
        public float GetCurrentMP() => currentMP;
        public int GetMaxMP() => maxMP;
        
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
            
            // Update world position
            if (movementHandler != null)
            {
                movementHandler.RequestPath(position);
            }
            else
            {
                transform.position = GridManager.Instance.GridToWorld(position);
            }
            
            // Update grid manager
            if (GridManager.Instance != null)
            {
                GridManager.Instance.ClearUnitFromPreviousTile(this);
                GridManager.Instance.SetTileOccupant(position, this);
            }
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
        /// Modify the unit's health by the specified amount. Positive values heal, negative values damage.
        /// </summary>
        public void ModifyHealth(int amount, DamageType damageType = DamageType.Normal)
        {
            if (isDefeated) return;

            int oldHealth = currentHealth;
            currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
            
            if (amount < 0)
            {
                OnDamageTaken?.Invoke(-amount, damageType);
            }
            else if (amount > 0)
            {
                OnHealingReceived?.Invoke(amount);
            }
            
            // Check if unit was defeated
            if (oldHealth > 0 && currentHealth <= 0)
            {
                isDefeated = true;
                OnUnitDefeated?.Invoke();
            }
        }

        /// <summary>
        /// Apply a status effect to the unit for the specified duration
        /// </summary>
        public void ApplyStatusEffect(StatusEffectData effect, int duration)
        {
            if (isDefeated || effect == null) return;
            
            // Check if the effect already exists
            var existingEffect = statusEffects.FirstOrDefault(e => e.Effect?.effectType == effect.effectType);
            
            if (existingEffect != null)
            {
                // Update duration if the new duration is longer
                if (duration > existingEffect.RemainingDuration)
                {
                    existingEffect.RemainingDuration = duration;
                }
                existingEffect.stacks = Mathf.Min(existingEffect.stacks + 1, effect.maxStacks);
            }
            else
            {
                // Create new status effect instance
                var newEffect = new StatusEffectInstance
                {
                    Effect = effect,
                    RemainingDuration = duration,
                    stacks = 1
                };
                statusEffects.Add(newEffect);
            }
            
            OnStatusEffectApplied?.Invoke(effect);
            SmartLogger.Log($"{unitName} received status effect {effect.displayName} for {duration} turns", LogCategory.Unit);
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
        public List<StatusEffectInstance> GetStatusEffects() => statusEffects;

        // Movement and action state
        public void ResetMP() => currentMP = maxMP;
        public void ProcessStatusEffects()
        {
            for (int i = statusEffects.Count - 1; i >= 0; i--)
            {
                var effect = statusEffects[i];
                effect.RemainingDuration--;
                if (effect.RemainingDuration <= 0)
                {
                    OnStatusEffectRemoved?.Invoke(effect.Effect);
                    statusEffects.RemoveAt(i);
                }
            }
        }

        public void EndTurn()
        {
            ProcessStatusEffects();
            UpdateCooldowns();
            ResetActionState();
            ResetAbilityState();
        }

        public void ResetActionState()
        {
            hasPendingMovement = false;
            targetPosition = default;
            isMoving = false;
        }

        public void ResetAbilityState()
        {
            hasPendingAbility = false;
            pendingAbilityIndex = -1;
            pendingAbilityTarget = default;
        }

        public void SetInteractable(bool interactable) => isInteractable = interactable;

        public bool IsReady() => isInteractable && !isMoving && !hasPendingMovement && !hasPendingAbility;

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

        public bool HasPendingAbility() => hasPendingAbility;

        public void PlanAbilityUse(int abilityIndex, GridPosition target)
        {
            if (abilityIndex >= 0 && abilityIndex < abilities.Count)
            {
                hasPendingAbility = true;
                pendingAbilityIndex = abilityIndex;
                pendingAbilityTarget = target;
            }
        }

        public bool HasPendingMovement() => hasPendingMovement;

        public List<GridPosition> GetValidMovePositions()
        {
            var validPositions = new List<GridPosition>();
            var currentPos = GetGridPosition();
            
            for (int x = -MovementRange; x <= MovementRange; x++)
            {
                for (int y = -MovementRange; y <= MovementRange; y++)
                {
                    var testPos = new GridPosition(currentPos.x + x, currentPos.z + y);
                    if (GridManager.Instance != null && 
                        GridManager.Instance.IsPositionValid(testPos) && 
                        GridManager.Instance.IsWalkable(testPos))
                    {
                        validPositions.Add(testPos);
                    }
                }
            }
            
            return validPositions;
        }

        public void SetTargetPosition(GridPosition target)
        {
            if (!hasPendingMovement)
            {
                // Validate target position
                var validMoves = GetValidMovePositions();
                if (!validMoves.Contains(target))
                {
                    SmartLogger.LogWarning($"Invalid move position {target} for unit {GetUnitName()}", LogCategory.Movement);
                    return;
                }

                hasPendingMovement = true;
                targetPosition = target;

                // Trigger movement handler
                if (movementHandler != null)
                {
                    movementHandler.RequestPath(target);
                }
                else
                {
                    SmartLogger.LogError($"No movement handler found for unit {GetUnitName()}", LogCategory.Movement);
                }
            }
        }

        public void ModifyAura(int amount)
        {
            currentAura = Mathf.Clamp(currentAura + amount, 0, maxAura);
        }

        private bool IsValidMove(GridPosition targetPosition, GridManager gridManager)
        {
            // Check if the position is within grid bounds
            if (!gridManager.IsPositionValid(targetPosition))
            {
                return false;
            }

            // Check if the position is walkable
            if (!gridManager.IsWalkable(targetPosition))
            {
                return false;
            }

            // Calculate Manhattan distance
            int distance = Mathf.Abs(targetPosition.x - CurrentGridPosition.x) + 
                         Mathf.Abs(targetPosition.z - CurrentGridPosition.z);

            // Check if the move is within range
            return distance <= MovementRange;
        }
    }
}