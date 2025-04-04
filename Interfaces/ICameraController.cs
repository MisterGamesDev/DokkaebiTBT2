using UnityEngine;

namespace Dokkaebi.Interfaces
{
    /// <summary>
    /// Interface for camera controller to break cyclic dependencies
    /// </summary>
    public interface ICameraController
    {
        /// <summary>
        /// Set a transform for the camera to follow
        /// </summary>
        void SetFollowTarget(Transform target);
        
        /// <summary>
        /// Focus the camera on a specific world position
        /// </summary>
        void FocusOnWorldPosition(Vector3 worldPosition);
        
        /// <summary>
        /// Stop following any target
        /// </summary>
        void StopFollowing();
    }
} 