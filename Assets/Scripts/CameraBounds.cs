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
        SetRoomBounds(room.CenterPosition, room.Size.x, room.Size.y);
    }

    public void SetRoomBounds(Vector3 roomCenter, float roomWidth, float roomHeight)
    {
        roomWidth += 4f;
        roomHeight += 2f;
        float finalWidth = Mathf.Max(roomWidth, camHorizSize);
        float finalHeight = Mathf.Max(roomHeight, camVertSize);

        Debug.Log($"Camera bounds set for room {0} at position {roomCenter} with with {roomWidth} and height {roomHeight}");

        boundsCollider.transform.position = roomCenter;
        boundsCollider.size = new Vector2(finalWidth, finalHeight);

        if (confiner != null)
        {
            confiner.InvalidateBoundingShapeCache();

            CinemachineCamera vcam = confiner.GetComponent<CinemachineCamera>();
            if (vcam != null)
            {
                CinemachineFollow follow = vcam.GetComponent<CinemachineFollow>();
                if (follow != null)
                {
                    follow.enabled = false;
                }

                Vector3 targetCamPos = new Vector3(roomCenter.x, roomCenter.y, vcam.transform.position.z);
                vcam.ForceCameraPosition(targetCamPos, Quaternion.identity);

                if (vcam.Target.TrackingTarget != null)
                {
                    vcam.OnTargetObjectWarped(vcam.Target.TrackingTarget, Vector3.zero);
                }

                if (follow != null)
                {
                    follow.enabled = true;
                }
            }
        }
    }
}