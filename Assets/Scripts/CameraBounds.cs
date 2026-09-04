using UnityEngine;
using Unity.Cinemachine;

public class CameraBounds : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CinemachineConfiner2D confiner;

    private BoxCollider2D boundsCollider;
    private float camVertSize;
    private float camHorizSize;

    
    private void OnEnable()
    {
        GameManager.NewRoomEntered += OnNewRoomEntered;
    }

    private void OnDisable()
    {
        GameManager.NewRoomEntered -= OnNewRoomEntered;
    }

    public void Start()
    {
        boundsCollider = GetComponent<BoxCollider2D>();
        mainCamera = Camera.main;

        if (boundsCollider == null || mainCamera == null)
        {
            Debug.LogError("Missing required components on CameraBounds script.");
            return;
        }

        camVertSize = mainCamera.orthographicSize * 2f;
        camHorizSize = camVertSize * mainCamera.aspect;

        if (GameManager.Instance != null && GameManager.Instance.DungeonDictionary.TryGetValue(GameManager.Instance.CurrentRoomIndex, out GameManager.RoomData room))
        {
            OnNewRoomEntered(room);
        }
    }

    private void OnNewRoomEntered(GameManager.RoomData room)
    {
        if (boundsCollider == null || mainCamera == null) return;

        Vector3 center = new Vector3(room.WorldCenterTile.x, room.WorldCenterTile.y, transform.position.z);
        SetRoomBounds(center, room.Size.x, room.Size.y);
    }

    /// <summary>
    /// Call this function whenever a new room finishes generating.
    /// </summary>
    /// <param name="roomCenter">World position center of the generated room</param>
    /// <param name="roomWidth">Room width in world units (e.g. 25 tiles * tileSize)</param>
    /// <param name="roomHeight">Room height in world units (e.g. 20 tiles * tileSize)</param>

    public void SetRoomBounds(Vector3 roomCenter, float roomWidth, float roomHeight)
    {
        
        float finalWidth = Mathf.Max(roomWidth, camHorizSize);
        float finalHeight = Mathf.Max(roomHeight, camVertSize);

        boundsCollider.transform.position = roomCenter;
        boundsCollider.size = new Vector2(finalWidth, finalHeight);

        if (confiner != null)
        {
            confiner.InvalidateBoundingShapeCache();
        }
    }
}