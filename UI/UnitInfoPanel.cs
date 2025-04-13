using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using Dokkaebi.Interfaces;
using Dokkaebi.Common;
using Dokkaebi.Units;
using Dokkaebi.Core;
using Dokkaebi.Utilities;

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
        [SerializeField] private Slider auraSlider;
        [SerializeField] private TextMeshProUGUI auraText;

        [Header("Status Effects")]
        [SerializeField] private Transform statusEffectContainer;
        [SerializeField] private GameObject statusEffectPrefab;

        private IDokkaebiUnit currentUnit;
        private Dictionary<StatusEffectType, GameObject> activeStatusEffects = new Dictionary<StatusEffectType, GameObject>();
        private bool isInitialized = false;

        private void Start()
        {
            SmartLogger.Log("[UnitInfoPanel Start] Initializing panel...", LogCategory.UI, this);
            gameObject.SetActive(false);
            currentUnit = null;
            ClearAllDisplays();
            InitializeComponents();
            isInitialized = true;
            SmartLogger.Log($"[UnitInfoPanel Start] Panel initialized. activeSelf: {gameObject.activeSelf}", LogCategory.UI, this);
        }

        private void OnEnable()
        {
            SmartLogger.Log("[UnitInfoPanel OnEnable] Panel enabled", LogCategory.UI, this);
            if (currentUnit != null)
            {
                SmartLogger.Log($"[UnitInfoPanel OnEnable] Refreshing display for unit: {currentUnit.GetUnitName()}", LogCategory.UI, this);
                SubscribeToUnitEvents(currentUnit);
                UpdateAllInfo(); // Refresh display when panel becomes visible
            }
        }

        private void OnDisable()
        {
            SmartLogger.Log("[UnitInfoPanel OnDisable] Panel disabled", LogCategory.UI, this);
            if (currentUnit != null)
            {
                SmartLogger.Log($"[UnitInfoPanel OnDisable] Unsubscribing from events for unit: {currentUnit.GetUnitName()}", LogCategory.UI, this);
                UnsubscribeFromUnitEvents(currentUnit);
            }
        }

        public void SetUnit(IDokkaebiUnit unit)
        {
            SmartLogger.Log($"[UnitInfoPanel SetUnit ENTRY] Called with unit: {(unit != null ? unit.GetUnitName() : "NULL")}. Panel currently activeSelf: {gameObject.activeSelf}", LogCategory.UI, this);

            // If we're setting to the same unit, no need to do anything
            if (currentUnit == unit)
            {
                SmartLogger.Log("[UnitInfoPanel SetUnit] Same unit, returning early", LogCategory.UI, this);
                return;
            }

            // Unsubscribe from current unit's events if there is one
            if (currentUnit != null)
            {
                SmartLogger.Log($"[UnitInfoPanel SetUnit] Unsubscribing from current unit: {currentUnit.GetUnitName()}", LogCategory.UI, this);
                UnsubscribeFromUnitEvents(currentUnit);
            }

            // Update current unit reference before anything else
            currentUnit = unit;
            SmartLogger.Log($"[UnitInfoPanel SetUnit] Updated currentUnit reference to: {(currentUnit != null ? currentUnit.GetUnitName() : "NULL")}", LogCategory.UI, this);
            
            if (currentUnit != null)
            {
                // First update all information while panel might still be hidden
                SmartLogger.Log("[UnitInfoPanel SetUnit] Setting up new unit...", LogCategory.UI, this);
                SubscribeToUnitEvents(currentUnit);
                UpdateAllInfo();
                
                // Then show the panel
                SmartLogger.Log($"[UnitInfoPanel SetUnit] Preparing to set panel ACTIVE for '{currentUnit.GetUnitName()}'", LogCategory.UI, this);
                gameObject.SetActive(true);
                SmartLogger.Log($"[UnitInfoPanel SetUnit] Panel activeSelf is now: {gameObject.activeSelf}", LogCategory.UI, this);
            }
            else
            {
                // Clear and hide the panel
                SmartLogger.Log("[UnitInfoPanel SetUnit] Clearing and hiding panel...", LogCategory.UI, this);
                ClearAllDisplays();
                SmartLogger.Log("[UnitInfoPanel SetUnit] Preparing to set panel INACTIVE", LogCategory.UI, this);
                gameObject.SetActive(false);
                SmartLogger.Log($"[UnitInfoPanel SetUnit] Panel activeSelf is now: {gameObject.activeSelf}", LogCategory.UI, this);
            }
        }

        private void InitializeComponents()
        {
            // Verify all required components are assigned
            if (unitNameText == null || originText == null || callingText == null || 
                hpText == null || auraText == null || hpSlider == null || auraSlider == null)
            {
                SmartLogger.LogError("One or more required UI components are not assigned!", LogCategory.UI, this);
            }

            if (statusEffectContainer == null || statusEffectPrefab == null)
            {
                SmartLogger.LogError("Status effect display components are not assigned!", LogCategory.UI, this);
            }
        }

        private void ClearAllDisplays()
        {
            ClearStatusEffects();
            if (unitNameText != null) unitNameText.text = "";
            if (originText != null) originText.text = "";
            if (callingText != null) callingText.text = "";
            if (hpText != null) hpText.text = "0/0";
            if (auraText != null) auraText.text = "0/0";
            if (hpSlider != null) hpSlider.value = 0;
            if (auraSlider != null) auraSlider.value = 0;
        }

        private void SubscribeToUnitEvents(IDokkaebiUnit unit)
        {
            if (unit is IUnitEventHandler eventHandler)
            {
                eventHandler.OnDamageTaken += HandleDamageTaken;
                eventHandler.OnHealingReceived += HandleHealingReceived;
                eventHandler.OnStatusEffectApplied += HandleStatusEffectApplied;
                eventHandler.OnStatusEffectRemoved += HandleStatusEffectRemoved;
                
                // Subscribe to unit-specific Aura changes
                if (unit is DokkaebiUnit dokkaebiUnit)
                {
                    dokkaebiUnit.OnUnitAuraChanged += HandleUnitAuraChanged;
                }
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
                
                // Unsubscribe from unit-specific Aura changes
                if (unit is DokkaebiUnit dokkaebiUnit)
                {
                    dokkaebiUnit.OnUnitAuraChanged -= HandleUnitAuraChanged;
                }
            }
        }

        private void UpdateAllInfo()
        {
            if (!isInitialized) return;

            if (currentUnit == null)
            {
                SmartLogger.LogWarning("[UnitInfoPanel UpdateAllInfo] Called with null currentUnit!", LogCategory.UI, this);
                return;
            }

            SmartLogger.Log($"[UnitInfoPanel UpdateAllInfo] Updating display for unit: {currentUnit.GetUnitName()}", LogCategory.UI, this);

            // Update basic info
            if (unitNameText != null)
            {
                unitNameText.text = currentUnit.GetUnitName();
            }

            // Try to get Origin/Calling data if available
            if (currentUnit is DokkaebiUnit concreteUnit)
            {
                if (originText != null)
                {
                    originText.text = concreteUnit.GetOrigin()?.displayName ?? "Unknown";
                }
                if (callingText != null)
                {
                    callingText.text = concreteUnit.GetCalling()?.displayName ?? "Unknown";
                }

                // Update HP with MaxHealth from concrete unit
                UpdateHPDisplay(currentUnit.CurrentHealth, concreteUnit.MaxHealth);
                
                // Update unit-specific Aura
                UpdateAuraDisplay(concreteUnit.GetCurrentUnitAura(), concreteUnit.GetMaxUnitAura());
                
                // Update status effects
                ClearStatusEffects();
                foreach (var effect in concreteUnit.GetStatusEffects())
                {
                    CreateStatusEffectUI(effect);
                }
            }
            else
            {
                if (originText != null) originText.text = "Unknown";
                if (callingText != null) callingText.text = "Unknown";
                
                // Fallback HP display if we can't get MaxHealth
                UpdateHPDisplay(currentUnit.CurrentHealth, currentUnit.CurrentHealth);
                SmartLogger.LogWarning("Could not get MaxHealth via concrete cast in UpdateAllInfo.", LogCategory.UI, this);
                
                // Reset Aura display
                if (auraSlider != null) auraSlider.value = 0;
                if (auraText != null) auraText.text = "0/0";
            }
        }

        private void UpdateHPDisplay(int currentHP, int maxHP)
        {
            if (hpSlider != null)
            {
                hpSlider.maxValue = maxHP > 0 ? maxHP : 1;
                hpSlider.value = currentHP;
            }
            if (hpText != null)
            {
                hpText.text = $"{currentHP} / {maxHP}";
            }
        }

        private void HandleDamageTaken(int amount, DamageType damageType)
        {
            if (currentUnit != null && currentUnit is DokkaebiUnit concreteUnit)
            {
                UpdateHPDisplay(currentUnit.CurrentHealth, concreteUnit.MaxHealth);
            }
        }

        private void HandleHealingReceived(int amount)
        {
            if (currentUnit != null && currentUnit is DokkaebiUnit concreteUnit)
            {
                UpdateHPDisplay(currentUnit.CurrentHealth, concreteUnit.MaxHealth);
            }
        }

        private void UpdateAuraDisplay(int currentAura, int maxAura)
        {
            if (auraSlider != null)
            {
                auraSlider.maxValue = maxAura > 0 ? maxAura : 1;
                auraSlider.value = currentAura;
            }
            if (auraText != null)
            {
                auraText.text = $"{currentAura} / {maxAura}";
            }
        }

        private void HandleUnitAuraChanged(int oldAura, int newAura)
        {
            if (currentUnit != null && currentUnit is DokkaebiUnit concreteUnit)
            {
                UpdateAuraDisplay(newAura, concreteUnit.GetMaxUnitAura());
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
            
            // Try to get the StatusEffectDisplay component
            var statusEffectDisplay = effectUI.GetComponent<StatusEffectDisplay>();
            if (statusEffectDisplay != null)
            {
                statusEffectDisplay.Initialize(effect.Effect);
            }
            else
            {
                // Fallback to basic display if StatusEffectDisplay component not found
                var effectIcon = effectUI.GetComponentInChildren<Image>();
                var effectText = effectUI.GetComponentInChildren<TextMeshProUGUI>();
                
                if (effectText != null)
                {
                    effectText.text = $"{effect.RemainingTurns}";
                }
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