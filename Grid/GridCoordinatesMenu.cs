using UnityEngine;
using UnityEditor;
using Dokkaebi.Grid;

#if UNITY_EDITOR
namespace Dokkaebi.Editor
{
    /// <summary>
    /// Provides menu items for adding and toggling grid coordinate labels.
    /// </summary>
    public static class GridCoordinatesMenu
    {
        [MenuItem("Dokkaebi/Grid/Show Coordinate Labels")]
        private static void ShowCoordinateLabels()
        {
            GridManager gridManager = Object.FindObjectOfType<GridManager>();
            if (gridManager != null)
            {
                gridManager.EnableCoordinateLabels(true, Color.black);
                Debug.Log("Grid coordinate labels enabled.");
            }
            else
            {
                Debug.LogWarning("No GridManager found in the scene.");
            }
        }
        
        [MenuItem("Dokkaebi/Grid/Hide Coordinate Labels")]
        private static void HideCoordinateLabels()
        {
            GridManager gridManager = Object.FindObjectOfType<GridManager>();
            if (gridManager != null)
            {
                gridManager.ToggleCoordinateLabels(false);
                Debug.Log("Grid coordinate labels hidden.");
            }
            else
            {
                Debug.LogWarning("No GridManager found in the scene.");
            }
        }
        
        [MenuItem("Dokkaebi/Grid/Toggle Coordinate Labels")]
        private static void ToggleCoordinateLabels()
        {
            GridManager gridManager = Object.FindObjectOfType<GridManager>();
            if (gridManager != null)
            {
                GridLabelManager labelManager = gridManager.GetComponent<GridLabelManager>();
                bool currentState = labelManager != null && labelManager.ShowLabels;
                
                gridManager.ToggleCoordinateLabels(!currentState);
                Debug.Log($"Grid coordinate labels {(!currentState ? "enabled" : "disabled")}.");
            }
            else
            {
                Debug.LogWarning("No GridManager found in the scene.");
            }
        }
        
        [MenuItem("Dokkaebi/Grid/Label Sizes/Small (0.3)")]
        private static void SetSmallLabelSize()
        {
            SetLabelSize(0.3f);
        }
        
        [MenuItem("Dokkaebi/Grid/Label Sizes/Medium (0.6)")]
        private static void SetMediumLabelSize()
        {
            SetLabelSize(0.6f);
        }
        
        [MenuItem("Dokkaebi/Grid/Label Sizes/Large (1.0)")]
        private static void SetLargeLabelSize()
        {
            SetLabelSize(1.0f);
        }
        
        [MenuItem("Dokkaebi/Grid/Label Sizes/X-Large (1.5)")]
        private static void SetXLargeLabelSize()
        {
            SetLabelSize(1.5f);
        }
        
        [MenuItem("Dokkaebi/Grid/Label Sizes/XX-Large (2.0)")]
        private static void SetXXLargeLabelSize()
        {
            SetLabelSize(2.0f);
        }
        
        private static void SetLabelSize(float size)
        {
            GridManager gridManager = Object.FindObjectOfType<GridManager>();
            if (gridManager != null)
            {
                GridLabelManager labelManager = gridManager.GetComponent<GridLabelManager>();
                if (labelManager != null)
                {
                    labelManager.TextSize = size;
                    Debug.Log($"Grid label size set to {size}");
                }
                else
                {
                    gridManager.EnableCoordinateLabels(true, Color.black, size);
                    Debug.Log($"Grid labels created with size {size}");
                }
            }
            else
            {
                Debug.LogWarning("No GridManager found in the scene.");
            }
        }
        
        [MenuItem("Dokkaebi/Grid/Adjust Label Settings")]
        private static void ShowAdjustLabelSizeWindow()
        {
            GridLabelSizeWindow.ShowWindow();
        }
    }
    
    /// <summary>
    /// Editor window for adjusting grid label size.
    /// </summary>
    public class GridLabelSizeWindow : EditorWindow
    {
        private float textSize = 0.4f;
        private float textHeight = 0.1f;
        private Color textColor = Color.black;
        
        public static void ShowWindow()
        {
            // Get existing open window or create a new one
            GridLabelSizeWindow window = EditorWindow.GetWindow<GridLabelSizeWindow>("Grid Label Settings");
            window.minSize = new Vector2(300, 150);
            
            // Initialize with current settings if available
            GridManager gridManager = Object.FindObjectOfType<GridManager>();
            if (gridManager != null)
            {
                GridLabelManager labelManager = gridManager.GetComponent<GridLabelManager>();
                if (labelManager != null)
                {
                    window.textSize = labelManager.TextSize;
                    window.textHeight = labelManager.TextHeight;
                    window.textColor = labelManager.TextColor;
                }
            }
            
            window.Show();
        }
        
        private void OnGUI()
        {
            GridManager gridManager = Object.FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                EditorGUILayout.HelpBox("No GridManager found in the scene.", MessageType.Warning);
                return;
            }
            
            EditorGUILayout.LabelField("Grid Label Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Text size slider
            EditorGUI.BeginChangeCheck();
            textSize = EditorGUILayout.Slider("Text Size", textSize, 0.2f, 3.0f);
            textHeight = EditorGUILayout.Slider("Text Height", textHeight, 0.05f, 1.0f);
            textColor = EditorGUILayout.ColorField("Text Color", textColor);
            
            if (EditorGUI.EndChangeCheck())
            {
                GridLabelManager labelManager = gridManager.GetComponent<GridLabelManager>();
                if (labelManager != null)
                {
                    labelManager.TextSize = textSize;
                    labelManager.TextHeight = textHeight;
                    labelManager.TextColor = textColor;
                }
                else
                {
                    gridManager.EnableCoordinateLabels(true, textColor, textSize, textHeight);
                }
            }
            
            EditorGUILayout.Space();
            
            // Quick presets
            EditorGUILayout.LabelField("Quick Size Presets", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Small (0.3)"))
            {
                textSize = 0.3f;
                ApplySettings(gridManager);
            }
            
            if (GUILayout.Button("Medium (0.6)"))
            {
                textSize = 0.6f;
                ApplySettings(gridManager);
            }
            
            if (GUILayout.Button("Large (1.0)"))
            {
                textSize = 1.0f;
                ApplySettings(gridManager);
            }
            
            if (GUILayout.Button("X-Large (1.5)"))
            {
                textSize = 1.5f;
                ApplySettings(gridManager);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Toggle visibility
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Show Labels"))
            {
                gridManager.ToggleCoordinateLabels(true);
            }
            
            if (GUILayout.Button("Hide Labels"))
            {
                gridManager.ToggleCoordinateLabels(false);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void ApplySettings(GridManager gridManager)
        {
            GridLabelManager labelManager = gridManager.GetComponent<GridLabelManager>();
            if (labelManager != null)
            {
                labelManager.TextSize = textSize;
                labelManager.TextHeight = textHeight;
                labelManager.TextColor = textColor;
            }
            else
            {
                gridManager.EnableCoordinateLabels(true, textColor, textSize, textHeight);
            }
        }
    }
}
#endif 