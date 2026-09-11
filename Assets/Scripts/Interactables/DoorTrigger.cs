using UnityEngine;
using UnityEngine.Tilemaps;

public class DoorTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        GameManager manager = GameManager.Instance;
        if (manager == null) return;

        Tilemap objectTilemap = manager.DoorTilemap;
        if (objectTilemap == null) return;

        Vector3Int cell = objectTilemap.WorldToCell(other.transform.position);
        if (manager.TryResolveDoor(cell, out DoorType doorType))
        {
            if (doorType == DoorType.SpecialExitDoor)
            {
                EventBus<BossExitDoorTriggeredEvent>.Raise(new BossExitDoorTriggeredEvent());
                UnityEngine.SceneManagement.SceneManager.LoadScene("StartMenu");
                return;
            }

            GameManager.TriggerDoor(doorType, new Vector2Int(cell.x, cell.y));
        }
    }
}