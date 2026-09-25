using System.Collections;
using UnityEngine;
using Presentation.Effects;

namespace Presentation.Entities
{
    /// Presenter view for destructible props (e.g. barrels, crates) on the game board.
    /// Handles break / destruction VFX particle instantiation, destruction events,
    /// and flying heart restoration particles for barrels.
    public class PropTileObject : TileObject
    {
        [Header("Prop Destruction")]
        [SerializeField] private GameObject breakEffectPrefab;
        [SerializeField] private AudioClip breakSound;

        [Header("Heart Restore (Barrels)")]
        [SerializeField] private bool restoresHeartOnDestruction = true;
        [Range(0f, 1f)]
        [SerializeField] private float heartDropChance = 1.0f;
        [SerializeField] private Sprite heartSprite;
        [SerializeField] private int healthRestoreAmount = 1;

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

        public bool RestoresHeartOnDestruction
        {
            get => restoresHeartOnDestruction;
            set => restoresHeartOnDestruction = value;
        }

        public float HeartDropChance
        {
            get => heartDropChance;
            set => heartDropChance = Mathf.Clamp01(value);
        }

        public Sprite HeartSprite
        {
            get => heartSprite;
            set => heartSprite = value;
        }

        public int HealthRestoreAmount
        {
            get => healthRestoreAmount;
            set => healthRestoreAmount = Mathf.Max(1, value);
        }

        public override IEnumerator AnimateDeathRoutine()
        {
            Vector3 deathPos = transform.position;

            if (breakEffectPrefab != null)
            {
                Instantiate(breakEffectPrefab, deathPos, Quaternion.identity);
            }

            if (breakSound != null)
            {
                AudioSource.PlayClipAtPoint(breakSound, deathPos);
            }

            // If this is a barrel (or configured to restore hearts), launch flying heart particle directly to player
            bool isBarrel = restoresHeartOnDestruction && (gameObject.name.ToLowerInvariant().Contains("barrel") || restoresHeartOnDestruction);
            if (isBarrel && (heartDropChance >= 1.0f || Random.value <= heartDropChance))
            {
                TriggerFlyingHeartRestore(deathPos);
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

        private void TriggerFlyingHeartRestore(Vector3 startPos)
        {
            var player = Object.FindAnyObjectByType<PlayerMovement>();
            if (player != null)
            {
                FlyingHeartParticle.Spawn(
                    startPos,
                    player.transform,
                    onArrival: null,
                    customHeartSprite: heartSprite,
                    duration: 0.45f,
                    restoreAmount: healthRestoreAmount
                );
            }
            else
            {
                GameManager.Instance?.Board?.FindPlayer()?.Heal(healthRestoreAmount);
                EventBus<HeartCollectedEvent>.Raise(new HeartCollectedEvent(healthRestoreAmount, startPos));
            }
        }
    }
}
