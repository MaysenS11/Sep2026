using UnityEngine;

namespace Dungeon
{
    [RequireComponent(typeof(Animator))]
    public class Chest : MonoBehaviour, IInteractable
    {
        private static readonly int IsOpenHash = Animator.StringToHash("isOpen");

        [SerializeField] private Animator animator;
        [SerializeField] private bool isOpen;
        [SerializeField] private bool isChestRoom;

        public bool IsOpen => isOpen;
        public bool CanInteract => !isOpen;
        public bool IsChestRoom => isChestRoom;

        public void SetChestRoom(bool value)
        {
            isChestRoom = value;
        }

        private void Awake()
        {
            EnsureAnimator();
        }

        private void Reset()
        {
            EnsureAnimator();
        }

        private void OnValidate()
        {
            EnsureAnimator();
        }

        private void EnsureAnimator()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        public void Interact(GameObject player)
        {
            TryOpen();
        }

        public bool TryOpen()
        {
            if (isOpen) return false;

            EnsureAnimator();
            isOpen = true;
            if (animator != null)
            {
                animator.SetBool(IsOpenHash, true);
            }

            EventBus<ChestOpenedEvent>.Raise(new ChestOpenedEvent(transform.position, isChestRoom));
            return true;
        }
    }
}
