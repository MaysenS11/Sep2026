using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Runtime Counter")]
    [SerializeField] private TMP_Text timerText;
    private float elapsedTime;

    [Header("Player Equipment")]
    [SerializeField] private Image maskDisplayImage;

    [Header("Health Display (Heart Fills)")]
    [Tooltip("Assign the HeartFillContainer transforms in left-to-right order, or leave empty to auto-find from HUD/HealthContainer")]
    [SerializeField] private Transform[] heartFillContainers;

    private readonly List<GameObject> rightToLeftFills = new List<GameObject>();
    private bool isInitialized = false;

    private void Awake()
    {
        InitializeHeartFills();
    }

    private void InitializeHeartFills()
    {
        if (isInitialized) return;

        if (heartFillContainers == null || heartFillContainers.Length == 0)
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
                    Transform child = healthContainer.GetChild(i);
                    Transform fillContainer = child.Find("HeartFillContainer");
                    if (fillContainer != null)
                    {
                        containerList.Add(fillContainer);
                    }
                }
                heartFillContainers = containerList.ToArray();
            }
        }

        rightToLeftFills.Clear();

        if (heartFillContainers != null && heartFillContainers.Length > 0)
        {
            for (int c = heartFillContainers.Length - 1; c >= 0; c--)
            {
                Transform container = heartFillContainers[c];
                if (container == null) continue;

                for (int f = container.childCount - 1; f >= 0; f--)
                {
                    Transform fillChild = container.GetChild(f);
                    if (fillChild != null)
                    {
                        rightToLeftFills.Add(fillChild.gameObject);
                    }
                }
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
        System.TimeSpan t = System.TimeSpan.FromSeconds(elapsedTime);
        if (timerText != null)
        {
            timerText.text = string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D2}", 
                t.Hours, t.Minutes, t.Seconds, t.Milliseconds / 10);
        }
    }

    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        if (!isInitialized)
        {
            InitializeHeartFills();
        }

        if (rightToLeftFills.Count > 0)
        {
            int damageTaken = Mathf.Max(0, maxHealth - currentHealth);

            for (int i = 0; i < rightToLeftFills.Count; i++)
            {
                if (rightToLeftFills[i] == null) continue;

                bool isActive = i >= damageTaken;
                rightToLeftFills[i].SetActive(isActive);
            }
        }
    }

    public void SetMaskSprite(Sprite newMask)
    {
        if (maskDisplayImage != null) maskDisplayImage.sprite = newMask;
    }
}