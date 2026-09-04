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

        confiner.InvalidateBoundingShapeCache();
    }
}