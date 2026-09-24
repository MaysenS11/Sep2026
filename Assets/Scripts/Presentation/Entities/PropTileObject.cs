using System.Collections;
using UnityEngine;

namespace Presentation.Entities
{
    /// Presenter view for destructible props (e.g. barrels, crates) on the game board.
    /// Handles break / destruction VFX particle instantiation and destruction events.
    public class PropTileObject : TileObject
    {
        [Header("Prop Destruction")]
        [SerializeField] private GameObject breakEffectPrefab;
        [SerializeField] private AudioClip breakSound;

        public GameObject BreakEffectPrefab
        {
            get => breakEffectPrefab;
            set => breakEffectPrefab = value;
        }

        public AudioClip BreakSound
        {
            get => breakSound;
            set => breakSound = value;
        }

        public override IEnumerator AnimateDeathRoutine()
        {
            if (breakEffectPrefab != null)
            {
                Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
            }

            if (breakSound != null)
            {
                AudioSource.PlayClipAtPoint(breakSound, transform.position);
            }

            EventBus<PropDestroyedEvent>.Raise(new PropDestroyedEvent(gameObject));
            EventBus<EntityDiedEvent>.Raise(new EntityDiedEvent(gameObject));

            gameObject.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }

            yield break;
        }
    }
}
