using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Dokkaebi.Core;
using Dokkaebi.Common;
using Dokkaebi.Interfaces;

namespace Dokkaebi.UI
{
    public class TurnPhaseUI : MonoBehaviour
    {
        [Header("Phase Display")]
        [SerializeField] private TextMeshProUGUI phaseText;
        [SerializeField] private TextMeshProUGUI turnNumberText;
        [SerializeField] private TextMeshProUGUI activePlayerText;
        [SerializeField] private TextMeshProUGUI phaseTimerText;

        [Header("Phase Icons")]
        [SerializeField] private GameObject[] phaseIcons;
        [SerializeField] private Color activePhaseColor = Color.yellow;
        [SerializeField] private Color inactivePhaseColor = Color.gray;

        private ITurnSystem turnSystem;

        private void Awake()
        {
            turnSystem = FindObjectOfType<DokkaebiTurnSystemCore>();
            if (turnSystem == null)
            {
                Debug.LogError("TurnPhaseUI: Could not find ITurnSystem implementation!");
            }
        }

        private void OnEnable()
        {
            if (turnSystem != null)
            {
                turnSystem.OnPhaseChanged += HandlePhaseStart;
                turnSystem.OnTurnChanged += HandleTurnStart;
                turnSystem.OnActivePlayerChanged += HandleActivePlayerChanged;
            }
        }

        private void OnDisable()
        {
            if (turnSystem != null)
            {
                turnSystem.OnPhaseChanged -= HandlePhaseStart;
                turnSystem.OnTurnChanged -= HandleTurnStart;
                turnSystem.OnActivePlayerChanged -= HandleActivePlayerChanged;
            }
        }

        private void HandlePhaseStart(TurnPhase phase)
        {
            UpdatePhaseDisplay(phase);
            UpdatePhaseIcons(phase);
        }

        private void HandleTurnStart(int turnNumber)
        {
            UpdateTurnDisplay(turnNumber, turnSystem.ActivePlayerId);
        }

        private void HandleActivePlayerChanged(int playerId)
        {
            UpdateTurnDisplay(turnSystem.CurrentTurn, playerId);
        }

        private void UpdatePhaseDisplay(TurnPhase phase)
        {
            if (phaseText != null)
            {
                phaseText.text = FormatPhaseName(phase);
            }
        }

        private void UpdateTurnDisplay(int turnNumber, int activePlayerNumber)
        {
            if (turnNumberText != null)
            {
                turnNumberText.text = $"Turn {turnNumber}";
            }

            if (activePlayerText != null)
            {
                string playerText = activePlayerNumber == 0 ? "Both Players" : $"Player {activePlayerNumber}";
                activePlayerText.text = $"{playerText}'s Turn";
            }
        }

        private void UpdatePhaseTimer(float remainingTime)
        {
            if (phaseTimerText != null)
            {
                if (remainingTime > 0)
                {
                    phaseTimerText.text = $"Time: {Mathf.CeilToInt(remainingTime)}s";
                    phaseTimerText.gameObject.SetActive(true);
                }
                else
                {
                    phaseTimerText.gameObject.SetActive(false);
                }
            }
        }

        private void UpdatePhaseIcons(TurnPhase currentPhase)
        {
            if (phaseIcons == null) return;

            for (int i = 0; i < phaseIcons.Length; i++)
            {
                if (phaseIcons[i] != null)
                {
                    var iconImage = phaseIcons[i].GetComponent<Image>();
                    if (iconImage != null)
                    {
                        iconImage.color = (TurnPhase)i == currentPhase ? activePhaseColor : inactivePhaseColor;
                    }
                }
            }
        }

        private string FormatPhaseName(TurnPhase phase)
        {
            switch (phase)
            {
                case TurnPhase.Opening:
                    return "Opening Phase";
                case TurnPhase.MovementPhase:
                    return "Movement Phase";
                case TurnPhase.AuraPhase1A:
                    return "Aura Phase (Player 1)";
                case TurnPhase.AuraPhase1B:
                    return "Aura Phase (Player 2)";
                case TurnPhase.AuraPhase2A:
                    return "Second Aura Phase (Player 1)";
                case TurnPhase.AuraPhase2B:
                    return "Second Aura Phase (Player 2)";
                case TurnPhase.Resolution:
                    return "Resolution Phase";
                case TurnPhase.EndTurn:
                    return "End Turn";
                case TurnPhase.GameOver:
                    return "Game Over";
                default:
                    return phase.ToString();
            }
        }
    }
} 