using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Dokkaebi.Units;
using Dokkaebi.Core.Data;
using Dokkaebi.Interfaces;
using Dokkaebi.Common;
using Dokkaebi.Utilities;
// using Dokkaebi.Events;
using System.Linq;
using Dokkaebi.Core;

namespace Dokkaebi.UI
{
    /// <summary>
    /// Handles UI elements for displaying unit status, cooldowns, and effects
    /// </summary>
    public class UnitStatusUI : MonoBehaviour
    {
        [Header("Health UI Components")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Aura UI Components")]
        [SerializeField] private Slider auraSlider;
        [SerializeField] private TextMeshProUGUI auraText;

        [Header("Status Effect UI")]
        [SerializeField] private Transform statusEffectContainer;
        [SerializeField] private GameObject statusEffectIconPrefab;

        [Header("Cooldown UI")]
        [SerializeField] private Transform cooldownContainer;
        [SerializeField] private GameObject cooldownDisplayPrefab;

        private IDokkaebiUnit unit;
        private Dictionary<string, GameObject> statusEffectIcons = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> cooldownDisplays = new Dictionary<string, GameObject>();

        private void OnEnable()
        {
            if (unit != null)
            {
                SubscribeToUnitEvents();
            }
        }

        private void OnDisable()
        {
            if (unit != null)
            {
                UnsubscribeFromUnitEvents();
            }
        }

        private void Awake()
        {
            SmartLogger.Log("[UnitStatusUI] Initializing...", LogCategory.UI, this);
            
            // Try to find IDokkaebiUnit component on the same GameObject
            var unitComponent = GetComponent<IDokkaebiUnit>();
            if (unitComponent != null)
            {
                SmartLogger.Log($"[UnitStatusUI.Awake] Found IDokkaebiUnit component on GameObject. Auto-linking to unit: {unitComponent.DisplayName}", LogCategory.UI, this);
                SetUnit(unitComponent);
            }
            else
            {
                SmartLogger.Log("[UnitStatusUI.Awake] No IDokkaebiUnit component found on GameObject. UI will need manual unit assignment.", LogCategory.UI, this);
            }
            
            InitializeComponents();
        }

        private void Start()
        {
            if (unit == null)
            {
                SmartLogger.LogWarning("[UnitStatusUI] No unit assigned on Start!", LogCategory.UI, this);
                return;
            }

            InitializeHealthDisplay();
            InitializeAuraDisplay();
            SmartLogger.Log("[UnitStatusUI] Initialization complete", LogCategory.UI, this);
        }

        private void InitializeComponents()
        {
            if (healthBar == null || healthText == null)
            {
                SmartLogger.LogError("[UnitStatusUI] Health UI components not assigned!", LogCategory.UI, this);
            }

            if (auraSlider == null || auraText == null)
            {
                SmartLogger.LogError("[UnitStatusUI] Aura UI components not assigned!", LogCategory.UI, this);
            }

            if (statusEffectContainer == null || statusEffectIconPrefab == null)
            {
                SmartLogger.LogError("[UnitStatusUI] Status effect UI components not assigned!", LogCategory.UI, this);
            }

            if (cooldownContainer == null || cooldownDisplayPrefab == null)
            {
                SmartLogger.LogError("[UnitStatusUI] Cooldown UI components not assigned!", LogCategory.UI, this);
            }
        }

        public void SetUnit(IDokkaebiUnit newUnit)
        {
            if (unit != null)
            {
                UnsubscribeFromUnitEvents();
            }

            unit = newUnit;
            
            if (unit != null)
            {
                if (unit is DokkaebiUnit concreteUnit)
                {
                    SmartLogger.Log($"[UnitStatusUI.SetUnit] Setting unit: {unit.DisplayName}, Health: {unit.CurrentHealth}/{concreteUnit.MaxHealth}", LogCategory.UI, this);
                }
                else
                {
                    SmartLogger.Log($"[UnitStatusUI.SetUnit] Setting unit: {unit.DisplayName}, Current Health: {unit.CurrentHealth} (MaxHealth unavailable - not a DokkaebiUnit)", LogCategory.UI, this);
                }
                
                SubscribeToUnitEvents();
                InitializeHealthDisplay();
                InitializeAuraDisplay();
                SmartLogger.Log($"[UnitStatusUI] Set new unit: {unit.DisplayName}", LogCategory.UI, this);
            }
            else
            {
                SmartLogger.LogWarning("[UnitStatusUI] Attempted to set null unit!", LogCategory.UI, this);
            }
        }

        private void SubscribeToUnitEvents()
        {
            if (unit == null) return;

            if (unit is IUnitEventHandler eventHandler)
            {
                eventHandler.OnDamageTaken += HandleUnitDamageTaken;
                eventHandler.OnHealingReceived += HandleUnitHealingReceived;
                eventHandler.OnStatusEffectApplied += HandleStatusEffectAdded;
                eventHandler.OnStatusEffectRemoved += HandleStatusEffectRemoved;
                SmartLogger.Log($"[{unit.DisplayName}] Subscribed to unit events", LogCategory.UI, this);
            }
            
            // Subscribe to unit-specific aura changes
            if (unit is DokkaebiUnit dokkaebiUnit)
            {
                dokkaebiUnit.OnUnitAuraChanged += HandleUnitAuraChanged;
                SmartLogger.Log($"[{unit.DisplayName}] Subscribed to unit aura changes", LogCategory.UI, this);
            }
        }

        private void UnsubscribeFromUnitEvents()
        {
            if (unit == null) return;

            if (unit is IUnitEventHandler eventHandler)
            {
                eventHandler.OnDamageTaken -= HandleUnitDamageTaken;
                eventHandler.OnHealingReceived -= HandleUnitHealingReceived;
                eventHandler.OnStatusEffectApplied -= HandleStatusEffectAdded;
                eventHandler.OnStatusEffectRemoved -= HandleStatusEffectRemoved;
                SmartLogger.Log($"[{unit.DisplayName}] Unsubscribed from unit events", LogCategory.UI, this);
            }
            
            // Unsubscribe from unit-specific aura changes
            if (unit is DokkaebiUnit dokkaebiUnit)
            {
                dokkaebiUnit.OnUnitAuraChanged -= HandleUnitAuraChanged;
                SmartLogger.Log($"[{unit.DisplayName}] Unsubscribed from unit aura changes", LogCategory.UI, this);
            }
        }

        private void InitializeHealthDisplay()
        {
            if (unit == null || healthBar == null) return;

            if (unit is IUnit baseUnit)
            {
                healthBar.maxValue = Mathf.Max(1, baseUnit.MaxHealth);
                healthBar.value = Mathf.Clamp(baseUnit.CurrentHealth, 0, baseUnit.MaxHealth);
                SmartLogger.Log($"[UnitStatusUI.InitializeHealthDisplay] Unit: {unit.DisplayName}, Health: {baseUnit.CurrentHealth}/{baseUnit.MaxHealth}, HealthBar: {healthBar.value}/{healthBar.maxValue}", LogCategory.UI, this);

                UpdateHealthText();
            }
        }

        private void HandleUnitDamageTaken(int amount, DamageType damageType)
        {
            if (unit == null)
            {
                SmartLogger.LogWarning("[UnitStatusUI.HandleUnitDamageTaken] Event received but unit is null!", LogCategory.UI, this);
                return;
            }

            SmartLogger.Log($"[UnitStatusUI.HandleUnitDamageTaken] Event received for {unit.DisplayName} - Amount: {amount}, Type: {damageType}", LogCategory.UI, this);
            
            if (unit is IUnit baseUnit)
            {
                SmartLogger.Log($"[UnitStatusUI.HandleUnitDamageTaken] Current unit state - Health: {baseUnit.CurrentHealth}/{baseUnit.MaxHealth}", LogCategory.UI, this);
            }
            
            UpdateHealthDisplay();
        }

        private void HandleUnitHealingReceived(int healAmount)
        {
            if (unit == null)
            {
                SmartLogger.LogWarning("[UnitStatusUI.HandleUnitHealingReceived] Event received but unit is null!", LogCategory.UI, this);
                return;
            }

            SmartLogger.Log($"[UnitStatusUI.HandleUnitHealingReceived] Event received for {unit.DisplayName} - Amount: {healAmount}", LogCategory.UI, this);
            
            if (unit is IUnit baseUnit)
            {
                SmartLogger.Log($"[UnitStatusUI.HandleUnitHealingReceived] Current unit state - Health: {baseUnit.CurrentHealth}/{baseUnit.MaxHealth}", LogCategory.UI, this);
            }
            
            UpdateHealthDisplay();
        }

        private void HandleStatusEffectAdded(IStatusEffectInstance effect)
        {
            if (unit == null || statusEffectContainer == null || statusEffectIconPrefab == null) return;

            string effectId = effect.StatusEffectType.ToString();
            if (!statusEffectIcons.ContainsKey(effectId))
            {
                GameObject newIcon = Instantiate(statusEffectIconPrefab, statusEffectContainer);
                statusEffectIcons[effectId] = newIcon;
                SmartLogger.Log($"[{unit.DisplayName}] Added status effect icon for {effectId}", LogCategory.UI, this);
            }
        }

        private void HandleStatusEffectRemoved(IStatusEffectInstance effect)
        {
            string effectId = effect.StatusEffectType.ToString();
            if (statusEffectIcons.TryGetValue(effectId, out GameObject icon))
            {
                Destroy(icon);
                statusEffectIcons.Remove(effectId);
                SmartLogger.Log($"[{unit?.DisplayName}] Removed status effect icon for {effectId}", LogCategory.UI, this);
            }
        }

        private void InitializeAuraDisplay()
        {
            if (unit == null || auraSlider == null) return;

            if (unit is DokkaebiUnit dokkaebiUnit)
            {
                int maxUnitAura = dokkaebiUnit.GetMaxUnitAura();
                int currentUnitAura = dokkaebiUnit.GetCurrentUnitAura();
                
                auraSlider.maxValue = Mathf.Max(1, maxUnitAura);
                auraSlider.value = Mathf.Clamp(currentUnitAura, 0, maxUnitAura);
                
                SmartLogger.Log($"[UnitStatusUI.InitializeAuraDisplay] Unit: {unit.DisplayName}, Aura: {currentUnitAura}/{maxUnitAura}, Slider: {auraSlider.value}/{auraSlider.maxValue}, Active: {auraSlider.gameObject.activeSelf}", LogCategory.UI, this);

                UpdateAuraText();
            }
            else
            {
                SmartLogger.LogWarning($"[UnitStatusUI.InitializeAuraDisplay] Unit {unit.DisplayName} is not a DokkaebiUnit - cannot initialize aura display", LogCategory.UI, this);
            }
        }

        private void UpdateAuraText()
        {
            if (auraText == null || unit == null) return;

            if (unit is DokkaebiUnit dokkaebiUnit)
            {
                int currentUnitAura = dokkaebiUnit.GetCurrentUnitAura();
                int maxUnitAura = dokkaebiUnit.GetMaxUnitAura();
                
                SmartLogger.Log($"[UnitStatusUI.UpdateAuraText] About to update text for {unit.DisplayName} - Current: {currentUnitAura}, Max: {maxUnitAura}", LogCategory.UI, this);
                auraText.text = $"{currentUnitAura}/{maxUnitAura}";
            }
            else
            {
                SmartLogger.LogWarning($"[UnitStatusUI.UpdateAuraText] Unit {unit.DisplayName} is not a DokkaebiUnit - cannot update aura text", LogCategory.UI, this);
            }
        }

        private void UpdateHealthDisplay()
        {
            if (unit == null || healthBar == null) return;

            if (unit is IUnit baseUnit)
            {
                SmartLogger.Log($"[UnitStatusUI.UpdateHealthDisplay] Starting update for {unit.DisplayName} - Current: {baseUnit.CurrentHealth}, Max: {baseUnit.MaxHealth}", LogCategory.UI, this);
                
                healthBar.maxValue = Mathf.Max(1, baseUnit.MaxHealth);
                healthBar.value = Mathf.Clamp(baseUnit.CurrentHealth, 0, baseUnit.MaxHealth);
                
                SmartLogger.Log($"[UnitStatusUI.UpdateHealthDisplay] Set healthBar values - maxValue: {healthBar.maxValue}, value: {healthBar.value}, gameObject active: {healthBar.gameObject.activeSelf}, component enabled: {healthBar.enabled}", LogCategory.UI, this);
                
                UpdateHealthText();
            }
        }

        private void UpdateHealthText()
        {
            if (unit == null || healthText == null) return;

            if (unit is IUnit baseUnit)
            {
                string newText = $"{baseUnit.CurrentHealth}/{baseUnit.MaxHealth}";
                healthText.text = newText;
                SmartLogger.Log($"[UnitStatusUI.UpdateHealthText] Updated text for {unit.DisplayName} to '{newText}', gameObject active: {healthText.gameObject.activeSelf}, component enabled: {healthText.enabled}", LogCategory.UI, this);
            }
        }

        private void UpdateAuraDisplay()
        {
            if (unit == null || auraSlider == null) return;

            if (unit is DokkaebiUnit dokkaebiUnit)
            {
                int maxUnitAura = dokkaebiUnit.GetMaxUnitAura();
                int currentUnitAura = dokkaebiUnit.GetCurrentUnitAura();
                
                SmartLogger.Log($"[UnitStatusUI.UpdateAuraDisplay] Starting update for {unit.DisplayName} - Current: {currentUnitAura}, Max: {maxUnitAura}", LogCategory.UI, this);
                
                auraSlider.maxValue = Mathf.Max(1, maxUnitAura);
                auraSlider.value = Mathf.Clamp(currentUnitAura, 0, maxUnitAura);
                
                SmartLogger.Log($"[UnitStatusUI.UpdateAuraDisplay] Set auraSlider values - maxValue: {auraSlider.maxValue}, value: {auraSlider.value}, gameObject active: {auraSlider.gameObject.activeSelf}, component enabled: {auraSlider.enabled}", LogCategory.UI, this);
                
                UpdateAuraText();
            }
            else
            {
                SmartLogger.LogWarning($"[UnitStatusUI.UpdateAuraDisplay] Unit {unit.DisplayName} is not a DokkaebiUnit - cannot update aura display", LogCategory.UI, this);
            }
        }

        private void HandleUnitAuraChanged(int oldAura, int newAura)
        {
            if (unit == null)
            {
                SmartLogger.LogWarning("[UnitStatusUI.HandleUnitAuraChanged] Event received but unit is null!", LogCategory.UI, this);
                return;
            }

            SmartLogger.Log($"[UnitStatusUI.HandleUnitAuraChanged] Event received for {unit.DisplayName} - Old: {oldAura}, New: {newAura}", LogCategory.UI, this);
            
            if (unit is DokkaebiUnit dokkaebiUnit)
            {
                int maxAura = dokkaebiUnit.GetMaxUnitAura();
                SmartLogger.Log($"[UnitStatusUI.HandleUnitAuraChanged] Current unit state - Aura: {newAura}/{maxAura}", LogCategory.UI, this);
                UpdateAuraDisplay();
            }
            else
            {
                SmartLogger.LogWarning($"[UnitStatusUI.HandleUnitAuraChanged] Unit {unit.DisplayName} is not a DokkaebiUnit - cannot handle aura change", LogCategory.UI, this);
            }
        }

        private void LateUpdate()
        {
            if (unit == null) return;
            
            // Make UI face camera
            FaceCamera();
        }
        
        private void FaceCamera()
        {
            if (UnityEngine.Camera.main != null)
            {
                transform.forward = UnityEngine.Camera.main.transform.forward;
            }
        }
    }
} 
