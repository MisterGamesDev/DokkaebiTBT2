using UnityEngine;

public class InputManager : MonoBehaviour
{
    public float raycastDistance = 100f;
    public LayerMask groundLayer;
    public GridManager gridManager;
    public PlayerActionManager playerActionManager;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // If no unit was hit, check for ground hit
            if (Physics.Raycast(ray, out hit, raycastDistance, groundLayer))
            {
                SmartLogger.Log($"No hit on unit layer. Checking ground hit at {hit.point}", LogCategory.General);
                GridPosition gridPos = gridManager.WorldToGrid(hit.point);
                Vector2Int vectorPos = gridPos.ToVector2Int();
                Debug.Log($"[InputManager] Ground Click: Hit World Pos={hit.point}, Calculated GridPos={gridPos}, Calculated Vector2Int={vectorPos}");
                playerActionManager.HandleGroundClick(vectorPos);
            }
        }
    }
} 