using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class UIButtonSoundTrigger : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
{
    [SerializeField] private bool playHover = true;
    [SerializeField] private bool playClick = true;

    private Selectable selectable;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (playHover && (selectable == null || selectable.interactable))
        {
            EventBus<PlayUISoundEvent>.Raise(new PlayUISoundEvent(UISoundType.Hover));
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (playClick && (selectable == null || selectable.interactable))
        {
            EventBus<PlayUISoundEvent>.Raise(new PlayUISoundEvent(UISoundType.Click));
        }
    }
}
