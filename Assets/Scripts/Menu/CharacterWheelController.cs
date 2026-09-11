using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CharacterWheelController : MonoBehaviour
{
    [SerializeField] private CharacterSlotUI[] slots;
    [SerializeField] private Image fullBodyPreview;
    [SerializeField] private float radius = 220f;
    [SerializeField] private float spinDuration = 0.15f;
    [SerializeField] private float inputCooldown = 0.2f;

    [SerializeField] private InputActionAsset inputActionAsset;

    private int selectedIndex;
    private int totalSlots;
    private float lastInputTime;
    private Coroutine spinCoroutine;

    private InputAction wheelSpinAction;
    private InputAction wheelNextAction;
    private InputAction wheelPrevAction;

    private void Awake()
    {
        totalSlots = slots != null ? slots.Length : 0;
        SetupInputActions();
    }

    private void SetupInputActions()
    {
        if (inputActionAsset == null) return;

        InputActionMap menuMap = inputActionAsset.FindActionMap("Menu");
        if (menuMap != null)
        {
            wheelSpinAction = menuMap.FindAction("WheelSpin");
            wheelNextAction = menuMap.FindAction("WheelNext");
            wheelPrevAction = menuMap.FindAction("WheelPrev");
        }
    }

    private void OnEnable()
    {
        if (inputActionAsset != null)
        {
            InputActionMap menuMap = inputActionAsset.FindActionMap("Menu");
            menuMap?.Enable();
        }

        if (wheelNextAction != null) wheelNextAction.performed += OnNextPerformed;
        if (wheelPrevAction != null) wheelPrevAction.performed += OnPrevPerformed;

        UpdateSlotPositionsImmediate();
        UpdatePreview();
    }

    private void OnDisable()
    {
        if (wheelNextAction != null) wheelNextAction.performed -= OnNextPerformed;
        if (wheelPrevAction != null) wheelPrevAction.performed -= OnPrevPerformed;

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

    private void Update()
    {
        if (wheelSpinAction != null && Time.unscaledTime >= lastInputTime + inputCooldown)
        {
            Vector2 scroll = wheelSpinAction.ReadValue<Vector2>();
            if (scroll.y > 0.1f)
            {
                RotateWheel(1);
            }
            else if (scroll.y < -0.1f)
            {
                RotateWheel(-1);
            }
        }
    }

    private void OnNextPerformed(InputAction.CallbackContext context)
    {
        if (Time.unscaledTime >= lastInputTime + inputCooldown)
        {
            RotateWheel(1);
        }
    }

    private void OnPrevPerformed(InputAction.CallbackContext context)
    {
        if (Time.unscaledTime >= lastInputTime + inputCooldown)
        {
            RotateWheel(-1);
        }
    }

    public void RotateWheel(int direction)
    {
        if (totalSlots == 0) return;

        lastInputTime = Time.unscaledTime;
        selectedIndex = (selectedIndex + direction) % totalSlots;
        if (selectedIndex < 0) selectedIndex += totalSlots;

        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
        }
        spinCoroutine = StartCoroutine(AnimateSlotsToTargets());
        UpdatePreview();
    }

    private Vector2 GetTargetPositionForSlot(int slotIndex)
    {
        int relativeOffset = (slotIndex - selectedIndex) % totalSlots;
        if (relativeOffset < 0) relativeOffset += totalSlots;

        float angleDeg = 90f - (relativeOffset * (360f / totalSlots));
        float angleRad = angleDeg * Mathf.Deg2Rad;

        return new Vector2(Mathf.Cos(angleRad) * radius, Mathf.Sin(angleRad) * radius);
    }

    private void UpdateSlotPositionsImmediate()
    {
        if (totalSlots == 0) return;

        for (int i = 0; i < totalSlots; i++)
        {
            if (slots[i] != null && slots[i].transform is RectTransform rect)
            {
                rect.anchoredPosition = GetTargetPositionForSlot(i);
            }
        }
    }

    private IEnumerator AnimateSlotsToTargets()
    {
        if (totalSlots == 0) yield break;

        Vector2[] startPositions = new Vector2[totalSlots];
        Vector2[] targetPositions = new Vector2[totalSlots];

        for (int i = 0; i < totalSlots; i++)
        {
            if (slots[i] != null && slots[i].transform is RectTransform rect)
            {
                startPositions[i] = rect.anchoredPosition;
                targetPositions[i] = GetTargetPositionForSlot(i);
            }
        }

        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / spinDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < totalSlots; i++)
            {
                if (slots[i] != null && slots[i].transform is RectTransform rect)
                {
                    rect.anchoredPosition = Vector2.Lerp(startPositions[i], targetPositions[i], smoothT);
                }
            }

            yield return null;
        }

        for (int i = 0; i < totalSlots; i++)
        {
            if (slots[i] != null && slots[i].transform is RectTransform rect)
            {
                rect.anchoredPosition = targetPositions[i];
            }
        }

        spinCoroutine = null;
    }

    private void UpdatePreview()
    {
        if (fullBodyPreview == null || totalSlots == 0) return;

        CharacterSlotUI currentSlot = slots[selectedIndex];
        if (currentSlot != null && currentSlot.CharacterDefinition != null)
        {
            fullBodyPreview.sprite = currentSlot.CharacterDefinition.FullBodySprite;
            fullBodyPreview.enabled = fullBodyPreview.sprite != null;
            fullBodyPreview.color = currentSlot.IsLocked ? currentSlot.CharacterDefinition.LockedTint : Color.white;
        }
        else
        {
            fullBodyPreview.enabled = false;
        }
    }
}
