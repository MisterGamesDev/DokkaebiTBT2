using UnityEngine;
using System.Collections.Generic;
using Dokkaebi.Utilities;
using Dokkaebi.Units;
using Dokkaebi.Common;

namespace Dokkaebi.Core
{
    /// <summary>
    /// Manages player Aura resources and their usage across the game.
    /// Centralizes aura-related functionality that was previously scattered.
    /// </summary>
    public class AuraManager : MonoBehaviour
    {
        // Singleton pattern
        private static AuraManager instance;
        public static AuraManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("AuraManager");
                    instance = go.AddComponent<AuraManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        [Header("Aura Settings")]
        [SerializeField] private int baseAuraPerTurn = 2;
        [SerializeField] private int maxAuraPerTurn = 5;

        // Player aura tracking
        private int player1CurrentAura;
        private int player2CurrentAura;
        private int player1MaxAura;
        private int player2MaxAura;

        // Events
        public delegate void AuraChangedHandler(int playerId, int oldValue, int newValue);
        public event AuraChangedHandler OnAuraChanged;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize aura values
            player1CurrentAura = baseAuraPerTurn;
            player2CurrentAura = baseAuraPerTurn;
            player1MaxAura = maxAuraPerTurn;
            player2MaxAura = maxAuraPerTurn;
        }

        /// <summary>
        /// Get the current aura for a player
        /// </summary>
        public int GetCurrentAura(bool isPlayer1)
        {
            return isPlayer1 ? player1CurrentAura : player2CurrentAura;
        }

        /// <summary>
        /// Get the maximum aura for a player
        /// </summary>
        public int GetMaxAura(bool isPlayer1)
        {
            return isPlayer1 ? player1MaxAura : player2MaxAura;
        }

        /// <summary>
        /// Modify a player's aura by the specified amount
        /// </summary>
        public void ModifyAura(bool isPlayer1, int amount)
        {
            int oldValue;
            int newValue;

            if (isPlayer1)
            {
                oldValue = player1CurrentAura;
                player1CurrentAura = Mathf.Clamp(player1CurrentAura + amount, 0, player1MaxAura);
                newValue = player1CurrentAura;
            }
            else
            {
                oldValue = player2CurrentAura;
                player2CurrentAura = Mathf.Clamp(player2CurrentAura + amount, 0, player2MaxAura);
                newValue = player2CurrentAura;
            }

            // Notify listeners of the change
            OnAuraChanged?.Invoke(isPlayer1 ? 1 : 2, oldValue, newValue);
            
            SmartLogger.Log($"Player {(isPlayer1 ? "1" : "2")} aura changed: {oldValue} -> {newValue} (modifier: {amount})", LogCategory.Ability);
        }

        /// <summary>
        /// Check if a player has enough aura to perform an action
        /// </summary>
        public bool HasEnoughAura(bool isPlayer1, int cost)
        {
            return GetCurrentAura(isPlayer1) >= cost;
        }

        /// <summary>
        /// Gains base aura at the start of a turn (or appropriate trigger point).
        /// Changed from ResetAuraForTurn to reflect adding behavior.
        /// </summary>
        public void GainAuraForTurn(bool isPlayer1)
        {
            // Use ModifyAura to handle clamping and events
            ModifyAura(isPlayer1, baseAuraPerTurn); 
            
            // Log the gain specifically
            SmartLogger.Log($"Player {(isPlayer1 ? "1" : "2")} gained {baseAuraPerTurn} Aura for turn. New value: {GetCurrentAura(isPlayer1)}", LogCategory.Ability);
        }

        /// <summary>
        /// Set the maximum aura for a player
        /// </summary>
        public void SetMaxAura(bool isPlayer1, int maxAura)
        {
            if (isPlayer1)
            {
                player1MaxAura = maxAura;
                player1CurrentAura = Mathf.Min(player1CurrentAura, maxAura);
            }
            else
            {
                player2MaxAura = maxAura;
                player2CurrentAura = Mathf.Min(player2CurrentAura, maxAura);
            }

            SmartLogger.Log($"Player {(isPlayer1 ? "1" : "2")} max aura set to {maxAura}", LogCategory.Ability);
        }
    }
} 