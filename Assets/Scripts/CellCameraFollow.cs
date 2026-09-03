using UnityEngine;

public class CellCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Vector2Int macroCellSize = new Vector2Int(25, 20);
    [SerializeField] private DungeonGenerator dungeonGenerator;

    private Vector2Int currentCell = new Vector2Int(-1, -1);

    private void LateUpdate()
    {
        if (playerTransform == null) return;
        if (dungeonGenerator == null)
            dungeonGenerator = FindAnyObjectByType<DungeonGenerator>();

        // Calculate which macro cell the player is standing in
        int cellX = Mathf.FloorToInt(playerTransform.position.x / macroCellSize.x);
        int cellY = Mathf.FloorToInt(playerTransform.position.y / macroCellSize.y);

        Vector2Int newCell = new Vector2Int(cellX, cellY);

        // Snap camera center when entering a new cell
        if (newCell != currentCell)
        {
            currentCell = newCell;
            Vector3 targetPos;
            if (dungeonGenerator != null &&
                dungeonGenerator.TryGetRoomCenter(currentCell, out Vector3 roomCenter))
            {
                targetPos = roomCenter;
                targetPos.z = -10f;
            }
            else
            {
                targetPos = new Vector3(
                    (currentCell.x * macroCellSize.x) + (macroCellSize.x / 2f),
                    (currentCell.y * macroCellSize.y) + (macroCellSize.y / 2f),
                    -10f);
            }

            transform.position = targetPos;
        }
    }
}
