using System;
using UnityEngine;
using FMODUnity;

public class UIAudioManager : MonoBehaviour
{
    private static UIAudioManager instance;

    [Header("UI Events")]
    [SerializeField] private EventReference clickSound;
    [SerializeField] private EventReference hoverSound;
    [SerializeField] private EventReference circleMenuSound;
    [SerializeField] private EventReference healthCollectSound;
    [SerializeField] private EventReference keyCollectSound;
    [SerializeField] private EventReference characterUpgrade;

    [Header("Environment Events")]
    [SerializeField] private EventReference openChestSound = default;
    [SerializeField] private EventReference destroyBarrelSound = default;
    [SerializeField] private EventReference openLockSound = default;

    [Header("Shared Entity Sounds")]
    [SerializeField] private EventReference moveSound = default;
    [SerializeField] private EventReference hitSound = default;
    [SerializeField] private EventReference deathSound = default;

    [Header("Shared Player Sounds")]
    [SerializeField] private EventReference lowLifeSound = default;
    [SerializeField] private EventReference stairsSound = default;
    
    [Header("Music Events")]
    [SerializeField] private EventReference bossroomMusic;
    [SerializeField] private EventReference chestroomMusic;
    [SerializeField] private EventReference dungeonMusic;
    [SerializeField] private EventReference winMusic;
    [SerializeField] private EventReference gameOverMusic;
    [SerializeField] private EventReference menuMusic;


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance == null)
        {
            var go = new GameObject("UIAudioManager");
            instance = go.AddComponent<UIAudioManager>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureDefaultEvents();
        BroadcastSharedAudio();
    }

    private void OnEnable()
    {
        EventBus<PlayUISoundEvent>.Subscribe(OnPlayUISound);
        EventBus<RequestSharedAudioEvent>.Subscribe(OnRequestSharedAudio);
        EventBus<PropDestroyedEvent>.Subscribe(OnPropDestroyed);
        EventBus<DoorTriggeredEvent>.Subscribe(OnDoorTriggered);
    }

    private void OnDisable()
    {
        EventBus<PlayUISoundEvent>.Unsubscribe(OnPlayUISound);
        EventBus<RequestSharedAudioEvent>.Unsubscribe(OnRequestSharedAudio);
        EventBus<PropDestroyedEvent>.Unsubscribe(OnPropDestroyed);
        EventBus<DoorTriggeredEvent>.Unsubscribe(OnDoorTriggered);
    }

    private void OnRequestSharedAudio(RequestSharedAudioEvent evt)
    {
        BroadcastSharedAudio();
    }

    private void OnPropDestroyed(PropDestroyedEvent evt)
    {
        if (evt.Prop != null && evt.Prop.TryGetComponent<DestructibleProp>(out var prop))
        {
            if (prop.PropData != null && prop.PropData.PropName == "Barrel")
            {
                if (!destroyBarrelSound.IsNull)
                {
                    RuntimeManager.PlayOneShot(destroyBarrelSound);
                }
                else
                {
                    Debug.LogWarning("[UIAudioManager] destroyBarrelSound is not assigned.");
                }
            }
        }
    }

    private void OnDoorTriggered(DoorTriggeredEvent evt)
    {
        if (!stairsSound.IsNull)
        {
            RuntimeManager.PlayOneShot(stairsSound);
        }
        else
        {
            Debug.LogWarning("[UIAudioManager] stairsSound is not assigned.");
        }
    }

    private void BroadcastSharedAudio()
    {
        EventBus<SharedAudioConfiguredEvent>.Raise(new SharedAudioConfiguredEvent(
            moveSound,
            hitSound,
            lowLifeSound,
            openChestSound,
            destroyBarrelSound
        ));
    }

    private void EnsureDefaultEvents()
    {
        if (destroyBarrelSound.IsNull)
        {
            destroyBarrelSound = RuntimeManager.PathToEventReference("event:/SFX_Chest_Destroy");
        }
        if (stairsSound.IsNull)
        {
            stairsSound = RuntimeManager.PathToEventReference("event:/SFX_Footsteps_Stairs");
        }
        if (clickSound.IsNull)
        {
            clickSound = RuntimeManager.PathToEventReference("event:/UI_Click");
        }
        if (hoverSound.IsNull)
        {
            hoverSound = RuntimeManager.PathToEventReference("event:/UI_Hover");
        }
        if (circleMenuSound.IsNull)
        {
            circleMenuSound = RuntimeManager.PathToEventReference("event:/UI_Circle_Menu");
        }
        if (moveSound.IsNull)
        {
            moveSound = RuntimeManager.PathToEventReference("event:/SFX_Caracter_Movement");
        }
        if (hitSound.IsNull)
        {
            hitSound = RuntimeManager.PathToEventReference("event:/SFX_EnemyHitFeedback");
        }
        if (lowLifeSound.IsNull)
        {
            lowLifeSound = RuntimeManager.PathToEventReference("event:/SFX_Low_Health");
        }
    }

    private void OnPlayUISound(PlayUISoundEvent evt)
    {
        EventReference soundRef = default;

        switch (evt.SoundType)
        {
            case UISoundType.Click:
                soundRef = clickSound;
                break;
            case UISoundType.Hover:
                soundRef = hoverSound;
                break;
            case UISoundType.CircleMenu:
                soundRef = circleMenuSound;
                break;
        }

        if (!soundRef.IsNull)
        {
            RuntimeManager.PlayOneShot(soundRef);
        }
    }
}
