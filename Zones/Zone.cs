using UnityEngine;

namespace Dokkaebi.Zones
{
    /// <summary>
    /// Represents a gameplay zone on the grid
    /// </summary>
    public class Zone : MonoBehaviour
    {
        // Identification
        public string ZoneId { get; private set; }
        public string ZoneType { get; private set; }
        
        // Positioning
        public Vector2Int Position { get; private set; }
        public int Size { get; private set; }
        
        // Lifecycle
        public int RemainingDuration { get; private set; }
        public string OwnerUnitId { get; private set; }
        
        /// <summary>
        /// Initialize zone data
        /// </summary>
        public void Initialize(string id, string type, Vector2Int position, int size, int duration, string ownerUnitId)
        {
            ZoneId = id;
            ZoneType = type;
            Position = position;
            Size = size;
            RemainingDuration = duration;
            OwnerUnitId = ownerUnitId;
        }
        
        /// <summary>
        /// Set zone position
        /// </summary>
        public void SetPosition(Vector2Int newPosition)
        {
            Position = newPosition;
            transform.position = new Vector3(newPosition.x, 0, newPosition.y);
        }
        
        /// <summary>
        /// Set zone duration
        /// </summary>
        public void SetDuration(int duration)
        {
            RemainingDuration = duration;
        }
        
        /// <summary>
        /// Reduce duration by one turn
        /// </summary>
        public void DecrementDuration()
        {
            RemainingDuration = Mathf.Max(0, RemainingDuration - 1);
        }
        
        /// <summary>
        /// Check if zone has expired
        /// </summary>
        public bool IsExpired()
        {
            return RemainingDuration <= 0;
        }
    }
} 