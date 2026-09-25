using System;
using System.Collections;
using UnityEngine;
using Core.Board;
using Core.Occupants;

namespace Presentation.Effects
{
    /// <summary>
    /// Lightweight presenter particle that flies directly from a destroyed barrel into the player,
    /// restoring 1 HP on arrival without any ground drop or pickup step.
    /// </summary>
    public class FlyingHeartParticle : MonoBehaviour
    {
        private static Sprite s_ProceduralHeartSprite;

        [Header("Flight Settings")]
        [SerializeField] private float flightDuration = 0.45f;
        [SerializeField] private float arcPeakHeight = 1.0f;
        [SerializeField] private int healAmount = 1;

        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Transform _targetTransform;
        private Action _onArrival;

        public static FlyingHeartParticle Spawn(
            Vector3 startPosition,
            Transform targetTransform,
            Action onArrival = null,
            Sprite customHeartSprite = null,
            float duration = 0.45f,
            int restoreAmount = 1)
        {
            var go = new GameObject("FlyingHeartParticle");
            go.transform.position = startPosition;

            var heart = go.AddComponent<FlyingHeartParticle>();
            heart.flightDuration = duration;
            heart.healAmount = restoreAmount;
            heart._targetTransform = targetTransform;
            heart._onArrival = onArrival;

            var sr = go.AddComponent<SpriteRenderer>();
            heart.spriteRenderer = sr;
            sr.sortingOrder = 100; // Render above floor and props

            if (customHeartSprite != null)
            {
                sr.sprite = customHeartSprite;
            }
            else
            {
                sr.sprite = GetOrCreateHeartSprite();
            }

            sr.color = Color.white;
            go.transform.localScale = Vector3.one * 0.9f;

            heart.StartFlight();
            return heart;
        }

        private void StartFlight()
        {
            if (Application.isPlaying)
            {
                StartCoroutine(FlightRoutine());
            }
            else
            {
                ApplyRestore();
                _onArrival?.Invoke();
            }
        }

        private IEnumerator FlightRoutine()
        {
            Vector3 startPos = transform.position;
            float elapsed = 0f;

            Vector3 startScale = Vector3.one * 0.4f;
            Vector3 targetScale = Vector3.one * 1.1f;

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

                // Pulse scale
                float scaleT = Mathf.Sin(progress * Mathf.PI);
                transform.localScale = Vector3.Lerp(startScale, targetScale, scaleT);

                yield return null;
            }

            // On arrival at player:
            ApplyRestore();

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

        private void ApplyRestore()
        {
            Vector3 arrivalPos = _targetTransform != null ? _targetTransform.position : transform.position;


            // 2. Restore PlayerOccupant on authoritative GameBoard if available
            if (GameManager.Instance != null && GameManager.Instance.Board != null)
            {
                PlayerOccupant playerOccupant = GameManager.Instance.Board.FindPlayer();
                if (playerOccupant != null)
                {
                    playerOccupant.Heal(healAmount);
                }
            }

            // 3. Emit event bus notification
            EventBus<HeartCollectedEvent>.Raise(new HeartCollectedEvent(healAmount, arrivalPos));
        }

        /// <summary>
        /// Creates a crisp procedural 16x16 red pixel heart sprite fallback if no custom sprite is assigned.
        /// </summary>
        public static Sprite GetOrCreateHeartSprite()
        {
            if (s_ProceduralHeartSprite != null) return s_ProceduralHeartSprite;

            int width = 16;
            int height = 16;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };

            Color clear = new Color(0, 0, 0, 0);
            Color red = new Color(0.95f, 0.15f, 0.22f, 1f);
            Color outline = new Color(0.35f, 0.05f, 0.08f, 1f);
            Color highlight = new Color(1f, 0.65f, 0.7f, 1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            // 16x16 Heart Pixel Map
            string[] heartMap = new string[]
            {
                "................",
                "................",
                "....XXXX..XXXX..",
                "..XXRRRRXXRRRRX.",
                ".XRRHHRRRRRRRRRX",
                ".XRHHRRRRRRRRRRX",
                ".XRRRRRRRRRRRRRX",
                "..XRRRRRRRRRRRX.",
                "...XRRRRRRRRRX..",
                "....XRRRRRRRX...",
                ".....XRRRRRX....",
                "......XRRRX.....",
                ".......XRX......",
                "........X.......",
                "................",
                "................"
            };

            for (int y = 0; y < 16; y++)
            {
                string row = heartMap[15 - y]; // invert Y for texture coordinate
                for (int x = 0; x < 16; x++)
                {
                    char c = row[x];
                    if (c == 'X') texture.SetPixel(x, y, outline);
                    else if (c == 'R') texture.SetPixel(x, y, red);
                    else if (c == 'H') texture.SetPixel(x, y, highlight);
                }
            }

            texture.Apply();
            s_ProceduralHeartSprite = Sprite.Create(
                texture,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                16f
            );

            return s_ProceduralHeartSprite;
        }
    }
}
