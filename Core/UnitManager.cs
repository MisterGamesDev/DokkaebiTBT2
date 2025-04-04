using UnityEngine;
using System.Collections.Generic;
using Dokkaebi.Core.Data;
using Dokkaebi.Units;
using Dokkaebi.Grid;
using Dokkaebi.Utilities;
using Dokkaebi.Interfaces;
using System.Linq;

namespace Dokkaebi.Core
{
    /// <summary>
    /// Manages unit spawning, tracking, and lifecycle.
    /// </summary>
    public class UnitManager : MonoBehaviour
    {
        // Singleton instance
        public static UnitManager Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private GameObject baseUnitPrefab;

        [Header("Unit Definitions")]
        [SerializeField] private List<UnitDefinitionData> unitDefinitions;

        // Runtime data
        private Dictionary<int, DokkaebiUnit> activeUnits = new Dictionary<int, DokkaebiUnit>();
        private int nextUnitId = 1;
        private DokkaebiUnit selectedUnit;

        private void Awake()
        {
            // Singleton pattern setup
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Debug.LogWarning("Multiple UnitManager instances detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            if (baseUnitPrefab == null)
            {
                Debug.LogError("Base unit prefab not assigned to UnitManager!");
            }
        }

        /// <summary>
        /// Spawns all units defined in the UnitSpawnData configuration
        /// </summary>
        public void SpawnUnitsFromConfiguration()
        {
            Debug.Log("[UnitManager.SpawnUnitsFromConfiguration] Method Called.");
            var spawnData = DataManager.Instance.GetUnitSpawnData();
            if (spawnData == null) {
                Debug.LogError("[UnitManager.SpawnUnitsFromConfiguration] Failed to get UnitSpawnData from DataManager!");
                return; // Stop if data is null
            }
            Debug.Log($"[UnitManager.SpawnUnitsFromConfiguration] Found UnitSpawnData. Player Spawns: {spawnData.playerUnitSpawns.Count}, Enemy Spawns: {spawnData.enemyUnitSpawns.Count}");

            // Spawn player units
            foreach (var spawnConfig in spawnData.playerUnitSpawns)
            {
                Debug.Log($"[UnitManager.SpawnUnitsFromConfiguration] Processing Player Spawn Config - UnitDef: {(spawnConfig.unitDefinition != null ? spawnConfig.unitDefinition.name : "NULL")}, Pos: {spawnConfig.spawnPosition}");
                var unitDefinition = spawnConfig.unitDefinition;
                if (unitDefinition != null)
                {
                    SpawnUnit(unitDefinition, spawnConfig.spawnPosition, true);
                }
                else
                {
                    Debug.LogError($"Player unit spawn configuration is missing unit definition!");
                }
            }

            // Spawn enemy units
            foreach (var spawnConfig in spawnData.enemyUnitSpawns)
            {
                Debug.Log($"[UnitManager.SpawnUnitsFromConfiguration] Processing Enemy Spawn Config - UnitDef: {(spawnConfig.unitDefinition != null ? spawnConfig.unitDefinition.name : "NULL")}, Pos: {spawnConfig.spawnPosition}");
                var unitDefinition = spawnConfig.unitDefinition;
                if (unitDefinition != null)
                {
                    SpawnUnit(unitDefinition, spawnConfig.spawnPosition, false);
                }
                else
                {
                    Debug.LogError($"Enemy unit spawn configuration is missing unit definition!");
                }
            }

            Debug.Log($"Spawned {spawnData.playerUnitSpawns.Count} player units and {spawnData.enemyUnitSpawns.Count} enemy units");
        }

        /// <summary>
        /// Spawn a unit from a definition
        /// </summary>
        public DokkaebiUnit SpawnUnit(UnitDefinitionData definition, Vector2Int spawnPosition, bool isPlayerUnit)
        {
            return SpawnUnit(definition, GridPosition.FromVector2Int(spawnPosition), isPlayerUnit);
        }

        /// <summary>
        /// Spawn a unit from a definition at a grid position
        /// </summary>
        public DokkaebiUnit SpawnUnit(UnitDefinitionData definition, GridPosition gridPosition, bool isPlayerUnit)
        {
            if (definition == null)
            {
                Debug.LogError($"[UnitManager.SpawnUnit] Cannot spawn unit: definition is null");
                return null;
            }
            
            GameObject chosenPrefab = definition.unitPrefab ?? baseUnitPrefab;
            if (chosenPrefab == null) {
                Debug.LogError($"[UnitManager.SpawnUnit] Prefab is NULL for unit {definition.displayName}. definition.unitPrefab was {(definition.unitPrefab == null ? "null" : "assigned")}, baseUnitPrefab was {(baseUnitPrefab == null ? "null" : "assigned")}. Cannot spawn.");
                return null;
            }
            Debug.Log($"[UnitManager.SpawnUnit] Using prefab: {chosenPrefab.name} for unit {definition.displayName}");

            // Convert grid position to world position
            Vector3 worldPosition = GridManager.Instance.GridToWorld(gridPosition);
            
            // Instantiate the unit
            GameObject unitObject = Instantiate(chosenPrefab, worldPosition, Quaternion.identity);
            if (unitObject == null) {
                Debug.LogError($"[UnitManager.SpawnUnit] Instantiate FAILED for prefab {chosenPrefab.name}");
                return null;
            }
            Debug.Log($"[UnitManager.SpawnUnit] Instantiated GameObject: {unitObject.name}");

            // Get or add the DokkaebiUnit component
            DokkaebiUnit unit = unitObject.GetComponent<DokkaebiUnit>();
            if (unit == null)
            {
                Debug.LogError($"Unit prefab {chosenPrefab.name} is missing DokkaebiUnit component!");
                Destroy(unitObject);
                return null;
            }

            // Configure the unit
            ConfigureUnit(unit, definition, isPlayerUnit);
            
            // Register the unit
            RegisterUnit(unit);

            Vector3 finalWorldPos = unitObject.transform.position; // Get the actual final position
            Debug.Log($"[UnitManager.SpawnUnit] Successfully configured and returning unit: {unit.GetUnitName()} at GridPos {unit.GetGridPosition()} (World: {finalWorldPos})");
            return unit;
        }

        private void ConfigureUnit(DokkaebiUnit unit, UnitDefinitionData definition, bool isPlayerUnit)
        {
            // Set basic properties
            unit.SetUnitName(definition.displayName);
            unit.SetIsPlayerUnit(isPlayerUnit);

            // Set stats
            unit.SetMaxHealth(definition.baseHealth);
            unit.SetCurrentHealth(definition.baseHealth);
            unit.SetMaxAura(definition.baseAura);
            unit.SetCurrentAura(definition.baseAura);
            unit.SetMovementRange(definition.baseMovement);

            // Set identity
            unit.SetOrigin(definition.origin);
            unit.SetCalling(definition.calling);

            // Set abilities
            unit.SetAbilities(definition.abilities);
        }

        private void RegisterUnit(DokkaebiUnit unit)
        {
            int unitId = nextUnitId++;
            unit.SetUnitId(unitId);
            activeUnits.Add(unitId, unit);
        }

        public DokkaebiUnit GetUnitById(int unitId)
        {
            return activeUnits.TryGetValue(unitId, out var unit) ? unit : null;
        }

        public List<DokkaebiUnit> GetUnitsByPlayer(bool isPlayerUnit)
        {
            return new List<DokkaebiUnit>(activeUnits.Values).FindAll(u => u.IsPlayer() == isPlayerUnit);
        }

        public List<DokkaebiUnit> GetUnitsAtPosition(Vector2Int positionVector)
        {
            // Convert Vector2Int to GridPosition using the proper utility
            GridPosition position = Dokkaebi.Interfaces.GridPosition.FromVector2Int(positionVector);
            return new List<DokkaebiUnit>(activeUnits.Values).FindAll(u => u.GetGridPosition() == position);
        }

        /// <summary>
        /// Get a single unit at the specified grid position (returns first found if multiple exist)
        /// </summary>
        public DokkaebiUnit GetUnitAtPosition(GridPosition position)
        {
            return new List<DokkaebiUnit>(activeUnits.Values).FirstOrDefault(u => u.GetGridPosition() == position);
        }

        public void RemoveUnit(DokkaebiUnit unit)
        {
            if (unit != null && activeUnits.ContainsKey(unit.GetUnitId()))
            {
                activeUnits.Remove(unit.GetUnitId());
                Destroy(unit.gameObject);
            }
        }

        public void RemoveAllUnits()
        {
            foreach (var unit in activeUnits.Values)
            {
                if (unit != null)
                {
                    Destroy(unit.gameObject);
                }
            }
            activeUnits.Clear();
            nextUnitId = 1;
        }

        /// <summary>
        /// Unregisters a unit from the manager without destroying it
        /// </summary>
        public void UnregisterUnit(DokkaebiUnit unit)
        {
            if (unit != null && activeUnits.ContainsKey(unit.GetUnitId()))
            {
                activeUnits.Remove(unit.GetUnitId());
                Debug.Log($"Unit {unit.GetUnitName()} unregistered from UnitManager");
            }
        }

        #region Turn Management
        /// <summary>
        /// Apply start of turn effects to all player units
        /// </summary>
        public void StartPlayerTurn()
        {
            var playerUnits = GetUnitsByPlayer(true);
            foreach (var unit in playerUnits)
            {
                unit.ResetMP();
                unit.ReduceCooldowns();
                unit.ProcessStatusEffects();
            }
            
            Debug.Log("Player turn started - processed effects for " + playerUnits.Count + " units");
        }
        
        /// <summary>
        /// Apply start of turn effects to all enemy units
        /// </summary>
        public void StartEnemyTurn()
        {
            var enemyUnits = GetUnitsByPlayer(false);
            foreach (var unit in enemyUnits)
            {
                unit.ResetMP();
                unit.ReduceCooldowns();
                unit.ProcessStatusEffects();
            }
            
            Debug.Log("Enemy turn started - processed effects for " + enemyUnits.Count + " units");
        }
        
        /// <summary>
        /// Apply end of turn effects to all player units
        /// </summary>
        public void EndPlayerTurn()
        {
            var playerUnits = GetUnitsByPlayer(true);
            foreach (var unit in playerUnits)
            {
                unit.EndTurn();
            }
            
            Debug.Log("Player turn ended - processed " + playerUnits.Count + " units");
        }
        
        /// <summary>
        /// Apply end of turn effects to all enemy units
        /// </summary>
        public void EndEnemyTurn()
        {
            var enemyUnits = GetUnitsByPlayer(false);
            foreach (var unit in enemyUnits)
            {
                unit.EndTurn();
            }
            
            Debug.Log("Enemy turn ended - processed " + enemyUnits.Count + " units");
        }
        #endregion
        
        #region Unit State Queries
        /// <summary>
        /// Get all units that are still alive
        /// </summary>
        public List<DokkaebiUnit> GetAliveUnits()
        {
            return new List<DokkaebiUnit>(activeUnits.Values).FindAll(u => u.IsAlive);
        }
        
        /// <summary>
        /// Get all alive units belonging to a specific player
        /// </summary>
        public List<DokkaebiUnit> GetAliveUnitsByPlayer(bool isPlayerUnit)
        {
            return GetUnitsByPlayer(isPlayerUnit).FindAll(u => u.IsAlive);
        }
        
        /// <summary>
        /// Get all units within a certain range of a position
        /// </summary>
        public List<DokkaebiUnit> GetUnitsInRange(GridPosition center, int range)
        {
            List<DokkaebiUnit> unitsInRange = new List<DokkaebiUnit>();
            
            foreach (var unit in activeUnits.Values)
            {
                if (unit.IsAlive)
                {
                    int distance = GridPosition.GetManhattanDistance(center, unit.GetGridPosition());
                    if (distance <= range)
                    {
                        unitsInRange.Add(unit);
                    }
                }
            }
            
            return unitsInRange;
        }
        
        /// <summary>
        /// Check if any unit has movement points left
        /// </summary>
        public bool AnyUnitsHaveRemainingMP(bool isPlayerUnit)
        {
            var units = GetAliveUnitsByPlayer(isPlayerUnit);
            foreach (var unit in units)
            {
                if (unit.GetCurrentMP() > 0)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Unit State Management
        
        /// <summary>
        /// Reset action states for all units or for a specific player's units
        /// </summary>
        public void ResetActionStates(bool isPlayer = false)
        {
            var units = isPlayer ? GetUnitsByPlayer(isPlayer) : new List<DokkaebiUnit>(activeUnits.Values);
            
            foreach (var unit in units)
            {
                unit.ResetActionState();
            }
            
            Debug.Log($"Reset action states for {(isPlayer ? "player" : "all")} units");
        }
        
        /// <summary>
        /// Reset ability states for all units or for a specific player's units
        /// </summary>
        public void ResetAbilityStates(bool isPlayer = false)
        {
            var units = isPlayer ? GetUnitsByPlayer(isPlayer) : new List<DokkaebiUnit>(activeUnits.Values);
            
            foreach (var unit in units)
            {
                unit.ResetAbilityState();
            }
            
            Debug.Log($"Reset ability states for {(isPlayer ? "player" : "all")} units");
        }
        
        /// <summary>
        /// Set units interactable state based on player type
        /// </summary>
        public void SetUnitsInteractable(bool isPlayer, bool interactable)
        {
            var units = GetUnitsByPlayer(isPlayer);
            foreach (var unit in units)
            {
                unit.SetInteractable(interactable);
            }
            Debug.Log($"Set {units.Count} {(isPlayer ? "player" : "enemy")} units interactable: {interactable}");
        }
        
        /// <summary>
        /// Plan AI movement for non-player units
        /// </summary>
        public void PlanAIMovements()
        {
            var aiUnits = GetUnitsByPlayer(false);
            
            foreach (var unit in aiUnits)
            {
                // Basic AI logic - move towards nearest player unit if in range
                var playerUnits = GetUnitsByPlayer(true);
                if (playerUnits.Count > 0)
                {
                    // Find closest player unit
                    DokkaebiUnit closestUnit = null;
                    int minDistance = int.MaxValue;
                    
                    foreach (var playerUnit in playerUnits)
                    {
                        int distance = GridPosition.GetManhattanDistance(
                            unit.GetGridPosition(), 
                            playerUnit.GetGridPosition()
                        );
                        
                        if (distance < minDistance)
                        {
                            closestUnit = playerUnit;
                            minDistance = distance;
                        }
                    }
                    
                    // If found a player unit, try to move towards it
                    if (closestUnit != null)
                    {
                        // Get all possible move positions
                        var validMoves = unit.GetValidMovePositions();
                        
                        if (validMoves.Count > 0)
                        {
                            // Find move that gets us closest to target
                            GridPosition bestMove = unit.GetGridPosition();
                            int bestDistance = minDistance;
                            
                            foreach (var move in validMoves)
                            {
                                int dist = GridPosition.GetManhattanDistance(
                                    move, 
                                    closestUnit.GetGridPosition()
                                );
                                
                                if (dist < bestDistance)
                                {
                                    bestMove = move;
                                    bestDistance = dist;
                                }
                            }
                            
                            // Set the target position
                            if (bestDistance < minDistance)
                            {
                                unit.SetTargetPosition(bestMove);
                                Debug.Log($"AI unit {unit.GetUnitName()} planning move to {bestMove}");
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Plan AI abilities for non-player units
        /// </summary>
        public void PlanAIAbilities()
        {
            var aiUnits = GetUnitsByPlayer(false);
            
            foreach (var unit in aiUnits)
            {
                // Basic AI logic - use ability on nearest player unit if in range
                var abilities = unit.GetAbilities();
                var playerUnits = GetUnitsByPlayer(true);
                
                if (abilities.Count > 0 && playerUnits.Count > 0)
                {
                    // Find closest player unit
                    DokkaebiUnit closestUnit = null;
                    int minDistance = int.MaxValue;
                    
                    foreach (var playerUnit in playerUnits)
                    {
                        int distance = GridPosition.GetManhattanDistance(
                            unit.GetGridPosition(), 
                            playerUnit.GetGridPosition()
                        );
                        
                        if (distance < minDistance)
                        {
                            closestUnit = playerUnit;
                            minDistance = distance;
                        }
                    }
                    
                    // If found a player unit, try to use ability on it
                    if (closestUnit != null)
                    {
                        // Try each ability, starting with the most powerful
                        for (int i = abilities.Count - 1; i >= 0; i--)
                        {
                            AbilityData ability = abilities[i];
                            
                            // Check if ability is on cooldown
                            if (!unit.IsOnCooldown(ability.abilityType) && unit.GetCurrentAura() >= ability.auraCost)
                            {
                                // Check if ability is in range
                                if (minDistance <= ability.range)
                                {
                                    unit.PlanAbilityUse(i, closestUnit.GetGridPosition());
                                    Debug.Log($"AI unit {unit.GetUnitName()} planning ability {ability.displayName} on {closestUnit.GetUnitName()}");
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Get all units with pending movement
        /// </summary>
        public List<DokkaebiUnit> GetUnitsWithPendingMovement()
        {
            List<DokkaebiUnit> unitsWithMovement = new List<DokkaebiUnit>();
            
            foreach (var unit in activeUnits.Values)
            {
                if (unit.HasPendingMovement())
                {
                    unitsWithMovement.Add(unit);
                }
            }
            
            return unitsWithMovement;
        }
        
        /// <summary>
        /// Get all units with pending abilities
        /// </summary>
        public List<DokkaebiUnit> GetUnitsWithPendingAbilities()
        {
            List<DokkaebiUnit> unitsWithAbilities = new List<DokkaebiUnit>();
            
            foreach (var unit in activeUnits.Values)
            {
                if (unit.HasPendingAbility())
                {
                    unitsWithAbilities.Add(unit);
                }
            }
            
            return unitsWithAbilities;
        }
        
        /// <summary>
        /// Update all unit grid positions after movement
        /// </summary>
        public void UpdateAllUnitGridPositions()
        {
            foreach (var unit in activeUnits.Values)
            {
                // Update the grid position based on world position
                GridPosition gridPos = GridManager.Instance.WorldToGrid(unit.transform.position);
                unit.UpdateGridPosition(gridPos);
            }
        }
        
        /// <summary>
        /// Process status effects for all units
        /// </summary>
        public void ProcessStatusEffects()
        {
            foreach (var unit in activeUnits.Values)
            {
                unit.ProcessStatusEffects();
            }
        }
        
        /// <summary>
        /// Update cooldowns for all units
        /// </summary>
        public void UpdateCooldowns(bool isPlayerTurn)
        {
            var units = GetUnitsByPlayer(isPlayerTurn);
            foreach (var unit in units)
            {
                unit.UpdateCooldowns();
            }
        }
        
        /// <summary>
        /// Check if a player has any units remaining
        /// </summary>
        public bool HasRemainingUnits(bool isPlayer)
        {
            var units = GetUnitsByPlayer(isPlayer);
            return units.Count > 0 && units.Exists(u => u.IsAlive);
        }
        
        /// <summary>
        /// Check if all units of a player are ready
        /// </summary>
        public bool AreAllUnitsReady(bool isPlayer)
        {
            var units = GetUnitsByPlayer(isPlayer);
            
            foreach (var unit in units)
            {
                if (!unit.IsReady())
                {
                    return false;
                }
            }
            
            return true;
        }
        
        #endregion

        /// <summary>
        /// Process a unit being defeated and remove it from tracking
        /// </summary>
        public void HandleUnitDefeat(DokkaebiUnit unit)
        {
            if (unit == null) return;
            
            SmartLogger.Log($"UnitManager processing defeat of {unit.GetUnitName()} (Player: {unit.IsPlayer()})", LogCategory.Unit);
            
            // Trigger the unit defeated event for other systems
            OnUnitDefeated?.Invoke(unit);
            
            // Unregister the unit
            if (activeUnits.ContainsKey(unit.GetUnitId()))
            {
                activeUnits.Remove(unit.GetUnitId());
            }
        }

        /// <summary>
        /// Event triggered when a unit is defeated
        /// </summary>
        public event System.Action<DokkaebiUnit> OnUnitDefeated;

        /// <summary>
        /// Get the currently selected unit
        /// </summary>
        public DokkaebiUnit GetSelectedUnit()
        {
            return selectedUnit;
        }

        /// <summary>
        /// Set the currently selected unit
        /// </summary>
        public void SetSelectedUnit(DokkaebiUnit unit)
        {
            
Debug.Log($"[UnitManager] SetSelectedUnit called. Setting selectedUnit to: {(unit != null ? unit.GetUnitName() : "NULL")}");
    selectedUnit = unit;
        }

        /// <summary>
        /// Clear the currently selected unit
        /// </summary>
        public void ClearSelectedUnit()
        {
            Debug.Log($"[UnitManager] ClearSelectedUnit called. Setting selectedUnit to NULL.");
    selectedUnit = null;
        }
    }
} 