using System.Collections.Generic;
using UnityEngine;

namespace Dokkaebi.Utilities
{
    /// <summary>
    /// Log categories for the Dokkaebi game
    /// </summary>
    public enum LogCategory
    {
        General,
        TurnSystem,
        Movement,
        Ability,
        Zone,
        AI,
        Performance,
        Grid,
        Unit,
        Game
    }

    /// <summary>
    /// Smart logging system with categorization and filtering capabilities
    /// </summary>
    public static class SmartLogger
    {
        // Track which categories are enabled
        private static Dictionary<LogCategory, bool> enabledCategories = new Dictionary<LogCategory, bool>();
        
        // Track if smart logging is enabled globally
        private static bool globallyEnabled = true;

        // Initialize defaults
        static SmartLogger()
        {
            // Default all categories to enabled
            foreach (LogCategory category in System.Enum.GetValues(typeof(LogCategory)))
            {
                enabledCategories[category] = true;
            }
        }

        /// <summary>
        /// Enable or disable logging for a specific category
        /// </summary>
        public static void SetCategoryEnabled(LogCategory category, bool enabled)
        {
            enabledCategories[category] = enabled;
        }

        /// <summary>
        /// Enable or disable all logging categories
        /// </summary>
        public static void SetAllCategoriesEnabled(bool enabled)
        {
            foreach (LogCategory category in System.Enum.GetValues(typeof(LogCategory)))
            {
                enabledCategories[category] = enabled;
            }
        }

        /// <summary>
        /// Enable or disable smart logging globally
        /// </summary>
        public static void SetGloballyEnabled(bool enabled)
        {
            globallyEnabled = enabled;
        }

        /// <summary>
        /// Log a message with a specific category
        /// </summary>
        public static void Log(string message, LogCategory category = LogCategory.General)
        {
            if (!globallyEnabled || !enabledCategories.TryGetValue(category, out bool isEnabled) || !isEnabled)
                return;
                
            Debug.Log($"[{category}] {message}");
        }

        /// <summary>
        /// Log a warning with a specific category
        /// </summary>
        public static void LogWarning(string message, LogCategory category = LogCategory.General)
        {
            if (!globallyEnabled || !enabledCategories.TryGetValue(category, out bool isEnabled) || !isEnabled)
                return;
                
            Debug.LogWarning($"[{category}] {message}");
        }

        /// <summary>
        /// Log an error with a specific category
        /// </summary>
        public static void LogError(string message, LogCategory category = LogCategory.General)
        {
            if (!globallyEnabled || !enabledCategories.TryGetValue(category, out bool isEnabled) || !isEnabled)
                return;
                
            Debug.LogError($"[{category}] {message}");
        }

        /// <summary>
        /// Log using a StringBuilder for efficient string operations
        /// </summary>
        public static void LogWithBuilder(System.Text.StringBuilder builder, LogCategory category = LogCategory.General)
        {
            if (!globallyEnabled || !enabledCategories.TryGetValue(category, out bool isEnabled) || !isEnabled)
                return;
                
            Debug.Log($"[{category}] {builder.ToString()}");
        }
    }
}