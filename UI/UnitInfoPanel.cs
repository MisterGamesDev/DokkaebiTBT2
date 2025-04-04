using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using Dokkaebi.Interfaces;
using Dokkaebi.Common;

namespace Dokkaebi.UI
{
    public class UnitInfoPanel : MonoBehaviour
    {
        [Header("Unit Basic Info")]
        [SerializeField] private TextMeshProUGUI unitNameText;
        [SerializeField] private TextMeshProUGUI originText;
        [SerializeField] private TextMeshProUGUI callingText;

        [Header("Stats")]
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private Slider mpSlider;
        [SerializeField] private TextMeshProUGUI mpText;

        [Header("Status Effects")]
        [SerializeField] private Transform statusEffectContainer;
        [SerializeField] private GameObject statusEffectPrefab;

        private IDokkaebiUnit currentUnit;
        private Dictionary<StatusEffectType, GameObject> activeStatusEffects = new Dictionary<StatusEffectType, GameObject>();

        private void OnEnable()
        {
            if (currentUnit != null)
            {
                SubscribeToUnitEvents(currentUnit);
            }
        }

        private void OnDisable()
        {
            if (currentUnit != null)
            {
                UnsubscribeFromUnitEvents(currentUnit);
            }
        }

        public void SetUnit(IDokkaebiUnit unit)
        {
            if (currentUnit != null)
            {
                UnsubscribeFromUnitEvents(currentUnit);
            }

            currentUnit = unit;
            if (currentUnit != null)
            {
                SubscribeToUnitEvents(currentUnit);
                UpdateAllInfo();
            }
        }

        private void SubscribeToUnitEvents(IDokkaebiUnit unit)
        {
            if (unit is IUnitEventHandler eventHandler)
            {
                eventHandler.OnDamageTaken += HandleDamageTaken;
                eventHandler.OnHealingReceived += HandleHealingReceived;
                eventHandler.OnStatusEffectApplied += HandleStatusEffectApplied;
                eventHandler.OnStatusEffectRemoved += HandleStatusEffectRemoved;
            }
        }

        private void UnsubscribeFromUnitEvents(IDokkaebiUnit unit)
        {
            if (unit is IUnitEventHandler eventHandler)
            {
                eventHandler.OnDamageTaken -= HandleDamageTaken;
                eventHandler.OnHealingReceived -= HandleHealingReceived;
                eventHandler.OnStatusEffectApplied -= HandleStatusEffectApplied;
                eventHandler.OnStatusEffectRemoved -= HandleStatusEffectRemoved;
            }
        }

        private void UpdateAllInfo()
        {
            if (currentUnit == null) return;

            // Update basic info
            if (unitNameText != null)
            {
                unitNameText.text = currentUnit.GetUnitName();
            }

            if (originText != null)
            {
                originText.text = "Unknown"; // Origin info not available through interface
            }

            if (callingText != null)
            {
                callingText.text = "Unknown"; // Calling info not available through interface
            }

            // Update stats
            UpdateHPDisplay(currentUnit.CurrentHealth, currentUnit.CurrentHealth); // IDokkaebiUnit only has CurrentHealth

            if (currentUnit is IExtendedDokkaebiUnit extendedUnit)
            {
                UpdateMPDisplay((int)extendedUnit.CurrentMP, (int)extendedUnit.MaxMP);
                
                // Update status effects
                ClearStatusEffects();
                foreach (var effect in extendedUnit.GetStatusEffects())
                {
                    CreateStatusEffectUI(effect);
                }
            }
        }

        private void UpdateHPDisplay(int currentHP, int maxHP)
        {
            if (hpSlider != null)
            {
                hpSlider.value = maxHP > 0 ? (float)currentHP / maxHP : 0;
            }
            if (hpText != null)
            {
                hpText.text = $"{currentHP}/{maxHP}";
            }
        }

        private void HandleDamageTaken(int amount, DamageType damageType)
        {
            if (currentUnit != null)
            {
                UpdateHPDisplay(currentUnit.CurrentHealth, currentUnit.CurrentHealth);
            }
        }

        private void HandleHealingReceived(int amount)
        {
            if (currentUnit != null)
            {
                UpdateHPDisplay(currentUnit.CurrentHealth, currentUnit.CurrentHealth);
            }
        }

        private void UpdateMPDisplay(int currentMP, int maxMP)
        {
            if (mpSlider != null)
            {
                mpSlider.value = maxMP > 0 ? (float)currentMP / maxMP : 0;
            }
            if (mpText != null)
            {
                mpText.text = $"{currentMP}/{maxMP}";
            }
        }

        private void ClearStatusEffects()
        {
            foreach (var effectUI in activeStatusEffects.Values)
            {
                if (effectUI != null)
                {
                    Destroy(effectUI);
                }
            }
            activeStatusEffects.Clear();
        }

        private void CreateStatusEffectUI(IStatusEffectInstance effect)
        {
            if (effect == null || statusEffectContainer == null || statusEffectPrefab == null) return;

            GameObject effectUI = Instantiate(statusEffectPrefab, statusEffectContainer);
            
            // Update UI elements based on the status effect
            var effectIcon = effectUI.GetComponentInChildren<Image>();
            var effectText = effectUI.GetComponentInChildren<TextMeshProUGUI>();
            
            if (effectText != null)
            {
                effectText.text = $"{effect.RemainingTurns}";
            }

            activeStatusEffects[effect.StatusEffectType] = effectUI;
        }

        private void HandleStatusEffectApplied(IStatusEffectInstance effect)
        {
            if (effect != null)
            {
                CreateStatusEffectUI(effect);
            }
        }

        private void HandleStatusEffectRemoved(IStatusEffectInstance effect)
        {
            if (effect != null && activeStatusEffects.TryGetValue(effect.StatusEffectType, out GameObject effectUI))
            {
                if (effectUI != null)
                {
                    Destroy(effectUI);
                }
                activeStatusEffects.Remove(effect.StatusEffectType);
            }
        }
    }
} 