using System;
using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Units;
using Dokkaebi.Grid;
using Dokkaebi.Common;
using Dokkaebi.Core.Data;
using Dokkaebi.Interfaces;
using Dokkaebi.Utilities;

namespace Dokkaebi.Core.Networking
{
    /// <summary>
    /// Represents the game state data that is synchronized between client and server
    /// </summary>
    [Serializable]
    public class GameStateData
    {
        // Match metadata
        public string MatchId;
        public int CurrentTurn;
        public TurnPhase CurrentPhase;
        public bool IsPlayer1Turn;
        public long LastUpdateTimestamp;
        
        // Player data
        public PlayerStateData Player1;
        public PlayerStateData Player2;
        
        // Unit data
        public List<UnitStateData> Units;
        
        // Zone data
        public List<ZoneStateData> Zones;
        
        /// <summary>
        /// Convert from server dictionary data to typed GameStateData
        /// </summary>
        /// <param name="serverData">Dictionary data received from server</param>
        /// <returns>Parsed GameStateData object</returns>
        public static GameStateData FromDictionary(Dictionary<string, object> serverData)
        {
            var gameState = new GameStateData();
            
            // Parse metadata
            if (serverData.TryGetValue("matchId", out object matchIdObj))
                gameState.MatchId = matchIdObj as string;
                
            if (serverData.TryGetValue("currentTurn", out object turnObj) && turnObj is long turnLong)
                gameState.CurrentTurn = (int)turnLong;
                
            if (serverData.TryGetValue("currentPhase", out object phaseObj) && phaseObj is long phaseLong)
                gameState.CurrentPhase = (TurnPhase)(int)phaseLong;
                
            if (serverData.TryGetValue("isPlayer1Turn", out object isP1TurnObj))
                gameState.IsPlayer1Turn = Convert.ToBoolean(isP1TurnObj);
                
            if (serverData.TryGetValue("lastUpdateTimestamp", out object timestampObj) && timestampObj is long timestamp)
                gameState.LastUpdateTimestamp = timestamp;
            
            // Parse player data
            if (serverData.TryGetValue("player1", out object p1Obj) && p1Obj is Dictionary<string, object> p1Dict)
                gameState.Player1 = PlayerStateData.FromDictionary(p1Dict);
                
            if (serverData.TryGetValue("player2", out object p2Obj) && p2Obj is Dictionary<string, object> p2Dict)
                gameState.Player2 = PlayerStateData.FromDictionary(p2Dict);
            
            // Parse units
            gameState.Units = new List<UnitStateData>();
            if (serverData.TryGetValue("units", out object unitsObj) && unitsObj is List<object> unitsList)
            {
                foreach (var unitObj in unitsList)
                {
                    if (unitObj is Dictionary<string, object> unitDict)
                    {
                        var unitData = UnitStateData.FromDictionary(unitDict);
                        gameState.Units.Add(unitData);
                    }
                }
            }
            
            // Parse zones
            gameState.Zones = new List<ZoneStateData>();
            if (serverData.TryGetValue("zones", out object zonesObj) && zonesObj is List<object> zonesList)
            {
                foreach (var zoneObj in zonesList)
                {
                    if (zoneObj is Dictionary<string, object> zoneDict)
                    {
                        var zoneData = ZoneStateData.FromDictionary(zoneDict);
                        gameState.Zones.Add(zoneData);
                    }
                }
            }
            
            return gameState;
        }
        
        /// <summary>
        /// Convert GameStateData to a dictionary for server transmission
        /// </summary>
        /// <returns>Dictionary representation of game state</returns>
        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                { "matchId", MatchId },
                { "currentTurn", CurrentTurn },
                { "currentPhase", (int)CurrentPhase },
                { "isPlayer1Turn", IsPlayer1Turn },
                { "lastUpdateTimestamp", LastUpdateTimestamp }
            };
            
            // Add player data
            if (Player1 != null)
                dict["player1"] = Player1.ToDictionary();
                
            if (Player2 != null)
                dict["player2"] = Player2.ToDictionary();
            
            // Add units
            var unitsList = new List<Dictionary<string, object>>();
            if (Units != null)
            {
                foreach (var unit in Units)
                {
                    unitsList.Add(unit.ToDictionary());
                }
            }
            dict["units"] = unitsList;
            
            // Add zones
            var zonesList = new List<Dictionary<string, object>>();
            if (Zones != null)
            {
                foreach (var zone in Zones)
                {
                    zonesList.Add(zone.ToDictionary());
                }
            }
            dict["zones"] = zonesList;
            
            return dict;
        }
    }
    
    /// <summary>
    /// Represents a player's state data
    /// </summary>
    [Serializable]
    public class PlayerStateData
    {
        public string PlayerId;
        public bool IsPlayer1;
        public int CurrentAura;
        public int TotalAura;
        public int UnitsRemaining;
        
        /// <summary>
        /// Parse player state from dictionary
        /// </summary>
        public static PlayerStateData FromDictionary(Dictionary<string, object> dict)
        {
            var playerData = new PlayerStateData();
            
            if (dict.TryGetValue("playerId", out object playerIdObj))
                playerData.PlayerId = playerIdObj as string;
                
            if (dict.TryGetValue("isPlayer1", out object isP1Obj))
                playerData.IsPlayer1 = Convert.ToBoolean(isP1Obj);
                
            if (dict.TryGetValue("currentAura", out object auraObj) && auraObj is long auraLong)
                playerData.CurrentAura = (int)auraLong;
                
            if (dict.TryGetValue("totalAura", out object totalAuraObj) && totalAuraObj is long totalAuraLong)
                playerData.TotalAura = (int)totalAuraLong;
                
            if (dict.TryGetValue("unitsRemaining", out object unitsObj) && unitsObj is long unitsLong)
                playerData.UnitsRemaining = (int)unitsLong;
                
            return playerData;
        }
        
        /// <summary>
        /// Convert to dictionary
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "playerId", PlayerId },
                { "isPlayer1", IsPlayer1 },
                { "currentAura", CurrentAura },
                { "totalAura", TotalAura },
                { "unitsRemaining", UnitsRemaining }
            };
        }
    }
    
    /// <summary>
    /// Represents a unit's state data
    /// </summary>
    [Serializable]
    public class UnitStateData
    {
        public string UnitId;
        public string UnitType;
        public bool IsPlayerUnit;
        public bool IsPlayer1Unit;
        public Vector2Int Position;
        public int CurrentHP;
        public int MaxHP;
        public float CurrentMP;
        public float MaxMP;
        public bool HasMoved;
        public bool HasUsedAbility;
        public List<AbilityStateData> Abilities;
        public List<IStatusEffectInstance> StatusEffects;
        
        // Planned ability data
        public bool HasPlannedAbility;
        public int PlannedAbilityIndex;
        public Vector2Int PlannedAbilityTarget;
        
        /// <summary>
        /// Parse unit state from dictionary
        /// </summary>
        public static UnitStateData FromDictionary(Dictionary<string, object> dict)
        {
            var unitData = new UnitStateData();
            
            if (dict.TryGetValue("unitId", out object unitIdObj))
                unitData.UnitId = unitIdObj as string;
                
            if (dict.TryGetValue("unitType", out object unitTypeObj))
                unitData.UnitType = unitTypeObj as string;
                
            if (dict.TryGetValue("isPlayerUnit", out object isPlayerObj))
                unitData.IsPlayerUnit = Convert.ToBoolean(isPlayerObj);
                
            if (dict.TryGetValue("isPlayer1Unit", out object isP1Obj))
                unitData.IsPlayer1Unit = Convert.ToBoolean(isP1Obj);
                
            // Parse position
            unitData.Position = new Vector2Int();
            if (dict.TryGetValue("posX", out object posXObj) && posXObj is long posXLong)
                unitData.Position.x = (int)posXLong;
                
            if (dict.TryGetValue("posY", out object posYObj) && posYObj is long posYLong)
                unitData.Position.y = (int)posYLong;
                
            // Parse HP/MP
            if (dict.TryGetValue("currentHP", out object currentHPObj) && currentHPObj is long currentHPLong)
                unitData.CurrentHP = (int)currentHPLong;
                
            if (dict.TryGetValue("maxHP", out object maxHPObj) && maxHPObj is long maxHPLong)
                unitData.MaxHP = (int)maxHPLong;
                
            if (dict.TryGetValue("currentMP", out object currentMPObj))
            {
                if (currentMPObj is long mpLong)
                    unitData.CurrentMP = mpLong;
                else if (currentMPObj is double mpDouble)
                    unitData.CurrentMP = (float)mpDouble;
            }
                
            if (dict.TryGetValue("maxMP", out object maxMPObj))
            {
                if (maxMPObj is long mpLong)
                    unitData.MaxMP = mpLong;
                else if (maxMPObj is double mpDouble)
                    unitData.MaxMP = (float)mpDouble;
            }
                
            // Parse action states
            if (dict.TryGetValue("hasMoved", out object hasMovedObj))
                unitData.HasMoved = Convert.ToBoolean(hasMovedObj);
                
            if (dict.TryGetValue("hasUsedAbility", out object hasUsedObj))
                unitData.HasUsedAbility = Convert.ToBoolean(hasUsedObj);
                
            // Parse planned ability data
            if (dict.TryGetValue("hasPlannedAbility", out object hasPlannedObj))
                unitData.HasPlannedAbility = Convert.ToBoolean(hasPlannedObj);
                
            if (dict.TryGetValue("plannedAbilityIndex", out object plannedIndexObj) && plannedIndexObj is long plannedIndexLong)
                unitData.PlannedAbilityIndex = (int)plannedIndexLong;
                
            // Parse planned ability target position
            if (dict.TryGetValue("plannedTargetX", out object targetXObj) && targetXObj is long targetXLong)
                unitData.PlannedAbilityTarget.x = (int)targetXLong;
                
            if (dict.TryGetValue("plannedTargetY", out object targetYObj) && targetYObj is long targetYLong)
                unitData.PlannedAbilityTarget.y = (int)targetYLong;
                
            // Parse abilities
            unitData.Abilities = new List<AbilityStateData>();
            if (dict.TryGetValue("abilities", out object abilitiesObj) && abilitiesObj is List<object> abilitiesList)
            {
                foreach (var abilityObj in abilitiesList)
                {
                    if (abilityObj is Dictionary<string, object> abilityDict)
                    {
                        var abilityData = AbilityStateData.FromDictionary(abilityDict);
                        unitData.Abilities.Add(abilityData);
                    }
                }
            }
            
            // Parse status effects
            unitData.StatusEffects = new List<IStatusEffectInstance>();
            if (dict.TryGetValue("statusEffects", out object effectsObj) && effectsObj is List<object> effectsList)
            {
                foreach (var effectObj in effectsList)
                {
                    if (effectObj is Dictionary<string, object> effectDict)
                    {
                        string effectId = effectDict.TryGetValue("effectId", out object idObj) ? idObj as string : null;
                        int remainingDuration = effectDict.TryGetValue("remainingDuration", out object durationObj) && durationObj is long durationLong ? (int)durationLong : 0;
                        int sourceUnitId = effectDict.TryGetValue("sourceUnitId", out object sourceObj) && sourceObj is long sourceLong ? (int)sourceLong : -1;

                        // Get effect data from DataManager using effectId
                        var effectData = DataManager.Instance.GetStatusEffectData(effectId);
                        if (effectData != null)
                        {
                            var effectInstance = new StatusEffectInstance(effectData, remainingDuration, sourceUnitId);
                            unitData.StatusEffects.Add(effectInstance);
                        }
                        else
                        {
                            SmartLogger.LogWarning($"Status effect data not found for ID: {effectId}", LogCategory.StateSync);
                        }
                    }
                }
            }
            
            return unitData;
        }
        
        /// <summary>
        /// Convert to dictionary
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                { "unitId", UnitId },
                { "unitType", UnitType },
                { "isPlayerUnit", IsPlayerUnit },
                { "isPlayer1Unit", IsPlayer1Unit },
                { "posX", Position.x },
                { "posY", Position.y },
                { "currentHP", CurrentHP },
                { "maxHP", MaxHP },
                { "currentMP", CurrentMP },
                { "maxMP", MaxMP },
                { "hasMoved", HasMoved },
                { "hasUsedAbility", HasUsedAbility },
                { "hasPlannedAbility", HasPlannedAbility },
                { "plannedAbilityIndex", PlannedAbilityIndex },
                { "plannedTargetX", PlannedAbilityTarget.x },
                { "plannedTargetY", PlannedAbilityTarget.y }
            };
            
            // Add abilities
            var abilitiesList = new List<Dictionary<string, object>>();
            if (Abilities != null)
            {
                foreach (var ability in Abilities)
                {
                    abilitiesList.Add(ability.ToDictionary());
                }
            }
            dict["abilities"] = abilitiesList;
            
            // Add status effects
            var effectsList = new List<Dictionary<string, object>>();
            if (StatusEffects != null)
            {
                foreach (var effect in StatusEffects)
                {
                    effectsList.Add(new Dictionary<string, object>
                    {
                        { "effectId", effect.Effect.effectId },
                        { "effectType", effect.Effect.effectType.ToString() },
                        { "remainingDuration", effect.RemainingDuration },
                        { "sourceUnitId", effect.SourceUnitId }
                    });
                }
            }
            dict["statusEffects"] = effectsList;
            
            return dict;
        }
    }
    
    /// <summary>
    /// Represents an ability's state data
    /// </summary>
    [Serializable]
    public class AbilityStateData
    {
        public int AbilityId;
        public string AbilityName;
        public int CurrentCooldown;
        public int MaxCooldown;
        public bool IsOnCooldown => CurrentCooldown > 0;
        
        /// <summary>
        /// Parse ability state from dictionary
        /// </summary>
        public static AbilityStateData FromDictionary(Dictionary<string, object> dict)
        {
            var abilityData = new AbilityStateData();
            
            if (dict.TryGetValue("abilityId", out object idObj) && idObj is long idLong)
                abilityData.AbilityId = (int)idLong;
                
            if (dict.TryGetValue("abilityName", out object nameObj))
                abilityData.AbilityName = nameObj as string;
                
            if (dict.TryGetValue("currentCooldown", out object currentObj) && currentObj is long currentLong)
                abilityData.CurrentCooldown = (int)currentLong;
                
            if (dict.TryGetValue("maxCooldown", out object maxObj) && maxObj is long maxLong)
                abilityData.MaxCooldown = (int)maxLong;
                
            return abilityData;
        }
        
        /// <summary>
        /// Convert to dictionary
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "abilityId", AbilityId },
                { "abilityName", AbilityName },
                { "currentCooldown", CurrentCooldown },
                { "maxCooldown", MaxCooldown }
            };
        }
    }
    
    /// <summary>
    /// Represents a zone's state data
    /// </summary>
    [Serializable]
    public class ZoneStateData
    {
        public string ZoneId;
        public string ZoneType;
        public Vector2Int Position;
        public int Size;
        public int RemainingDuration;
        public string OwnerUnitId;
        
        /// <summary>
        /// Parse zone state from dictionary
        /// </summary>
        public static ZoneStateData FromDictionary(Dictionary<string, object> dict)
        {
            var zoneData = new ZoneStateData();
            
            if (dict.TryGetValue("zoneId", out object idObj))
                zoneData.ZoneId = idObj as string;
                
            if (dict.TryGetValue("zoneType", out object typeObj))
                zoneData.ZoneType = typeObj as string;
                
            // Parse position
            zoneData.Position = new Vector2Int();
            if (dict.TryGetValue("posX", out object posXObj) && posXObj is long posXLong)
                zoneData.Position.x = (int)posXLong;
                
            if (dict.TryGetValue("posY", out object posYObj) && posYObj is long posYLong)
                zoneData.Position.y = (int)posYLong;
                
            if (dict.TryGetValue("size", out object sizeObj) && sizeObj is long sizeLong)
                zoneData.Size = (int)sizeLong;
                
            if (dict.TryGetValue("remainingDuration", out object durationObj) && durationObj is long durationLong)
                zoneData.RemainingDuration = (int)durationLong;
                
            if (dict.TryGetValue("ownerUnitId", out object ownerObj))
                zoneData.OwnerUnitId = ownerObj as string;
                
            return zoneData;
        }
        
        /// <summary>
        /// Convert to dictionary
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "zoneId", ZoneId },
                { "zoneType", ZoneType },
                { "posX", Position.x },
                { "posY", Position.y },
                { "size", Size },
                { "remainingDuration", RemainingDuration },
                { "ownerUnitId", OwnerUnitId }
            };
        }
    }
} 