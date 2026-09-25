using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Runtime Counter")]
    [SerializeField] private TMP_Text timerText;
    private float elapsedTime;
    public float ElapsedTime => elapsedTime;
    private int lastDisplayedSecond = -1;
    private readonly char[] timerBuffer = new char[8];

    [Header("Player Equipment")]
    [SerializeField] private Image maskDisplayImage;

    [Header("Health Display (Hearts)")]
    [Tooltip("Assign heart roots in left-to-right order, or leave empty to auto-find from HealthContainer")]
    [SerializeField] private Transform[] heartSlots;

    private struct HeartSlotItem
    {
        public GameObject Root;
        public GameObject Half;
        public GameObject Full;
    }

    private readonly List<HeartSlotItem> registeredHearts = new List<HeartSlotItem>();
    private Transform healthContainerTransform;
    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        InitializeHearts();
    }

    private void InitializeHearts()
    {
        if (isInitialized) return;

        registeredHearts.Clear();

        if (heartSlots == null || heartSlots.Length == 0)
        {
            healthContainerTransform = transform.Find("IngamePanel/HealthContainer");
            if (healthContainerTransform == null)
            {
                healthContainerTransform = transform.Find("HUD/HealthContainer");
            }
            if (healthContainerTransform == null)
            {
                var foundHc = GameObject.Find("HealthContainer");
                if (foundHc != null) healthContainerTransform = foundHc.transform;
            }

            if (healthContainerTransform != null)
            {
                var containerList = new List<Transform>();
                for (int i = 0; i < healthContainerTransform.childCount; i++)
                {
                    containerList.Add(healthContainerTransform.GetChild(i));
                }
                heartSlots = containerList.ToArray();
            }
        }
        else if (heartSlots.Length > 0 && heartSlots[0] != null)
        {
            healthContainerTransform = heartSlots[0].parent;
        }

        if (heartSlots != null)
        {
            for (int i = 0; i < heartSlots.Length; i++)
            {
                Transform slotTransform = heartSlots[i];
                if (slotTransform == null) continue;

                Transform halfTrans = slotTransform.Find("HeartHalf");
                Transform fullTrans = slotTransform.Find("HeartFull");

                registeredHearts.Add(new HeartSlotItem
                {
                    Root = slotTransform.gameObject,
                    Half = halfTrans != null ? halfTrans.gameObject : null,
                    Full = fullTrans != null ? fullTrans.gameObject : null
                });
            }
        }

        isInitialized = true;
    }

    private void OnEnable()
    {
        EventBus<PlayerHealthChangedEvent>.Subscribe(OnPlayerHealthChanged);
    }

    private void OnDisable()
    {
        EventBus<PlayerHealthChangedEvent>.Unsubscribe(OnPlayerHealthChanged);
    }

    private void OnPlayerHealthChanged(PlayerHealthChangedEvent evt)
    {
        UpdateHealth(evt.CurrentHealth, evt.MaxHealth);
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        int totalSeconds = (int)elapsedTime;
        if (totalSeconds == lastDisplayedSecond) return;
        lastDisplayedSecond = totalSeconds;

        if (timerText != null)
        {
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;

            timerBuffer[0] = (char)('0' + (hours / 10) % 10);
            timerBuffer[1] = (char)('0' + hours % 10);
            timerBuffer[2] = ':';
            timerBuffer[3] = (char)('0' + (minutes / 10) % 10);
            timerBuffer[4] = (char)('0' + minutes % 10);
            timerBuffer[5] = ':';
            timerBuffer[6] = (char)('0' + (seconds / 10) % 10);
            timerBuffer[7] = (char)('0' + seconds % 10);

            timerText.SetCharArray(timerBuffer, 0, 8);
        }
    }

    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        if (!isInitialized)
        {
            InitializeHearts();
        }

        if (registeredHearts.Count == 0) return;

        int neededSlots = Mathf.Max(1, Mathf.CeilToInt(maxHealth / 2f));

        // Dynamically instantiate additional heart containers if maxHealth expanded beyond pre-spawned count
        while (registeredHearts.Count < neededSlots && registeredHearts.Count > 0 && healthContainerTransform != null)
        {
            GameObject template = registeredHearts[0].Root;
            if (template == null) break;

            GameObject newSlotGo = Instantiate(template, healthContainerTransform);
            newSlotGo.name = $"HeartSlot_{registeredHearts.Count}";
            Transform halfTrans = newSlotGo.transform.Find("HeartHalf");
            Transform fullTrans = newSlotGo.transform.Find("HeartFull");

            registeredHearts.Add(new HeartSlotItem
            {
                Root = newSlotGo,
                Half = halfTrans != null ? halfTrans.gameObject : null,
                Full = fullTrans != null ? fullTrans.gameObject : null
            });
        }

        int totalSlots = registeredHearts.Count;

        for (int i = 0; i < totalSlots; i++)
        {
            HeartSlotItem slot = registeredHearts[i];
            if (slot.Root == null) continue;

            if (i >= neededSlots)
            {
                slot.Root.SetActive(false);
                continue;
            }

            slot.Root.SetActive(true);
            int slotHp = currentHealth - (i * 2);

            if (slotHp >= 2)
            {
                if (slot.Half != null) slot.Half.SetActive(true);
                if (slot.Full != null) slot.Full.SetActive(true);
            }
            else if (slotHp == 1)
            {
                if (slot.Half != null) slot.Half.SetActive(true);
                if (slot.Full != null) slot.Full.SetActive(false);
            }
            else
            {
                if (slot.Half != null) slot.Half.SetActive(false);
                if (slot.Full != null) slot.Full.SetActive(false);
            }
        }
    }

    public void SetMaskSprite(Sprite newMask)
    {
        if (maskDisplayImage != null) maskDisplayImage.sprite = newMask;
    }

    [Header("Keys")]
    [SerializeField] private TMP_Text keyCountText;
    [SerializeField] private GameObject keyIcon;
    private int currentKeys = 0;
    public int CurrentKeys => currentKeys;

    [Header("Win Screen")]
    [SerializeField] private GameObject winScreenPanel;

    public void AddKey(int amount = 1)
    {
        currentKeys += amount;
        UpdateKeyUI();
    }

    public bool TryUseKey()
    {
        if (currentKeys > 0)
        {
            currentKeys--;
            UpdateKeyUI();
            return true;
        }
        return false;
    }

    public void UpdateKeyUI()
    {
        if (keyCountText == null)
        {
            var foundText = transform.Find("IngamePanel/KeyContainer/KeyCount")
                         ?? transform.Find("HUD/KeyContainer/KeyCount")
                         ?? transform.Find("KeyCount");
            if (foundText != null) keyCountText = foundText.GetComponent<TMP_Text>();
        }

        if (keyCountText != null)
        {
            keyCountText.text = currentKeys.ToString();
        }
    }

    public void ShowWinScreen()
    {
        if (winScreenPanel == null)
        {
            winScreenPanel = transform.Find("WinPanel")?.gameObject
                          ?? transform.Find("WinScreen")?.gameObject
                          ?? transform.Find("VictoryPanel")?.gameObject;
        }

        if (winScreenPanel != null)
        {
            winScreenPanel.SetActive(true);
        }
        else
        {
            Debug.Log("[UIManager] Win Screen activated! The King has fallen.");
        }
    }
}