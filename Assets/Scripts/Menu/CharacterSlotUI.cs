using UnityEngine;
using UnityEngine.UI;

public class CharacterSlotUI : MonoBehaviour
{
    [SerializeField] private CharacterDefinition characterDefinition;
    [SerializeField] private Image maskImage;
    [SerializeField] private Button selectButton;

    private bool isLocked;

    public CharacterDefinition CharacterDefinition => characterDefinition;
    public bool IsLocked => isLocked;

    private void Awake()
    {
        if (selectButton == null) selectButton = GetComponent<Button>();
        if (maskImage == null && selectButton != null) maskImage = selectButton.targetGraphic as Image;
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
            maskImage.color = isLocked ? characterDefinition.LockedTint : Color.white;
        }

        if (selectButton != null)
        {
            selectButton.interactable = !isLocked;
        }
    }

    private void OnCharacterUnlocked(CharacterUnlockedEvent evt)
    {
        if (characterDefinition != null && evt.CharacterId == characterDefinition.CharacterId)
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
