using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Dokkaebi.Units;
using Dokkaebi.Core.Data;
using Dokkaebi.Interfaces;

namespace Dokkaebi.UI
{
    /// <summary>
    /// Handles UI elements for displaying unit status, cooldowns, and effects
    /// </summary>
    public class UnitStatusUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DokkaebiUnit unit;
        [SerializeField] private Transform statusEffectContainer;
        [SerializeField] private Transform cooldownContainer;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject statusEffectIconPrefab;
        [SerializeField] private GameObject cooldownDisplayPrefab;
        
        [Header("Settings")]
        [SerializeField] private Vector3 offsetFromUnit = new Vector3(0, 2.5f, 0);
        [SerializeField] private float spacing = 0.5f;
        [SerializeField] private float iconSize = 0.5f;
        
        // Status effect icon instances
        private Dictionary<StatusEffectType, GameObject> activeStatusIcons = new Dictionary<StatusEffectType, GameObject>();
        
        // Cooldown display instances
        private Dictionary<AbilityType, GameObject> abilityCooldownDisplays = new Dictionary<AbilityType, GameObject>();
        
        private void Awake()
        {
            // Get unit component if not assigned
            if (unit == null)
            {
                unit = GetComponent<DokkaebiUnit>();
            }
            
            // Create containers if not assigned
            if (statusEffectContainer == null)
            {
                GameObject container = new GameObject("StatusEffectContainer");
                container.transform.SetParent(transform);
                statusEffectContainer = container.transform;
            }
            
            if (cooldownContainer == null)
            {
                GameObject container = new GameObject("CooldownContainer");
                container.transform.SetParent(transform);
                cooldownContainer = container.transform;
            }
        }
        
        private void Start()
        {
            // Initialize positions
            statusEffectContainer.localPosition = offsetFromUnit;
            cooldownContainer.localPosition = offsetFromUnit + new Vector3(0, -spacing, 0);
        }
        
        private void LateUpdate()
        {
            if (unit == null) return;
            
            // Update status effect icons
            UpdateStatusEffects();
            
            // Update cooldown displays
            UpdateCooldowns();
            
            // Make UI face camera
            FaceCamera();
        }
        
        /// <summary>
        /// Update the status effect icons
        /// </summary>
        private void UpdateStatusEffects()
        {
            // Get current status effects
            List<StatusEffectInstance> currentEffects = unit.GetStatusEffects();
            
            // Remove any icons for effects that are no longer active
            List<StatusEffectType> effectsToRemove = new List<StatusEffectType>();
            
            foreach (var pair in activeStatusIcons)
            {
                bool stillActive = false;
                
                foreach (var effect in currentEffects)
                {
                    if (effect.effectType == pair.Key)
                    {
                        stillActive = true;
                        break;
                    }
                }
                
                if (!stillActive)
                {
                    effectsToRemove.Add(pair.Key);
                }
            }
            
            // Remove inactive effects
            foreach (var effectType in effectsToRemove)
            {
                if (activeStatusIcons.TryGetValue(effectType, out var icon))
                {
                    Destroy(icon);
                    activeStatusIcons.Remove(effectType);
                }
            }
            
            // Add or update active effects
            float xOffset = 0;
            
            foreach (var effect in currentEffects)
            {
                // Skip if null or None type
                if (effect == null || effect.effectType == StatusEffectType.None)
                    continue;
                    
                GameObject iconObj;
                bool isNew = false;
                
                // Create icon if doesn't exist
                if (!activeStatusIcons.TryGetValue(effect.effectType, out iconObj))
                {
                    if (statusEffectIconPrefab != null)
                    {
                        iconObj = Instantiate(statusEffectIconPrefab, statusEffectContainer);
                        
                        // Set up the icon with status effect data
                        Image iconImage = iconObj.GetComponentInChildren<Image>();
                        if (iconImage != null && effect.effectData.icon != null)
                        {
                            iconImage.sprite = effect.effectData.icon;
                        }
                        
                        // Add to tracked icons
                        activeStatusIcons[effect.effectType] = iconObj;
                        isNew = true;
                    }
                }
                
                if (iconObj != null)
                {
                    // Update position if new
                    if (isNew)
                    {
                        iconObj.transform.localPosition = new Vector3(xOffset, 0, 0);
                        xOffset += spacing;
                    }
                    
                    // Update duration text if available
                    if (!effect.isPermanent)
                    {
                        TextMeshProUGUI durationText = iconObj.GetComponentInChildren<TextMeshProUGUI>();
                        if (durationText != null)
                        {
                            durationText.text = effect.remainingDuration.ToString();
                        }
                    }
                    
                    // Update stack count if more than 1
                    if (effect.stacks > 1)
                    {
                        // Find stack count text component
                        TextMeshProUGUI stackText = null;
                        foreach (var text in iconObj.GetComponentsInChildren<TextMeshProUGUI>())
                        {
                            if (text.gameObject.name.Contains("Stack"))
                            {
                                stackText = text;
                                break;
                            }
                        }
                        
                        if (stackText != null)
                        {
                            stackText.text = effect.stacks.ToString();
                            stackText.gameObject.SetActive(true);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Update the cooldown displays
        /// </summary>
        private void UpdateCooldowns()
        {
            // Get current abilities
            List<AbilityData> abilities = unit.GetAbilities();
            
            float xOffset = 0;
            
            foreach (var ability in abilities)
            {
                if (ability == null) continue;
                
                int cooldown = unit.GetRemainingCooldown(ability.abilityType);
                
                if (cooldown <= 0)
                {
                    // Remove cooldown display if not on cooldown
                    if (abilityCooldownDisplays.TryGetValue(ability.abilityType, out var display))
                    {
                        Destroy(display);
                        abilityCooldownDisplays.Remove(ability.abilityType);
                    }
                    continue;
                }
                
                // Create or update cooldown display
                GameObject displayObj;
                bool isNew = false;
                
                if (!abilityCooldownDisplays.TryGetValue(ability.abilityType, out displayObj))
                {
                    if (cooldownDisplayPrefab != null)
                    {
                        displayObj = Instantiate(cooldownDisplayPrefab, cooldownContainer);
                        
                        // Add icon if available
                        Image iconImage = displayObj.GetComponentInChildren<Image>();
                        if (iconImage != null && ability.icon != null)
                        {
                            iconImage.sprite = ability.icon;
                        }
                        
                        abilityCooldownDisplays[ability.abilityType] = displayObj;
                        isNew = true;
                    }
                }
                
                if (displayObj != null)
                {
                    // Update position if new
                    if (isNew)
                    {
                        displayObj.transform.localPosition = new Vector3(xOffset, 0, 0);
                        xOffset += spacing;
                    }
                    
                    // Update cooldown text
                    TextMeshProUGUI cooldownText = displayObj.GetComponentInChildren<TextMeshProUGUI>();
                    if (cooldownText != null)
                    {
                        cooldownText.text = cooldown.ToString();
                    }
                }
            }
        }
        
        /// <summary>
        /// Make the UI face the camera
        /// </summary>
        private void FaceCamera()
        {
            if (UnityEngine.Camera.main != null)
            {
                statusEffectContainer.rotation = UnityEngine.Camera.main.transform.rotation;
                cooldownContainer.rotation = UnityEngine.Camera.main.transform.rotation;
            }
        }
    }
} 