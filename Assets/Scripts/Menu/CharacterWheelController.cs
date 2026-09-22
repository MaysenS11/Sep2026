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
    public enum WheelLayoutMode
    {
        RadialArc,
        ManualWaypoints
    }

    [Header("Layout Settings")]
    [SerializeField] private WheelLayoutMode layoutMode = WheelLayoutMode.RadialArc;
    [SerializeField] private Vector2 centerOffset = Vector2.zero;
    [SerializeField] private float radius = 220f;
    [SerializeField] private float startAngleDeg = 90f;
    [SerializeField] private float arcSpreadDeg = 360f;
    [SerializeField] private RectTransform[] customWaypointTransforms;

    [Header("Animation & Input")]
    [SerializeField] private float spinDuration = 0.15f;
    [SerializeField] private float inputCooldown = 0.2f;

    [SerializeField] private InputActionAsset inputActionAsset;

    private int selectedIndex;
    private int totalSlots;
    private float lastInputTime;
    private Coroutine spinCoroutine;
    private Vector2[] cachedWaypoints;

    private InputAction wheelSpinAction;
    private InputAction wheelNextAction;
    private InputAction wheelPrevAction;

    private void Awake()
    {
        totalSlots = slots != null ? slots.Length : 0;
        if (characterNameText == null)
        {
            GameObject nameGo = GameObject.Find("CharacterName");
            if (nameGo != null)
            {
                characterNameText = nameGo.GetComponent<TMPro.TMP_Text>();
            }
        }
        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i;
                if (slots[i] != null && slots[i].SelectButton != null)
                {
                    slots[i].SelectButton.onClick.AddListener(() => SelectSlot(index));
                }
            }
        }
        CacheWaypoints();
        SetupInputActions();
    }

    private void CacheWaypoints()
    {
        if (totalSlots == 0) return;

        cachedWaypoints = new Vector2[totalSlots];

        if (customWaypointTransforms != null && customWaypointTransforms.Length >= totalSlots)
        {
            for (int i = 0; i < totalSlots; i++)
            {
                cachedWaypoints[i] = customWaypointTransforms[i] != null 
                    ? customWaypointTransforms[i].anchoredPosition 
                    : Vector2.zero;
            }
        }
        else
        {
            for (int i = 0; i < totalSlots; i++)
            {
                if (slots[i] != null && slots[i].transform is RectTransform rt)
                {
                    cachedWaypoints[i] = rt.anchoredPosition;
                }
            }
        }
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

    private void Start()
    {
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
        EventBus<PlayUISoundEvent>.Raise(new PlayUISoundEvent(UISoundType.CircleMenu));
    }

    public void SelectSlot(int slotIndex)
    {
        if (totalSlots == 0 || slotIndex == selectedIndex) return;

        int forwardDiff = (slotIndex - selectedIndex + totalSlots) % totalSlots;
        int backwardDiff = (selectedIndex - slotIndex + totalSlots) % totalSlots;
        int diff = forwardDiff <= backwardDiff ? forwardDiff : -backwardDiff;

        RotateWheel(diff);
    }

    private Vector2 GetTargetPositionForSlot(int slotIndex)
    {
        int relativeOffset = (slotIndex - selectedIndex) % totalSlots;
        if (relativeOffset < 0) relativeOffset += totalSlots;

        if (layoutMode == WheelLayoutMode.ManualWaypoints && cachedWaypoints != null && cachedWaypoints.Length == totalSlots)
        {
            return cachedWaypoints[relativeOffset];
        }

        float step = totalSlots > 1 && Mathf.Approximately(Mathf.Abs(arcSpreadDeg), 360f) 
            ? (arcSpreadDeg / totalSlots) 
            : (totalSlots > 1 ? (arcSpreadDeg / (totalSlots - 1)) : 0f);

        float angleDeg = startAngleDeg - (relativeOffset * step);
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
                rect.anchoredPosition = GetTargetPositionForSlot(i);
            }
        }
    }

    private void OnValidate()
    {
        if (slots == null || slots.Length == 0) return;
        totalSlots = slots.Length;

        if (layoutMode == WheelLayoutMode.RadialArc && !Application.isPlaying)
        {
            for (int i = 0; i < totalSlots; i++)
            {
                if (slots[i] != null && slots[i].transform is RectTransform rect)
                {
                    rect.anchoredPosition = GetTargetPositionForSlot(i);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (slots == null || slots.Length == 0) return;

        Gizmos.matrix = transform.localToWorldMatrix;

        Vector3 center = (Vector3)centerOffset;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);

        int count = slots.Length;
        float step = count > 1 && Mathf.Approximately(Mathf.Abs(arcSpreadDeg), 360f) 
            ? (arcSpreadDeg / count) 
            : (count > 1 ? (arcSpreadDeg / (count - 1)) : 0f);

        for (int i = 0; i < count; i++)
        {
            Vector3 targetPos;
            if (layoutMode == WheelLayoutMode.ManualWaypoints && customWaypointTransforms != null && i < customWaypointTransforms.Length && customWaypointTransforms[i] != null)
            {
                targetPos = customWaypointTransforms[i].localPosition;
            }
            else if (layoutMode == WheelLayoutMode.ManualWaypoints && slots[i] != null)
            {
                targetPos = slots[i].transform.localPosition;
            }
            else
            {
                float angleDeg = startAngleDeg - (i * step);
                float angleRad = angleDeg * Mathf.Deg2Rad;
                targetPos = center + new Vector3(Mathf.Cos(angleRad) * radius, Mathf.Sin(angleRad) * radius, 0f);
            }

            Gizmos.color = (i == 0) ? Color.green : new Color(0.3f, 0.7f, 1f, 0.8f);
            Gizmos.DrawWireSphere(targetPos, 16f);
            Gizmos.DrawLine(center, targetPos);
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
