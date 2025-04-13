using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Dokkaebi.Core;
using Dokkaebi.Units;
using Dokkaebi.Common;
using Dokkaebi.Core.Networking;
using Dokkaebi.Utilities;
using Dokkaebi.Interfaces;

namespace Dokkaebi.UI
{
    public class PlayerResourceUI : MonoBehaviour, DokkaebiUpdateManager.IUpdateObserver
    {
        [Header("Aura Display")]
        [SerializeField] private Slider auraSlider;
        [SerializeField] private TextMeshProUGUI auraText;
        [SerializeField] private Image auraSliderFill;
        [SerializeField] private Color fullAuraColor = Color.blue;
        [SerializeField] private Color lowAuraColor = Color.red;

        [Header("Moves Used")]
        [SerializeField] private TextMeshProUGUI movesUsedText;
        [SerializeField] private Slider movesUsedSlider;
        [SerializeField] private Image movesUsedSliderFill;
        [SerializeField] private Color availableMovesColor = Color.green;
        [SerializeField] private Color usedMovesColor = Color.gray;

        [Header("Aura Gain Display")]
        [SerializeField] private GameObject auraGainDisplay;

        private UnitStateManager unitStateManager;
        private DokkaebiTurnSystemCore turnSystem;
        private bool isPlayer1UI = true;
        private IDokkaebiUnit currentUnit;

        private void Awake()
        {
            unitStateManager = FindObjectOfType<UnitStateManager>();
            turnSystem = FindObjectOfType<DokkaebiTurnSystemCore>();

            if (unitStateManager == null || turnSystem == null)
            {
                SmartLogger.LogError("Required managers not found in scene!", LogCategory.General);
                return;
            }
        }

        private void OnEnable()
        {
            // Subscribe to turn system events
            if (turnSystem != null)
            {
                turnSystem.OnPhaseChanged += HandlePhaseChanged;
            }

            // Subscribe to game state updates
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameStateUpdated += HandleGameStateUpdate;
            }

            // Initial display update
            UpdateDisplays();

            // Start polling for selected unit changes
            DokkaebiUpdateManager.Instance.RegisterUpdateObserver(this);
        }

        private void OnDisable()
        {
            // Unsubscribe from turn system events
            if (turnSystem != null)
            {
                turnSystem.OnPhaseChanged -= HandlePhaseChanged;
            }

            // Unsubscribe from game state updates
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameStateUpdated -= HandleGameStateUpdate;
            }

            // Unsubscribe from current unit's events
            UnsubscribeFromCurrentUnit();

            // Stop polling for selected unit changes
            if (DokkaebiUpdateManager.Instance != null)
            {
                DokkaebiUpdateManager.Instance.UnregisterUpdateObserver(this);
            }
        }

        private void UnsubscribeFromCurrentUnit()
        {
            if (currentUnit is DokkaebiUnit dokkaebiUnit)
            {
                dokkaebiUnit.OnUnitAuraChanged -= HandleUnitAuraChanged;
            }
        }

        private void HandleUnitAuraChanged(int oldValue, int newValue)
        {
            if (currentUnit is DokkaebiUnit dokkaebiUnit)
            {
                UpdateAuraDisplay(newValue, dokkaebiUnit.GetMaxUnitAura());
            }
        }

        private void HandleGameStateUpdate(GameStateData newState)
        {
            if (newState == null || unitStateManager == null) return;

            // Get the relevant player data
            var playerData = isPlayer1UI ? newState.Player1 : newState.Player2;
            if (playerData == null) return;

            // Update moves used display
            int currentMoves = isPlayer1UI ? unitStateManager.GetPlayer1UnitsMoved() : unitStateManager.GetPlayer2UnitsMoved();
            int requiredMoves = isPlayer1UI ? unitStateManager.GetRequiredPlayer1Moves() : unitStateManager.GetRequiredPlayer2Moves();
            UpdateMovesUsedDisplay(currentMoves, requiredMoves);
        }

        private void HandlePhaseChanged(TurnPhase phase)
        {
            // Only show aura gain in aura phases
            bool isAuraPhase = phase == TurnPhase.AuraPhase1A || 
                             phase == TurnPhase.AuraPhase1B || 
                             phase == TurnPhase.AuraPhase2A || 
                             phase == TurnPhase.AuraPhase2B;
            
            auraGainDisplay.SetActive(isAuraPhase);

            // Update displays when phase changes
            UpdateDisplays();
        }

        private void UpdateDisplays()
        {
            if (unitStateManager == null) return;

            // Update moves used display
            int currentMoves = isPlayer1UI ? unitStateManager.GetPlayer1UnitsMoved() : unitStateManager.GetPlayer2UnitsMoved();
            int requiredMoves = isPlayer1UI ? unitStateManager.GetRequiredPlayer1Moves() : unitStateManager.GetRequiredPlayer2Moves();
            UpdateMovesUsedDisplay(currentMoves, requiredMoves);

            // Update aura display if we have a current unit
            if (currentUnit is DokkaebiUnit dokkaebiUnit)
            {
                UpdateAuraDisplay(dokkaebiUnit.GetCurrentUnitAura(), dokkaebiUnit.GetMaxUnitAura());
            }
            else
            {
                // If no unit is selected, show 0/0 aura
                UpdateAuraDisplay(0, 0);
            }
        }

        private void UpdateAuraDisplay(int currentAura, int maxAura)
        {
            if (auraSlider != null)
            {
                auraSlider.maxValue = maxAura;
                auraSlider.value = currentAura;
            }

            if (auraText != null)
            {
                auraText.text = $"{currentAura}/{maxAura}";
            }

            if (auraSliderFill != null)
            {
                // Update color based on Aura amount
                float auraPercentage = maxAura > 0 ? (float)currentAura / maxAura : 0f;
                auraSliderFill.color = Color.Lerp(lowAuraColor, fullAuraColor, auraPercentage);
            }
        }

        private void UpdateMovesUsedDisplay(int used, int total)
        {
            if (movesUsedText != null)
            {
                movesUsedText.text = $"Moves Used: {used}/{total}";
            }

            if (movesUsedSlider != null)
            {
                movesUsedSlider.maxValue = total;
                movesUsedSlider.value = used;
            }

            if (movesUsedSliderFill != null)
            {
                // Update color based on remaining moves
                float remainingPercentage = 1f - ((float)used / total);
                movesUsedSliderFill.color = Color.Lerp(usedMovesColor, availableMovesColor, remainingPercentage);
            }
        }

        public void SetIsPlayer1UI(bool isPlayer1)
        {
            isPlayer1UI = isPlayer1;
            UpdateDisplays();
        }

        public void CustomUpdate(float deltaTime)
        {
            // Check for selected unit changes
            var selectedUnit = UnitManager.Instance?.GetSelectedUnit();
            
            // Only update if the selected unit has changed and matches our player side
            if (selectedUnit != currentUnit && selectedUnit != null && selectedUnit.IsPlayerControlled == isPlayer1UI)
            {
                UnsubscribeFromCurrentUnit();
                currentUnit = selectedUnit;
                
                // Subscribe to the new unit's events
                if (currentUnit is DokkaebiUnit dokkaebiUnit)
                {
                    dokkaebiUnit.OnUnitAuraChanged += HandleUnitAuraChanged;
                    UpdateAuraDisplay(dokkaebiUnit.GetCurrentUnitAura(), dokkaebiUnit.GetMaxUnitAura());
                }
            }
            else if (selectedUnit == null && currentUnit != null)
            {
                // Clear current unit if nothing is selected
                UnsubscribeFromCurrentUnit();
                currentUnit = null;
                UpdateAuraDisplay(0, 0);
            }
        }
    }
} 
