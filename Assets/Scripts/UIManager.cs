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

    [Header("Health Display")]
    [SerializeField] private Image[] heartImages; // Drag your 3 Heart Image components here
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;

    private void Update()
    {
        // 1. Update Timer (Format 00:00:00:00 -> Hrs:Mins:Secs:MS)
        elapsedTime += Time.deltaTime;
        System.TimeSpan t = System.TimeSpan.FromSeconds(elapsedTime);
        timerText.text = string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D2}", 
            t.Hours, t.Minutes, t.Seconds, t.Milliseconds / 10);
    }

    public void UpdateHealth(int currentHealth)
    {
        for (int i = 0; i < heartImages.Length; i++)
        {
            heartImages[i].sprite = (i < currentHealth) ? fullHeartSprite : emptyHeartSprite;
        }
    }

    public void SetMaskSprite(Sprite newMask)
    {
        if (maskDisplayImage != null) maskDisplayImage.sprite = newMask;
    }
}