using UnityEngine;
using System.Collections.Generic;

namespace Dokkaebi.Grid
{
    public class GridVisualizer : MonoBehaviour {
        public int gridWidth = 10;
        public int gridHeight = 10;
        public float cellSize = 1.0f;
        public Color lineColor = Color.black;
        public bool showInGameView = true;
        public float lineWidth = 0.02f;
        public Material lineMaterial;
        
        private List<LineRenderer> lineRenderers = new List<LineRenderer>();
        
        void OnDrawGizmos() {
            Gizmos.color = lineColor;
            
            // Draw horizontal lines
            for (int i = 0; i <= gridHeight; i++) {
                Vector3 start = new Vector3(0, 0.01f, i * cellSize);
                Vector3 end = new Vector3(gridWidth * cellSize, 0.01f, i * cellSize);
                Gizmos.DrawLine(start, end);
            }
            
            // Draw vertical lines
            for (int i = 0; i <= gridWidth; i++) {
                Vector3 start = new Vector3(i * cellSize, 0.01f, 0);
                Vector3 end = new Vector3(i * cellSize, 0.01f, gridHeight * cellSize);
                Gizmos.DrawLine(start, end);
            }
        }
        
        void Start() {
            if (showInGameView) {
                CreateRuntimeGrid();
            }
        }
        
        public void CreateRuntimeGrid() {
            // Clean up any existing line renderers
            foreach (var line in lineRenderers) {
                if (line != null) Destroy(line.gameObject);
            }
            lineRenderers.Clear();
            
            // Create horizontal lines
            for (int i = 0; i <= gridHeight; i++) {
                Vector3 start = new Vector3(0, 0.01f, i * cellSize);
                Vector3 end = new Vector3(gridWidth * cellSize, 0.01f, i * cellSize);
                CreateLine($"HLine_{i}", start, end);
            }
            
            // Create vertical lines
            for (int i = 0; i <= gridWidth; i++) {
                Vector3 start = new Vector3(i * cellSize, 0.01f, 0);
                Vector3 end = new Vector3(i * cellSize, 0.01f, gridHeight * cellSize);
                CreateLine($"VLine_{i}", start, end);
            }
        }
        
        private void CreateLine(string name, Vector3 start, Vector3 end) {
            GameObject lineObj = new GameObject(name);
            lineObj.transform.parent = transform;
            
            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
            line.startColor = lineColor;
            line.endColor = lineColor;
            
            lineRenderers.Add(line);
        }
        
        // Call this if you need to regenerate the grid (if size changes, etc.)
        public void RegenerateGrid() {
            if (showInGameView) {
                CreateRuntimeGrid();
            }
        }
    }
}