using UnityEngine;
using TMPro;
using Dokkaebi.Core;

namespace Dokkaebi.UI
{
    public class DebugUIController : MonoBehaviour {
        public TextMeshProUGUI debugText;
        private DokkaebiTurnSystemCore turnSystem;
        
        void Start() {
            turnSystem = FindObjectOfType<DokkaebiTurnSystemCore>();
        }
        
        void Update() {
            if (turnSystem != null && debugText != null) {
                debugText.text = $"Turn: {turnSystem.CurrentTurn}\n" +
                                $"Phase: {turnSystem.CurrentPhase}\n" +
                                $"Active Player: {turnSystem.ActivePlayerId}";
            }
        }
    }
}