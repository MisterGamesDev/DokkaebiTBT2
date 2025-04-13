using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Dokkaebi.Core.Data;

namespace Dokkaebi.UI
{
    public class StatusEffectDisplay : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI durationText;
        [SerializeField] private TextMeshProUGUI stacksText;
        [SerializeField] private Image cooldownOverlay;

        private StatusEffectData effectData;

        public void Initialize(StatusEffectData effect)
        {
            effectData = effect;
            UpdateDisplay();
        }

        private void Update()
        {
            if (effectData != null)
            {
                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            if (effectData == null) return;

            // Update icon
            if (icon != null)
            {
                icon.sprite = effectData.icon;
            }

            // Update duration
            if (durationText != null)
            {
                if (effectData.isPermanent)
                {
                    durationText.text = "∞";
                }
                else
                {
                    durationText.text = effectData.duration.ToString();
                }
            }

            // Update stacks
            if (stacksText != null)
            {
                if (effectData.isStackable && effectData.maxStacks > 1)
                {
                    stacksText.text = effectData.maxStacks.ToString();
                    stacksText.gameObject.SetActive(true);
                }
                else
                {
                    stacksText.gameObject.SetActive(false);
                }
            }

            // Update cooldown overlay
            if (cooldownOverlay != null)
            {
                cooldownOverlay.gameObject.SetActive(false);
            }
        }
    }
} 