using System;
using System.Collections;
using UnityEngine;
using Core.Occupants;

namespace Presentation.Effects
{
    /// <summary>
    /// Direct key delivery presenter particle that flies in a smooth parabolic arc
    /// from a defeated Keyholder directly into the player, incrementing HUD key count
    /// and player key inventory immediately with no physical ground pickup step.
    /// </summary>
    public class FlyingKeyParticle : MonoBehaviour
    {
        private static Sprite s_ProceduralKeySprite;

        [Header("Flight Settings")]
        [SerializeField] private float flightDuration = 0.5f;
        [SerializeField] private float arcPeakHeight = 1.2f;
        [SerializeField] private int keyAmount = 1;

        public int Keys => keyAmount;

        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Transform _targetTransform;
        private Action _onArrival;

        public static FlyingKeyParticle Spawn(
            Vector3 startPosition,
            Transform targetTransform,
            Action onArrival = null,
            Sprite customKeySprite = null,
            float duration = 0.5f,
            int keys = 1)
        {
            var go = new GameObject("FlyingKeyParticle");
            go.transform.position = startPosition;

            var keyParticle = go.AddComponent<FlyingKeyParticle>();
            keyParticle.flightDuration = duration;
            keyParticle.keyAmount = keys;
            keyParticle._targetTransform = targetTransform;
            keyParticle._onArrival = onArrival;

            var sr = go.AddComponent<SpriteRenderer>();
            keyParticle.spriteRenderer = sr;
            sr.sortingOrder = 100; // Above floor and characters

            if (customKeySprite != null)
            {
                sr.sprite = customKeySprite;
            }
            else
            {
                sr.sprite = GetOrCreateKeySprite();
            }

            sr.color = new Color(1f, 0.9f, 0.3f, 1f); // Rich golden tint
            go.transform.localScale = Vector3.one * 1.0f;

            keyParticle.StartFlight();
            return keyParticle;
        }

        private void StartFlight()
        {
            if (Application.isPlaying)
            {
                StartCoroutine(FlightRoutine());
            }
            else
            {
                ApplyKeyDelivery();
                _onArrival?.Invoke();
            }
        }

        private IEnumerator FlightRoutine()
        {
            Vector3 startPos = transform.position;
            float elapsed = 0f;

            Vector3 startScale = Vector3.one * 0.5f;
            Vector3 targetScale = Vector3.one * 1.2f;

            while (elapsed < flightDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / flightDuration);

                // Smooth ease in-out
                float t = Mathf.SmoothStep(0f, 1f, progress);

                Vector3 currentTargetPos = _targetTransform != null ? _targetTransform.position : startPos;
                Vector3 basePos = Vector3.Lerp(startPos, currentTargetPos, t);

                // Parabolic arc: 4 * h * p * (1 - p)
                float arc = 4f * arcPeakHeight * progress * (1f - progress);
                transform.position = new Vector3(basePos.x, basePos.y + arc, basePos.z);

                // Pulse scale & rotate slightly
                float scaleT = Mathf.Sin(progress * Mathf.PI);
                transform.localScale = Vector3.Lerp(startScale, targetScale, scaleT);
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(progress * Mathf.PI * 4f) * 15f);

                yield return null;
            }

            // On arrival at player:
            ApplyKeyDelivery();

            _onArrival?.Invoke();

            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        private void ApplyKeyDelivery()
        {
            // 1. Deliver to HUD / UIManager
            var ui = UIManager.Instance;
            if (ui != null)
            {
                ui.AddKey(keyAmount);
            }

            // 2. Deliver to PlayerOccupant on authoritative GameBoard if present
            if (GameManager.Instance != null && GameManager.Instance.Board != null)
            {
                PlayerOccupant player = GameManager.Instance.Board.FindPlayer();
                if (player != null)
                {
                    player.Keys += keyAmount;
                }
            }

            // 3. Raise event bus notification
            EventBus<KeyCollectedEvent>.Raise(new KeyCollectedEvent(keyAmount));
        }

        /// <summary>
        /// Creates a crisp procedural 16x16 golden pixel key sprite fallback.
        /// </summary>
        public static Sprite GetOrCreateKeySprite()
        {
            if (s_ProceduralKeySprite != null) return s_ProceduralKeySprite;

            int width = 16;
            int height = 16;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };

            Color clear = new Color(0, 0, 0, 0);
            Color gold = new Color(1f, 0.85f, 0.15f, 1f);
            Color goldDark = new Color(0.7f, 0.5f, 0.05f, 1f);
            Color highlight = new Color(1f, 0.98f, 0.65f, 1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            // 16x16 Golden Key Pixel Map
            string[] keyMap = new string[]
            {
                "................",
                "....GGGGGG......",
                "...GGHHHHGG.....",
                "..GGH....HGG....",
                "..GGH....HGG....",
                "...GGHHHHGG.....",
                "....GGGGGG......",
                "......GG........",
                "......GG..GG....",
                "......GG..GG....",
                "......GG........",
                "......GG..GG....",
                "......GG..GG....",
                "......GG........",
                "......GG........",
                "................"
            };

            for (int y = 0; y < 16; y++)
            {
                string row = keyMap[15 - y];
                for (int x = 0; x < 16; x++)
                {
                    char c = row[x];
                    if (c == 'G') texture.SetPixel(x, y, gold);
                    else if (c == 'H') texture.SetPixel(x, y, highlight);
                    else if (c == 'D') texture.SetPixel(x, y, goldDark);
                }
            }

            texture.Apply();
            s_ProceduralKeySprite = Sprite.Create(
                texture,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                16f
            );

            return s_ProceduralKeySprite;
        }
    }
}
