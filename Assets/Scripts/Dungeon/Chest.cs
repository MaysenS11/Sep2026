using UnityEngine;

namespace Dungeon
{
    [RequireComponent(typeof(Animator))]
    public class Chest : MonoBehaviour
    {
        private static readonly int IsOpenHash = Animator.StringToHash("isOpen");

        [SerializeField] private Animator animator;
        [SerializeField] private bool isOpen;

        public bool IsOpen => isOpen;

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

        public bool TryOpen()
        {
            if (isOpen) return false;

            EnsureAnimator();
            isOpen = true;
            if (animator != null)
            {
                animator.SetBool(IsOpenHash, true);
            }
            return true;
        }
    }
}
