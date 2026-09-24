using System.Collections;
using UnityEngine;

namespace Presentation.Entities
{
    /// Presenter view for chests on the game board.
    /// Handles lid open state visual transition, animator parameters, and loot particle effects.
    public class ChestTileObject : TileObject
    {
        private static readonly int IsOpenHash = Animator.StringToHash("isOpen");

        [Header("Chest Visuals")]
        [SerializeField] private Sprite openedSprite;
        [SerializeField] private GameObject lootEffectPrefab;
        [SerializeField] private bool isOpen = false;

        public bool IsOpen => isOpen;

        public override IEnumerator AnimateChestOpenRoutine(string lootInfo)
        {
            EnsureComponents();
            if (isOpen)
            {
                yield break;
            }

            isOpen = true;

            if (animator != null && HasParameter(animator, IsOpenHash))
            {
                animator.SetBool(IsOpenHash, true);
            }

            if (openedSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = openedSprite;
            }

            if (lootEffectPrefab != null)
            {
                Instantiate(lootEffectPrefab, transform.position, Quaternion.identity);
            }

            yield return new WaitForSeconds(0.25f);
        }
    }
}
