using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CharacterSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private CharacterDefinition characterDefinition;
    [SerializeField] private Image maskImage;
    [SerializeField] private Image overlayMaskImage;
    [SerializeField] private Button selectButton;

    [Header("Hover Overlay Settings")]
    [SerializeField] private float hoverAlpha = 0.45f;
    [SerializeField] private float normalAlpha = 0f;
    [SerializeField] private float fadeDuration = 0.12f;
    [SerializeField] private Color pressedTint = new Color(0.72f, 0.72f, 0.72f);
    [SerializeField] private Color lockedTint = new Color(0.25f, 0.25f, 0.25f, 1f);

    private bool isLocked;
    private bool isHovered;
    private bool isPressed;
    private Coroutine fadeCoroutine;

    public CharacterDefinition CharacterDefinition => characterDefinition;
    public bool IsLocked => isLocked;
    public Button SelectButton => selectButton;

    private void Awake()
    {
        if (selectButton == null) selectButton = GetComponent<Button>();
        if (maskImage == null && selectButton != null) maskImage = selectButton.targetGraphic as Image;
        if (overlayMaskImage == null)
        {
            Transform overlayChild = transform.Find("OverlayMask");
            if (overlayChild != null) overlayMaskImage = overlayChild.GetComponent<Image>();
        }
    }

    private void OnEnable()
    {
        EventBus<CharacterUnlockedEvent>.Subscribe(OnCharacterUnlocked);
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnSlotClicked);
        }
        ApplyDefinition();
    }

    private void OnDisable()
    {
        EventBus<CharacterUnlockedEvent>.Unsubscribe(OnCharacterUnlocked);
        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(OnSlotClicked);
        }
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        isHovered = false;
        isPressed = false;
        UpdateBaseMaskColor();
        SetOverlayAlphaImmediate(normalAlpha);
    }

    public void SetDefinition(CharacterDefinition definition)
    {
        characterDefinition = definition;
        ApplyDefinition();
    }

    private void ApplyDefinition()
    {
        if (characterDefinition == null) return;

        isLocked = characterDefinition.LockedByDefault;

        if (maskImage != null)
        {
            if (characterDefinition.MaskSprite != null)
            {
                maskImage.sprite = characterDefinition.MaskSprite;
            }
            UpdateBaseMaskColor();
        }

        if (overlayMaskImage != null)
        {
            if (characterDefinition.MaskWhiteSprite != null)
            {
                overlayMaskImage.sprite = characterDefinition.MaskWhiteSprite;
            }
            overlayMaskImage.raycastTarget = false;
            SetOverlayAlphaImmediate(normalAlpha);
        }

        if (selectButton != null)
        {
            selectButton.interactable = !isLocked;
            selectButton.transition = Selectable.Transition.None;
        }
    }

    private void UpdateBaseMaskColor()
    {
        if (maskImage == null || characterDefinition == null) return;

        if (isLocked)
        {
            maskImage.color = lockedTint;
        }
        else if (isPressed)
        {
            maskImage.color = pressedTint;
        }
        else
        {
            maskImage.color = Color.white;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isLocked) return;
        isHovered = true;
        if (!isPressed)
        {
            FadeOverlay(hoverAlpha);
        }
        EventBus<PlayUISoundEvent>.Raise(new PlayUISoundEvent(UISoundType.Hover));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isLocked) return;
        isHovered = false;
        isPressed = false;
        UpdateBaseMaskColor();
        FadeOverlay(normalAlpha);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isLocked) return;
        isPressed = true;
        UpdateBaseMaskColor();
        FadeOverlay(normalAlpha);
        EventBus<PlayUISoundEvent>.Raise(new PlayUISoundEvent(UISoundType.Click));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isLocked) return;
        isPressed = false;
        UpdateBaseMaskColor();
        FadeOverlay(isHovered ? hoverAlpha : normalAlpha);
    }

    private void FadeOverlay(float targetAlpha)
    {
        if (overlayMaskImage == null) return;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(AnimateOverlayAlpha(targetAlpha));
    }

    private IEnumerator AnimateOverlayAlpha(float targetAlpha)
    {
        if (overlayMaskImage == null) yield break;

        Color currentColor = overlayMaskImage.color;
        float startAlpha = currentColor.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            overlayMaskImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        overlayMaskImage.color = new Color(1f, 1f, 1f, targetAlpha);
        fadeCoroutine = null;
    }

    private void SetOverlayAlphaImmediate(float alpha)
    {
        if (overlayMaskImage != null)
        {
            overlayMaskImage.color = new Color(1f, 1f, 1f, alpha);
        }
    }

    private void OnCharacterUnlocked(CharacterUnlockedEvent evt)
    {
        if (characterDefinition != null && evt.Character == characterDefinition)
        {
            Unlock();
        }
    }

    public void Unlock()
    {
        isLocked = false;
        if (maskImage != null && characterDefinition != null)
        {
            maskImage.color = Color.white;
        }
        if (selectButton != null)
        {
            selectButton.interactable = true;
        }
    }

    private void OnSlotClicked()
    {
        if (isLocked || characterDefinition == null) return;

        EventBus<CharacterSelectedEvent>.Raise(new CharacterSelectedEvent(characterDefinition));
    }
}
