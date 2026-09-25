using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CharacterWheelController : MonoBehaviour
{
    [SerializeField] private CharacterSlotUI[] slots;
    [SerializeField] private Image fullBodyPreview;
    [SerializeField] private StatsDisplayUI statsDisplay;
    [SerializeField] private TMPro.TMP_Text characterNameText;
    [SerializeField] private Color lockedTint = new Color(0.25f, 0.25f, 0.25f, 1f);
    [Header("Layout Settings")]
    [SerializeField] private Vector2 centerOffset = Vector2.zero;
    [SerializeField] private float radius = 220f;
    [SerializeField] private float startAngleDeg = 90f;

    [Header("Animation & Input")]
    [SerializeField] private float spinDuration = 0.35f;
    [SerializeField] private float inputCooldown = 0.4f;

    [SerializeField] private InputActionAsset inputActionAsset;

    private const int WheelSlotCount = 4;

    private int selectedIndex;
    private int totalSlots;
    private float lastInputTime;
    private Coroutine spinCoroutine;

    private InputAction wheelSpinAction;
    private InputAction selectAction;

    private void Awake()
    {
        totalSlots = slots != null ? Mathf.Min(slots.Length, WheelSlotCount) : 0;
        if (characterNameText == null)
        {
            GameObject nameGo = GameObject.Find("CharacterName");
            if (nameGo != null)
            {
                characterNameText = nameGo.GetComponent<TMPro.TMP_Text>();
            }
        }
        SetupInputActions();
    }

    private void SetupInputActions()
    {
        if (inputActionAsset == null) return;

        InputActionMap menuMap = inputActionAsset.FindActionMap("Menu");
        if (menuMap != null)
        {
            wheelSpinAction = menuMap.FindAction("WheelSpin");
            selectAction = menuMap.FindAction("Select");
        }
    }

    private void OnEnable()
    {
        if (inputActionAsset != null)
        {
            InputActionMap menuMap = inputActionAsset.FindActionMap("Menu");
            menuMap?.Enable();
        }

        if (wheelSpinAction != null) wheelSpinAction.performed += OnWheelSpinPerformed;
        if (selectAction != null) selectAction.performed += OnSelectPerformed;

        UpdateSlotPositionsImmediate();
        UpdateSlotInteractivity();
        UpdatePreview();
    }

    private void OnDisable()
    {
        if (wheelSpinAction != null) wheelSpinAction.performed -= OnWheelSpinPerformed;
        if (selectAction != null) selectAction.performed -= OnSelectPerformed;

        if (inputActionAsset != null)
        {
            InputActionMap menuMap = inputActionAsset.FindActionMap("Menu");
            menuMap?.Disable();
        }

        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
            spinCoroutine = null;
        }
    }

    private void OnWheelSpinPerformed(InputAction.CallbackContext context)
    {
        if (Time.unscaledTime < lastInputTime + inputCooldown)
        {
            return;
        }

        float scroll = context.ReadValue<float>();
        if (scroll > 0.1f)
        {
            Rotate(1);
        }
        else if (scroll < -0.1f)
        {
            Rotate(-1);
        }
    }

    private void OnSelectPerformed(InputAction.CallbackContext context)
    {
        if (totalSlots == 0) return;

        CharacterSlotUI currentSlot = slots[selectedIndex];
        if (currentSlot == null || currentSlot.IsLocked || currentSlot.CharacterDefinition == null)
        {
            return;
        }

        EventBus<CharacterSelectedEvent>.Raise(
            new CharacterSelectedEvent(currentSlot.CharacterDefinition));
    }

    private void Rotate(int diff)
    {
        if (totalSlots == 0) return;

        lastInputTime = Time.unscaledTime;
        int previousSelectedIndex = selectedIndex;
        selectedIndex = (selectedIndex + diff) % totalSlots;
        if (selectedIndex < 0) selectedIndex += totalSlots;

        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
        }
        spinCoroutine = StartCoroutine(AnimateSlots(previousSelectedIndex, diff));
        UpdateSlotInteractivity();
        UpdatePreview();
        EventBus<PlayUISoundEvent>.Raise(new PlayUISoundEvent(UISoundType.CircleMenu));
    }

    private void UpdateSlotInteractivity()
    {
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                slots[i].SetTopSlot(i == selectedIndex);
            }
        }
    }

    private Vector2 GetPositionForSlot(int slotIndex, float selectionProgress)
    {
        float angleDeg = startAngleDeg - ((slotIndex - selectionProgress) * (360f / WheelSlotCount));
        float angleRad = angleDeg * Mathf.Deg2Rad;

        return centerOffset + new Vector2(Mathf.Cos(angleRad) * radius, Mathf.Sin(angleRad) * radius);
    }

    private void UpdateSlotPositionsImmediate()
    {
        if (totalSlots == 0) return;

        for (int i = 0; i < totalSlots; i++)
        {
            if (slots[i] != null && slots[i].transform is RectTransform rect)
            {
                rect.anchoredPosition = GetPositionForSlot(i, selectedIndex);
            }
        }
    }

    private IEnumerator AnimateSlots(int previousSelectedIndex, int selectionDelta)
    {
        if (totalSlots == 0) yield break;

        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / spinDuration);
            float smoothT = EaseOutBack(t);
            float selectionProgress = previousSelectedIndex + (selectionDelta * smoothT);

            for (int i = 0; i < totalSlots; i++)
            {
                if (slots[i] != null && slots[i].transform is RectTransform rect)
                {
                    rect.anchoredPosition = GetPositionForSlot(i, selectionProgress);
                }
            }

            yield return null;
        }

        spinCoroutine = null;
    }

    private static float EaseOutBack(float t)
    {
        const float overshoot = 1.70158f;
        float adjustedT = t - 1f;
        return 1f + ((overshoot + 1f) * adjustedT * adjustedT * adjustedT) + (overshoot * adjustedT * adjustedT);
    }

    private void UpdatePreview()
    {
        if (totalSlots == 0) return;

        CharacterSlotUI currentSlot = slots[selectedIndex];
        CharacterDefinition def = currentSlot != null ? currentSlot.CharacterDefinition : null;

        if (fullBodyPreview != null)
        {
            if (currentSlot != null && def != null)
            {
                fullBodyPreview.sprite = def.FullBodySprite;
                fullBodyPreview.enabled = fullBodyPreview.sprite != null;
                fullBodyPreview.color = currentSlot.IsLocked ? lockedTint : Color.white;
            }
            else
            {
                fullBodyPreview.enabled = false;
            }
        }

        if (characterNameText != null)
        {
            if (def != null)
            {
                characterNameText.text = def.CharacterName;
            }
            else
            {
                characterNameText.text = string.Empty;
            }
        }

        if (statsDisplay == null)
        {
            statsDisplay = Object.FindAnyObjectByType<StatsDisplayUI>(FindObjectsInactive.Include);
        }

        if (statsDisplay != null && def != null)
        {
            statsDisplay.DisplayCharacter(def);
        }
    }
}
