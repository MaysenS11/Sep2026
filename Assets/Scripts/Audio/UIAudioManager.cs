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


    private FMOD.Studio.EventInstance currentMusicInstance;
    private EventReference currentMusicRef;

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
        ApplySavedVolumes();

        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "StartMenu")
        {
            PlayMusic(menuMusic);
        }
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        StopMusic();
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name == "StartMenu")
        {
            PlayMusic(menuMusic);
        }
    }

    private void OnEnable()
    {
        EventBus<PlayUISoundEvent>.Subscribe(OnPlayUISound);
        EventBus<RequestSharedAudioEvent>.Subscribe(OnRequestSharedAudio);
        EventBus<PropDestroyedEvent>.Subscribe(OnPropDestroyed);
        EventBus<DoorTriggeredEvent>.Subscribe(OnDoorTriggered);
        EventBus<RoomEnteredEvent>.Subscribe(OnRoomEntered);
        EventBus<GameWonEvent>.Subscribe(OnGameWon);
        EventBus<GameOverEvent>.Subscribe(OnGameOver);
        EventBus<HeartCollectedEvent>.Subscribe(OnHeartCollected);
        EventBus<KeyCollectedEvent>.Subscribe(OnKeyCollected);
        EventBus<StatUpgradeAppliedEvent>.Subscribe(OnStatUpgradeApplied);
    }

    private void OnDisable()
    {
        EventBus<PlayUISoundEvent>.Unsubscribe(OnPlayUISound);
        EventBus<RequestSharedAudioEvent>.Unsubscribe(OnRequestSharedAudio);
        EventBus<PropDestroyedEvent>.Unsubscribe(OnPropDestroyed);
        EventBus<DoorTriggeredEvent>.Unsubscribe(OnDoorTriggered);
        EventBus<RoomEnteredEvent>.Unsubscribe(OnRoomEntered);
        EventBus<GameWonEvent>.Unsubscribe(OnGameWon);
        EventBus<GameOverEvent>.Unsubscribe(OnGameOver);
        EventBus<HeartCollectedEvent>.Unsubscribe(OnHeartCollected);
        EventBus<KeyCollectedEvent>.Unsubscribe(OnKeyCollected);
        EventBus<StatUpgradeAppliedEvent>.Unsubscribe(OnStatUpgradeApplied);
    }

    private void OnRoomEntered(RoomEnteredEvent evt)
    {
        if (evt.Room == null) return;

        switch (evt.Room.Type)
        {
            case RoomType.Boss:
                PlayMusic(bossroomMusic);
                break;
            case RoomType.Chest:
                PlayMusic(chestroomMusic);
                break;
            case RoomType.Start:
            case RoomType.Normal:
            default:
                PlayMusic(dungeonMusic);
                break;
        }
    }

    private void OnGameWon(GameWonEvent evt)
    {
        PlayMusic(winMusic);
    }

    private void OnGameOver(GameOverEvent evt)
    {
        PlayMusic(gameOverMusic);
    }

    private void OnHeartCollected(HeartCollectedEvent evt)
    {
        if (!healthCollectSound.IsNull)
        {
            RuntimeManager.PlayOneShot(healthCollectSound);
        }
    }

    private void OnKeyCollected(KeyCollectedEvent evt)
    {
        if (!keyCollectSound.IsNull)
        {
            RuntimeManager.PlayOneShot(keyCollectSound);
        }
    }

    private void OnStatUpgradeApplied(StatUpgradeAppliedEvent evt)
    {
        if (!characterUpgrade.IsNull)
        {
            RuntimeManager.PlayOneShot(characterUpgrade);
        }
    }

    public void PlayMusic(EventReference musicRef)
    {
        if (musicRef.IsNull) return;

        if (currentMusicInstance.isValid())
        {
            if (musicRef.Guid == currentMusicRef.Guid)
            {
                return;
            }

            currentMusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            currentMusicInstance.release();
        }

        currentMusicRef = musicRef;
        currentMusicInstance = RuntimeManager.CreateInstance(musicRef);
        currentMusicInstance.start();
    }

    public void StopMusic()
    {
        if (currentMusicInstance.isValid())
        {
            currentMusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            currentMusicInstance.release();
        }
        currentMusicRef = default;
    }

    public static void SetMasterVolume(float volume)
    {
        try
        {
            var bus = RuntimeManager.GetBus("bus:/");
            bus.setVolume(Mathf.Clamp01(volume));
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[UIAudioManager] SetMasterVolume warning: {ex.Message}");
        }
    }

    public static void SetSFXVolume(float volume)
    {
        try
        {
            var bus = RuntimeManager.GetBus("bus:/SFX");
            bus.setVolume(Mathf.Clamp01(volume));
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[UIAudioManager] SetSFXVolume warning: {ex.Message}");
        }
    }

    private void ApplySavedVolumes()
    {
        float master = PlayerPrefs.GetFloat("FMOD_MasterVolume", 1f);
        float sfx = PlayerPrefs.GetFloat("FMOD_SFXVolume", 1f);
        SetMasterVolume(master);
        SetSFXVolume(sfx);
    }

    private void OnRequestSharedAudio(RequestSharedAudioEvent evt)
    {
        BroadcastSharedAudio();
    }

    private void OnPropDestroyed(PropDestroyedEvent evt)
    {
        if (evt.Prop != null && (evt.Prop.name.Contains("Barrel") || evt.Prop.name.Contains("barrel") || evt.Prop.name.Contains("Prop")))
        {
            if (!destroyBarrelSound.IsNull)
            {
                RuntimeManager.PlayOneShot(destroyBarrelSound);
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
        if (healthCollectSound.IsNull)
        {
            healthCollectSound = RuntimeManager.PathToEventReference("event:/SFX_Heart_Collect");
        }
        if (keyCollectSound.IsNull)
        {
            keyCollectSound = RuntimeManager.PathToEventReference("event:/SFX_Key_Collect");
        }
        if (characterUpgrade.IsNull)
        {
            characterUpgrade = RuntimeManager.PathToEventReference("event:/SFX_Character_Upgrade");
        }
        if (dungeonMusic.IsNull)
        {
            dungeonMusic = RuntimeManager.PathToEventReference("event:/Music_Dungeon");
        }
        if (chestroomMusic.IsNull)
        {
            chestroomMusic = RuntimeManager.PathToEventReference("event:/Music_ChestRoom");
        }
        if (bossroomMusic.IsNull)
        {
            bossroomMusic = RuntimeManager.PathToEventReference("event:/Music_BossRoom");
        }
        if (winMusic.IsNull)
        {
            winMusic = RuntimeManager.PathToEventReference("event:/Music_Win");
        }
        if (gameOverMusic.IsNull)
        {
            gameOverMusic = RuntimeManager.PathToEventReference("event:/Music_GameOver");
        }
        if (menuMusic.IsNull)
        {
            menuMusic = RuntimeManager.PathToEventReference("event:/Music_Menu");
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

