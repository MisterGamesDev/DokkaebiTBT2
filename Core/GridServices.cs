using UnityEngine;

namespace Dokkaebi.Core
{
    /// <summary>
    /// Connects the Grid and Pathfinding systems through their interfaces.
    /// This allows the lower-level Grid system to work with pathfinding
    /// without directly referencing the Pathfinding implementations.
    /// </summary>
    public class GridServices : MonoBehaviour
    {
        private static GridServices instance;
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("More than one GridServices detected. This instance will be destroyed.");
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            
            // Initialize Grid systems
            Debug.Log("Grid services initialized successfully");
        }
    }
} 
