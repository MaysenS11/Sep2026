using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Runtime Counter")]
    [SerializeField] private TMP_Text timerText;
    private float elapsedTime;
    private int lastDisplayedSecond = -1;
    private readonly char[] timerBuffer = new char[8];

    [Header("Player Equipment")]
    [SerializeField] private Image maskDisplayImage;

    [Header("Health Display (Hearts)")]
    [Tooltip("Assign heart roots in left-to-right order, or leave empty to auto-find from HealthContainer")]
    [SerializeField] private Transform[] heartSlots;

    private struct HeartSlotItem
    {
        public GameObject Half;
        public GameObject Full;
    }

    private readonly List<HeartSlotItem> registeredHearts = new List<HeartSlotItem>();
    private bool isInitialized = false;

    private void Awake()
    {
        InitializeHearts();
    }

    private void InitializeHearts()
    {
        if (isInitialized) return;

        registeredHearts.Clear();

        if (heartSlots == null || heartSlots.Length == 0)
        {
            Transform healthContainer = transform.Find("IngamePanel/HealthContainer");
            if (healthContainer == null)
            {
                healthContainer = transform.Find("HUD/HealthContainer");
            }
            if (healthContainer == null)
            {
                var foundHc = GameObject.Find("HealthContainer");
                if (foundHc != null) healthContainer = foundHc.transform;
            }

            if (healthContainer != null)
            {
                var containerList = new List<Transform>();
                for (int i = 0; i < healthContainer.childCount; i++)
                {
                    containerList.Add(healthContainer.GetChild(i));
                }
                heartSlots = containerList.ToArray();
            }
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

        int halfHeartsRemaining = Mathf.Max(0, currentHealth / 2);

        for (int i = 0; i < registeredHearts.Count; i++)
        {
            HeartSlotItem slot = registeredHearts[i];
            int heartValue = (i + 1) * 2;

            if (halfHeartsRemaining >= heartValue)
            {
                if (slot.Half != null) slot.Half.SetActive(true);
                if (slot.Full != null) slot.Full.SetActive(true);
            }
            else if (halfHeartsRemaining == heartValue - 1)
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
}