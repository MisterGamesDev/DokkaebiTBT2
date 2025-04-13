using UnityEngine;
using TMPro;
using Dokkaebi.Grid;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Grid
{
    /// <summary>
    /// Creates visible labels showing grid coordinates for testing purposes.
    /// </summary>
    [RequireComponent(typeof(GridManager))]
    public class GridLabelManager : MonoBehaviour
    {
        [Header("Label Settings")]
        [SerializeField] private bool showLabels = true;
        [SerializeField] private Color textColor = Color.black;
        [SerializeField] private float textSize = 0.4f;
        [SerializeField] private float textHeight = 0.1f;
        [SerializeField] private TMP_FontAsset font;
        
        private GridManager gridManager;
        private GameObject labelContainer;
        
        // Public properties for runtime configuration
        public bool ShowLabels 
        { 
            get => showLabels; 
            set 
            { 
                showLabels = value;
                ToggleLabels(value);
            }
        }
        
        public Color TextColor
        {
            get => textColor;
            set
            {
                textColor = value;
                RefreshLabels();
            }
        }
        
        public float TextSize
        {
            get => textSize;
            set
            {
                textSize = value;
                RefreshLabels();
            }
        }
        
        public float TextHeight
        {
            get => textHeight;
            set
            {
                textHeight = value;
                RefreshLabels();
            }
        }
        
        public TMP_FontAsset Font
        {
            get => font;
            set
            {
                font = value;
                RefreshLabels();
            }
        }
        
        private void Start()
        {
            Initialize();
        }
        
        /// <summary>
        /// Initialize the label manager and create labels if needed.
        /// </summary>
        public void Initialize()
        {
            if (gridManager == null)
            {
                gridManager = GetComponent<GridManager>();
            }
            
            if (showLabels && labelContainer == null)
            {
                CreateCoordinateLabels();
            }
        }
        
        /// <summary>
        /// Creates text labels for each grid cell showing its coordinates.
        /// </summary>
        public void CreateCoordinateLabels()
        {
            // Remove existing labels if any
            DestroyLabels();
            
            // Create new container
            labelContainer = new GameObject("GridLabels");
            labelContainer.transform.SetParent(transform, false);
            
            // Get grid dimensions from the GridManager
            int width = gridManager.GetGridWidth();
            int height = gridManager.GetGridHeight();
            
            // Create labels for each cell
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    // Create text at center of cell
                    CreateLabel(new GridPosition(x, z));
                }
            }
            
            Debug.Log($"Created {width * height} grid coordinate labels");
        }
        
        /// <summary>
        /// Creates a single coordinate label at the specified grid position.
        /// </summary>
        private void CreateLabel(GridPosition pos)
        {
            // Create game object for the label
            GameObject labelObj = new GameObject($"Label_{pos.x}_{pos.z}");
            labelObj.transform.SetParent(labelContainer.transform, false);
            
            // Set position (center of the cell, elevated by textHeight)
            Vector3 worldPos = DokkaebiGridConverter.GridToWorld(pos);
            // Adjust position to be centered in the cell
            worldPos.y += textHeight;
            labelObj.transform.position = worldPos;
            
            // Rotate to face upward
            labelObj.transform.rotation = Quaternion.Euler(90, 0, 0);
            
            // Add TextMeshPro component
            TextMeshPro textMesh = labelObj.AddComponent<TextMeshPro>();
            textMesh.text = $"({pos.x},{pos.z})";
            textMesh.fontSize = textSize;
            textMesh.color = textColor;
            
            // Improve text centering with both horizontal and vertical alignment
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.verticalAlignment = VerticalAlignmentOptions.Middle;
            
            // Set font asset if provided
            if (font != null)
            {
                textMesh.font = font;
            }
            
            // Configure RectTransform for better centering
            textMesh.rectTransform.sizeDelta = new Vector2(1, 1);
            textMesh.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            textMesh.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            textMesh.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            textMesh.enableWordWrapping = false;
            
            // Ensure text is rendered on top of other geometry
            textMesh.renderMode = TextRenderFlags.DontRender | TextRenderFlags.Render;
        }
        
        /// <summary>
        /// Toggle the visibility of coordinate labels.
        /// </summary>
        public void ToggleLabels(bool visible)
        {
            if (visible && gridManager != null)
            {
                if (labelContainer == null)
                {
                    CreateCoordinateLabels();
                }
                else
                {
                    labelContainer.SetActive(true);
                }
            }
            else if (labelContainer != null)
            {
                labelContainer.SetActive(visible);
            }
        }
        
        /// <summary>
        /// Destroy all existing labels.
        /// </summary>
        private void DestroyLabels()
        {
            Transform existingContainer = transform.Find("GridLabels");
            if (existingContainer != null)
            {
                Destroy(existingContainer.gameObject);
                labelContainer = null;
            }
        }
        
        /// <summary>
        /// Refresh all labels with current settings.
        /// </summary>
        public void RefreshLabels()
        {
            if (labelContainer != null && labelContainer.activeSelf)
            {
                CreateCoordinateLabels();
            }
        }
    }
} 
