using UnityEngine;

namespace Dokkaebi.Camera
{
    /// <summary>
    /// Controls the camera movement and zoom for the Dokkaebi game
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 20f;
        [SerializeField] private float edgeScrollMargin = 20f;
        [SerializeField] private bool useEdgeScrolling = true;
        
        [Header("Zoom Settings")]
        [SerializeField] private float zoomSpeed = 10f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 20f;
        
        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 100f;
        [SerializeField] private bool allowRotation = true;
        
        [Header("Follow Settings")]
        [SerializeField] private bool followSelectedUnit = true;
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private Vector3 followOffset = new Vector3(0, 10, -10);
        
        private Transform cameraTransform;
        private Vector3 targetPosition;
        private float targetZoom;
        private bool isFollowingUnit = false;
        private Transform unitToFollow;
        
        private void Awake()
        {
            cameraTransform = transform;
            targetPosition = cameraTransform.position;
            targetZoom = cameraTransform.position.y;
        }
        
        private void Update()
        {
            HandleMovementInput();
            HandleZoomInput();
            HandleRotationInput();
            
            // Only update camera position if not following a unit
            if (!isFollowingUnit || unitToFollow == null)
            {
                // Apply movement
                Vector3 currentPos = cameraTransform.position;
                currentPos = Vector3.Lerp(currentPos, new Vector3(targetPosition.x, currentPos.y, targetPosition.z), Time.deltaTime * moveSpeed);
                
                // Apply zoom
                currentPos.y = Mathf.Lerp(currentPos.y, targetZoom, Time.deltaTime * zoomSpeed);
                
                cameraTransform.position = currentPos;
            }
            else
            {
                // Follow unit
                FollowUnit();
            }
        }
        
        /// <summary>
        /// Handle keyboard and mouse edge scrolling for camera movement
        /// </summary>
        private void HandleMovementInput()
        {
            // Return early if following a unit
            if (isFollowingUnit && unitToFollow != null)
                return;
                
            Vector3 moveDir = Vector3.zero;
            
            // Keyboard movement
            if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow))
                moveDir += transform.forward;
                
            if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow))
                moveDir -= transform.forward;
                
            if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow))
                moveDir -= transform.right;
                
            if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow))
                moveDir += transform.right;
                
            // Edge scrolling
            if (useEdgeScrolling)
            {
                Vector3 mousePos = UnityEngine.Input.mousePosition;
                
                if (mousePos.x < edgeScrollMargin)
                    moveDir -= transform.right;
                    
                if (mousePos.x > Screen.width - edgeScrollMargin)
                    moveDir += transform.right;
                    
                if (mousePos.y < edgeScrollMargin)
                    moveDir -= transform.forward;
                    
                if (mousePos.y > Screen.height - edgeScrollMargin)
                    moveDir += transform.forward;
            }
            
            // Normalize and apply movement
            if (moveDir.magnitude > 0)
            {
                moveDir.Normalize();
                targetPosition += moveDir * moveSpeed * Time.deltaTime;
            }
        }
        
        /// <summary>
        /// Handle mouse wheel for camera zoom
        /// </summary>
        private void HandleZoomInput()
        {
            float scrollWheel = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
            
            if (scrollWheel != 0)
            {
                targetZoom -= scrollWheel * zoomSpeed;
                targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            }
        }
        
        /// <summary>
        /// Handle camera rotation input
        /// </summary>
        private void HandleRotationInput()
        {
            if (!allowRotation)
                return;
                
            if (UnityEngine.Input.GetKey(KeyCode.Q))
            {
                transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime);
            }
            
            if (UnityEngine.Input.GetKey(KeyCode.E))
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
        }
        
        /// <summary>
        /// Follow a specific unit
        /// </summary>
        private void FollowUnit()
        {
            if (unitToFollow == null)
            {
                isFollowingUnit = false;
                return;
            }
            
            Vector3 targetPos = unitToFollow.position + followOffset;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
        }
        
        /// <summary>
        /// Set the unit to follow
        /// </summary>
        public void SetFollowTarget(Transform unit)
        {
            if (unit != null && followSelectedUnit)
            {
                unitToFollow = unit;
                isFollowingUnit = true;
            }
            else
            {
                unitToFollow = null;
                isFollowingUnit = false;
            }
        }
        
        /// <summary>
        /// Focus camera on a specific world position
        /// </summary>
        public void FocusOnWorldPosition(Vector3 worldPosition)
        {
            // Stop following any unit
            isFollowingUnit = false;
            unitToFollow = null;
            
            // Set target position
            targetPosition = worldPosition;
        }
        
        /// <summary>
        /// Stop following the current unit
        /// </summary>
        public void StopFollowing()
        {
            isFollowingUnit = false;
            unitToFollow = null;
        }
    }
}