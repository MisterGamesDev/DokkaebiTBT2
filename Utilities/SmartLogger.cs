using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics; // Add Conditional attribute support
using Debug = UnityEngine.Debug; // Explicit Debug reference to avoid ambiguity

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
        Game,
        StateSync,
        UI,          // Added for UI-related logs
        Network,     // Added for networking-related logs
        Physics,     // Added for physics-related logs
        Audio,       // Added for audio-related logs
        Debug,       // Added for temporary debug logs that should be stripped in release
        Pathfinding, // Added for pathfinding-related logs
        Input        // Added for input-related logs
    }

    /// <summary>
    /// Smart logging system with categorization, filtering capabilities, and conditional compilation support
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
            
            // Disable Debug category by default in non-development builds
            #if !DEVELOPMENT_BUILD && !UNITY_EDITOR
            enabledCategories[LogCategory.Debug] = false;
            #endif
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
        /// Log a message with a specific category and optional context object
        /// </summary>
        [Conditional("ENABLE_LOGGING")]
        public static void Log(string message, LogCategory category = LogCategory.General, Object context = null)
        {
            if (!globallyEnabled || !enabledCategories.TryGetValue(category, out bool isEnabled) || !isEnabled)
                return;
                
            Debug.Log($"[{category}] {message}", context);
        }

        /// <summary>
        /// Log a warning with a specific category and optional context object
        /// </summary>
        [Conditional("ENABLE_LOGGING")]
        public static void LogWarning(string message, LogCategory category = LogCategory.General, Object context = null)
        {
            if (!globallyEnabled || !enabledCategories.TryGetValue(category, out bool isEnabled) || !isEnabled)
                return;
                
            Debug.LogWarning($"[{category}] {message}", context);
        }

        /// <summary>
        /// Log an error with a specific category and optional context object.
        /// Note: Errors are not stripped in release builds by default for critical error tracking.
        /// </summary>
        public static void LogError(string message, LogCategory category = LogCategory.General, Object context = null)
        {
            if (!globallyEnabled || !enabledCategories.TryGetValue(category, out bool isEnabled) || !isEnabled)
                return;
                
            Debug.LogError($"[{category}] {message}", context);
        }

        /// <summary>
        /// Log using a StringBuilder for efficient string operations
        /// </summary>
        [Conditional("ENABLE_LOGGING")]
        public static void LogWithBuilder(System.Text.StringBuilder builder, LogCategory category = LogCategory.General, Object context = null)
        {
            if (!globallyEnabled || !enabledCategories.TryGetValue(category, out bool isEnabled) || !isEnabled)
                return;
                
            Debug.Log($"[{category}] {builder}", context);
        }

        /// <summary>
        /// Formats an exception with its stack trace for logging
        /// </summary>
        public static string FormatException(System.Exception ex)
        {
            return $"{ex.GetType().Name}: {ex.Message}\nStack Trace:\n{ex.StackTrace}";
        }
    }
}
