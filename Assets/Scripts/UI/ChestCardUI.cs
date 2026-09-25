using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Chest
{
    public class ChestCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image cardImage;
        [SerializeField] private GameObject highlightBorder;
        [SerializeField] private Button selectButton;

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private LayoutElement layoutElement;

        [Header("Hover Animation")]
        [SerializeField] private float hoverLiftAmount = 18f;
        [SerializeField] private float hoverTransitionSpeed = 15f;

        private CardRewardItem currentItem;
        private bool isSelected;
        private bool isHovered;
        private Action<ChestCardUI> onClickCallback;
        private Action<ChestCardUI> onHoverEnterCallback;
        private Action<ChestCardUI> onHoverExitCallback;
        private RectTransform rectTransform;
        private Coroutine revealCoroutine;
        private Vector2 baseAnchoredPosition;
        private bool hasBasePosition;
        private float currentHoverOffset = 0f;
        private float targetHoverOffset = 0f;

        public CardRewardItem CurrentItem => currentItem;
        public bool IsSelected => isSelected;
        public bool IsRevealing => revealCoroutine != null;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (layoutElement == null) layoutElement = GetComponent<LayoutElement>();

            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnCardClicked);
            }
        }

        private void Start()
        {
            if (!hasBasePosition && rectTransform != null)
            {
                baseAnchoredPosition = rectTransform.anchoredPosition;
                hasBasePosition = true;
            }
        }

        private void Update()
        {
            if (!hasBasePosition && rectTransform != null)
            {
                baseAnchoredPosition = rectTransform.anchoredPosition;
                hasBasePosition = true;
            }

            if (Mathf.Abs(currentHoverOffset - targetHoverOffset) > 0.01f)
            {
                currentHoverOffset = Mathf.Lerp(currentHoverOffset, targetHoverOffset, Time.unscaledDeltaTime * hoverTransitionSpeed);
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2(baseAnchoredPosition.x, baseAnchoredPosition.y + currentHoverOffset);
                }
            }
            else if (currentHoverOffset != targetHoverOffset)
            {
                currentHoverOffset = targetHoverOffset;
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2(baseAnchoredPosition.x, baseAnchoredPosition.y + currentHoverOffset);
                }
            }
        }

        public void Setup(CardRewardItem item, Action<ChestCardUI> onClick, Action<ChestCardUI> onHoverEnter = null, Action<ChestCardUI> onHoverExit = null)
        {
            currentItem = item;
            onClickCallback = onClick;
            onHoverEnterCallback = onHoverEnter;
            onHoverExitCallback = onHoverExit;
            isSelected = false;
            isHovered = false;
            targetHoverOffset = 0f;
            currentHoverOffset = 0f;

            if (cardImage != null)
            {
                cardImage.sprite = item.CardSprite;
                cardImage.enabled = item.CardSprite != null;
            }

            SetSelected(false);
            UpdateHighlightVisual();
        }

        public void PrepareForReveal(float startScale, Color startColor)
        {
            if (selectButton != null) selectButton.interactable = false;
            rectTransform.localScale = Vector3.one * startScale;
            if (cardImage != null)
            {
                cardImage.color = startColor;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        public void AnimateReveal(float delay, float duration, float startScale, Color startColor)
        {
            if (revealCoroutine != null)
            {
                StopCoroutine(revealCoroutine);
            }
            revealCoroutine = StartCoroutine(RevealRoutine(delay, duration, startScale, startColor));
        }

        private System.Collections.IEnumerator RevealRoutine(float delay, float duration, float startScale, Color startColor)
        {
            PrepareForReveal(startScale, startColor);

            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            float elapsed = 0f;
            Color targetColor = Color.white;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float easeT = 1f - Mathf.Pow(1f - t, 3f);
                float scaleEase = 1f + 1.70158f * Mathf.Pow(t - 1f, 3f) + 0.70158f * Mathf.Pow(t - 1f, 2f);
                float currentScale = Mathf.Lerp(startScale, 1f, easeT);
                if (scaleEase > currentScale && t > 0.5f)
                {
                    currentScale = Mathf.Min(scaleEase, 1.05f);
                }
                rectTransform.localScale = Vector3.one * currentScale;

                if (cardImage != null)
                {
                    cardImage.color = Color.Lerp(startColor, targetColor, easeT);
                }

                yield return null;
            }

            rectTransform.localScale = Vector3.one;
            if (cardImage != null)
            {
                cardImage.color = targetColor;
            }

            if (selectButton != null) selectButton.interactable = true;

            revealCoroutine = null;
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            UpdateHighlightVisual();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (IsRevealing) return;
            isHovered = true;
            targetHoverOffset = hoverLiftAmount;
            UpdateHighlightVisual();
            onHoverEnterCallback?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            targetHoverOffset = 0f;
            UpdateHighlightVisual();
            onHoverExitCallback?.Invoke(this);
        }

        private void UpdateHighlightVisual()
        {
            if (highlightBorder != null)
            {
                highlightBorder.SetActive(isSelected || isHovered);
            }
        }

        private void OnCardClicked()
        {
            if (IsRevealing) return;
            onClickCallback?.Invoke(this);
        }
    }
}
