using UnityEngine;
using Unity.Cinemachine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class CameraBounds : MonoBehaviour
{
    [Header("Components")]
    private Camera mainCamera;

    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private CinemachineConfiner2D confiner;

    [SerializeField] private float cameraTransitionOffset;


    private BoxCollider2D boundsCollider;
    private float camVertSize;
    private float camHorizSize;

    private DoorType currentDoorType;

    
    private void OnEnable()
    {
        EventBus<RoomEnteredEvent>.Subscribe(OnRoomEntered);
        EventBus<DoorTriggeredEvent>.Subscribe(OnDoorTriggered);
        EventBus<GenerateDungeonEvent>.Subscribe(OnGenerateDungeon);
    }

    private void OnDisable()
    {
        EventBus<RoomEnteredEvent>.Unsubscribe(OnRoomEntered);
        EventBus<DoorTriggeredEvent>.Unsubscribe(OnDoorTriggered);
        EventBus<GenerateDungeonEvent>.Unsubscribe(OnGenerateDungeon);
    }

    private bool EnsureInitialized()
    {
        if (boundsCollider == null)
        {
            boundsCollider = GetComponent<BoxCollider2D>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = Object.FindAnyObjectByType<Camera>();
            }
        }

        if (boundsCollider == null || mainCamera == null)
        {
            return false;
        }

        camVertSize = mainCamera.orthographicSize * 2f;
        camHorizSize = camVertSize * mainCamera.aspect;
        return true;
    }

    public void Start()
    {
        if (!EnsureInitialized())
        {
            Debug.LogError("Missing required components on CameraBounds script.");
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.DungeonDictionary.TryGetValue(GameManager.Instance.CurrentRoomIndex, out GameManager.RoomData room))
        {
            ApplyRoomBounds(room);
        }
    }

    private void OnGenerateDungeon(GenerateDungeonEvent evt)
    {
        AdjustToStartRoom();
    }

    public void AdjustToStartRoom(GameManager.RoomData startRoom = null)
    {
        if (!EnsureInitialized()) return;

        if (startRoom == null)
        {
            if (GameManager.Instance != null && GameManager.Instance.DungeonDictionary != null)
            {
                if (!GameManager.Instance.DungeonDictionary.TryGetValue(0, out startRoom))
                {
                    if (GameManager.Instance.DungeonDictionary.Count > 0)
                    {
                        var enumerator = GameManager.Instance.DungeonDictionary.Values.GetEnumerator();
                        if (enumerator.MoveNext())
                        {
                            startRoom = enumerator.Current;
                        }
                    }
                }
            }
        }

        if (startRoom == null) return;

        SetRoomBounds(startRoom.CenterPosition, startRoom.Size.x, startRoom.Size.y, startRoom.CenterPosition);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(gameObject);
            CinemachineCamera vcam = virtualCamera != null ? virtualCamera : (confiner != null ? confiner.GetComponent<CinemachineCamera>() : null);
            if (vcam != null)
            {
                EditorUtility.SetDirty(vcam.gameObject);
            }
            if (confiner != null)
            {
                EditorUtility.SetDirty(confiner.gameObject);
            }
        }
#endif
    }

    private void OnDoorTriggered(DoorTriggeredEvent evt)
    {
        if (GameManager.Instance == null) return;
        ApplyDoorData(evt.DoorType);
    }

    private void ApplyDoorData(DoorType doorType)
    {
        currentDoorType = doorType;
    }

    private void OnRoomEntered(RoomEnteredEvent evt)
    {
        ApplyRoomBounds(evt.Room);
    }

    private void ApplyRoomBounds(GameManager.RoomData room)
    {
        Debug.Log(currentDoorType + " " + room.RoomIndex);
        if (boundsCollider == null || mainCamera == null || room == null) return;
        Vector3 initialCamPos = Vector3.zero;

        switch (currentDoorType)
        {
            case DoorType.EntryDoor:
                // Entering from an EntryDoor (going back) -> Player spawns at ExitDoor of previous room (offset down)
                if (!room.ExitDoorPosition.HasValue) return;
                initialCamPos = new Vector3(room.ExitDoorPosition.Value.x, room.ExitDoorPosition.Value.y - cameraTransitionOffset, transform.position.z);
                break;
            case DoorType.ExitDoor:
                // Entering from an ExitDoor (moving forward) -> Player spawns at EntryDoor of next room (offset down)
                if (!room.EntryDoorPosition.HasValue) return;
                initialCamPos = new Vector3(room.EntryDoorPosition.Value.x, room.EntryDoorPosition.Value.y - cameraTransitionOffset, transform.position.z);
                break;
            case DoorType.SpecialEntryDoor:
                if (!room.SpecialExitDoorPosition.HasValue) return;
                initialCamPos = new Vector3(room.SpecialExitDoorPosition.Value.x, room.SpecialExitDoorPosition.Value.y - cameraTransitionOffset, transform.position.z);
                break;
            case DoorType.SpecialExitDoor:
                if (!room.SpecialEntryDoorPosition.HasValue) return;
                initialCamPos = new Vector3(room.SpecialEntryDoorPosition.Value.x, room.SpecialEntryDoorPosition.Value.y - cameraTransitionOffset, transform.position.z);
                break;
            default:
                Debug.LogWarning("Unknown door type encountered in CameraBounds: " + currentDoorType);
                break;
        }

        SetRoomBounds(room.CenterPosition, room.Size.x, room.Size.y, initialCamPos);
    }

    public void SetRoomBounds(Vector3 roomCenter, float roomWidth, float roomHeight, Vector3? initialCameraPosition = null)
    {
        roomWidth += 4f;
        roomHeight += 2f;
        float finalWidth = Mathf.Max(roomWidth, camHorizSize);
        float finalHeight = Mathf.Max(roomHeight, camVertSize);

        boundsCollider.transform.position = roomCenter;
        boundsCollider.size = new Vector2(finalWidth, finalHeight);

        CinemachineCamera vcam = virtualCamera != null ? virtualCamera : (confiner != null ? confiner.GetComponent<CinemachineCamera>() : null);

        if (confiner != null)
        {
            confiner.InvalidateBoundingShapeCache();
        }

        if (vcam != null)
        {
            CinemachineFollow follow = vcam.GetComponent<CinemachineFollow>();
            if (follow != null)
            {
                follow.enabled = false;
            }

            Vector3 startPos = initialCameraPosition.HasValue ? initialCameraPosition.Value : roomCenter;
            Vector3 targetCamPos = new Vector3(startPos.x, startPos.y, vcam.transform.position.z);
            vcam.transform.position = targetCamPos;
            vcam.PreviousStateIsValid = false;
            vcam.ForceCameraPosition(targetCamPos, Quaternion.identity);

            if (vcam.Target.TrackingTarget != null)
            {
                vcam.OnTargetObjectWarped(vcam.Target.TrackingTarget, Vector3.zero);
            }

            if (confiner != null)
            {
                confiner.InvalidateBoundingShapeCache();
            }

            if (follow != null)
            {
                follow.enabled = true;
            }
        }
    }
}