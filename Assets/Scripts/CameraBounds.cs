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
        EventBus<RoomEnteredEvent>.Subscribe(OnRoomEntered);
    }

    private void OnDisable()
    {
        EventBus<RoomEnteredEvent>.Unsubscribe(OnRoomEntered);
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
            ApplyRoomBounds(room);
        }
    }

    private void OnRoomEntered(RoomEnteredEvent evt)
    {
        ApplyRoomBounds(evt.Room);
    }

    private void ApplyRoomBounds(GameManager.RoomData room)
    {
        if (boundsCollider == null || mainCamera == null || room == null) return;

        Vector3 center = new Vector3(room.WorldCenterTile.x, room.WorldCenterTile.y, transform.position.z);
        SetRoomBounds(center, room.Size.x, room.Size.y);
    }

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