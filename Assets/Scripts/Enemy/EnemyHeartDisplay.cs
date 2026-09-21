using UnityEngine;

public enum HeartDepletionOrder
{
    FirstToLast,
    LastToFirst
}

public class EnemyHeartDisplay : MonoBehaviour
{
    [SerializeField] private GameObject heartsRoot;
    [SerializeField] private SpriteRenderer[] heartSlots;
    [SerializeField] private HeartDepletionOrder depletionOrder = HeartDepletionOrder.FirstToLast;
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;

    public GameObject HeartsRoot => heartsRoot;
    public SpriteRenderer[] HeartSlots => heartSlots;
    public HeartDepletionOrder DepletionOrder
    {
        get => depletionOrder;
        set => depletionOrder = value;
    }

    private void Awake()
    {
        if (heartsRoot != null)
        {
            heartsRoot.SetActive(false);
        }
    }

    public void SetVisible(bool visible)
    {
        if (heartsRoot != null && heartsRoot.activeSelf != visible)
        {
            heartsRoot.SetActive(visible);
        }
    }

    public void SetSprites(Sprite fullSprite, Sprite emptySprite)
    {
        if (fullSprite != null) fullHeartSprite = fullSprite;
        if (emptySprite != null) emptyHeartSprite = emptySprite;
    }

    public void UpdateHearts(int currentHealth, int maxHealth)
    {
        if (heartSlots == null || heartSlots.Length == 0) return;

        int totalHearts = heartSlots.Length;
        int fullCount = 0;
        if (currentHealth > 0 && maxHealth > 0)
        {
            fullCount = Mathf.Clamp(Mathf.CeilToInt((float)currentHealth / maxHealth * totalHearts), 1, totalHearts);
        }

        int emptyCount = totalHearts - fullCount;

        for (int i = 0; i < totalHearts; i++)
        {
            if (heartSlots[i] == null) continue;

            bool isFull = (depletionOrder == HeartDepletionOrder.FirstToLast)
                ? (i >= emptyCount)
                : (i < fullCount);

            heartSlots[i].sprite = isFull ? fullHeartSprite : emptyHeartSprite;
        }
    }
}