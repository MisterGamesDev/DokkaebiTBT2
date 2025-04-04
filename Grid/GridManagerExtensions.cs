using UnityEngine;

namespace Dokkaebi.Grid
{
    /// <summary>
    /// Extension methods for GridManager to add functionality for testing and debugging.
    /// </summary>
    public static class GridManagerExtensions
    {
        /// <summary>
        /// Adds and configures a GridLabelManager to display coordinate labels.
        /// </summary>
        public static GridLabelManager EnableCoordinateLabels(this GridManager gridManager, 
            bool enabled = true,
            Color? textColor = null,
            float textSize = 0.4f,
            float textHeight = 0.1f)
        {
            // Check if there's already a GridLabelManager
            var labelManager = gridManager.GetComponent<GridLabelManager>();
            
            // If no label manager exists, add one
            if (labelManager == null)
            {
                labelManager = gridManager.gameObject.AddComponent<GridLabelManager>();
                
                // Configure settings
                if (textColor.HasValue)
                {
                    labelManager.TextColor = textColor.Value;
                }
                
                labelManager.TextSize = textSize;
                labelManager.TextHeight = textHeight;
                labelManager.ShowLabels = enabled;
                labelManager.Initialize();
            }
            else
            {
                // Update settings on existing label manager
                if (textColor.HasValue)
                {
                    labelManager.TextColor = textColor.Value;
                }
                
                labelManager.TextSize = textSize;
                labelManager.TextHeight = textHeight;
                labelManager.ShowLabels = enabled;
            }
            
            return labelManager;
        }
        
        /// <summary>
        /// Toggles coordinate labels on/off.
        /// </summary>
        public static void ToggleCoordinateLabels(this GridManager gridManager, bool enabled)
        {
            var labelManager = gridManager.GetComponent<GridLabelManager>();
            
            if (labelManager != null)
            {
                labelManager.ShowLabels = enabled;
            }
            else if (enabled)
            {
                // If we need to enable but don't have the component, add it
                gridManager.EnableCoordinateLabels(enabled);
            }
        }
    }
} 