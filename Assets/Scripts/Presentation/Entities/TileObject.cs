using System.Collections;
using UnityEngine;
using Core.Board;

namespace Presentation.Entities
{
    /// Base MonoBehaviour presenter for all board visual entities.
    /// Strictly decoupled from simulation state and physics engines.
    /// Contains NO Rigidbody2D, NO Collider2D, and NO Physics2D queries.
    /// Manages purely visual transform interpolations (tweens) and sprite/animator states.
    public class TileObject : MonoBehaviour
    {
        [Header("Grid Identity")]
        [SerializeField] private int occupantId;
        [SerializeField] private Vector2Int currentGridPosition;

        [Header("Visual Components")]
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] protected Animator animator;

        public int OccupantId
        {
            get => occupantId;
            set => occupantId = value;
        }

        public Vector2Int CurrentGridPosition
        {
            get => currentGridPosition;
            protected set => currentGridPosition = value;
        }

        protected virtual void Awake()
        {
            EnsureComponents();
        }

        public void EnsureComponents()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = GetComponentInChildren<SpriteRenderer>();
                }
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
            }
        }

        public void SetVisualComponents(SpriteRenderer sr, Animator anim = null)
        {
            spriteRenderer = sr;
            animator = anim;
        }

        #region Coordinate Conversion & Snapping

        /// Converts discrete grid coordinate to continuous world space centered on the cell (x + 0.5f, y + 0.5f, z).
        public static Vector3 GridToWorld(Vector2Int gridPos, float z = 0f)
        {
            return new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, z);
        }

        /// Converts continuous world coordinate to discrete grid cell coordinate via mathematical floor rounding.
        public static Vector2Int WorldToGrid(Vector3 worldPos)
        {
            return new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
        }

        /// Gets the world center position of a grid cell maintaining this object's Z depth.
        public Vector3 GridToWorldCenter(Vector2Int gridPos)
        {
            return GridToWorld(gridPos, transform.position.z);
        }

        /// Directly positions the transform at the exact mathematical center of the target grid cell.
        public virtual void SnapToGrid(Vector2Int gridPos)
        {
            CurrentGridPosition = gridPos;
            transform.position = GridToWorldCenter(gridPos);
        }

        #endregion

        #region Core Animation Coroutines

        /// Smoothly interpolates transform along waypoints, easing out at final tile center.
        public virtual IEnumerator AnimateMoveRoutine(Vector2Int[] path, float duration)
        {
            if (path == null || path.Length == 0)
            {
                yield break;
            }

            if (path.Length == 1)
            {
                SnapToGrid(path[0]);
                yield break;
            }

            if (duration <= 0f)
            {
                SnapToGrid(path[path.Length - 1]);
                yield break;
            }

            int segmentCount = path.Length - 1;
            float segDuration = duration / segmentCount;

            for (int i = 0; i < segmentCount; i++)
            {
                if (this == null || transform == null) yield break;
                Vector3 segStart = (i == 0) ? transform.position : GridToWorldCenter(path[i]);
                Vector3 segEnd = GridToWorldCenter(path[i + 1]);

                float elapsed = 0f;
                while (elapsed < segDuration)
                {
                    if (this == null || transform == null) yield break;
                    elapsed += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsed / segDuration);

                    // Apply ease-out on the final segment approaching destination
                    float t = (i == segmentCount - 1)
                        ? Mathf.Sin(progress * Mathf.PI * 0.5f)
                        : progress;

                    transform.position = Vector3.Lerp(segStart, segEnd, t);
                    yield return null;
                }

                CurrentGridPosition = path[i + 1];
            }

            if (this != null && transform != null)
            {
                SnapToGrid(path[path.Length - 1]);
            }
        }

        /// Parabolic jump arc with ease-out landing at end tile center.
        public virtual IEnumerator AnimateJumpRoutine(Vector2Int start, Vector2Int end, float duration, float arcHeight = 1.0f)
        {
            if (duration <= 0f)
            {
                if (this != null && transform != null) SnapToGrid(end);
                yield break;
            }

            Vector3 startPos = GridToWorldCenter(start);
            Vector3 endPos = GridToWorldCenter(end);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (this == null || transform == null) yield break;
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                // Ease-out horizontal progress
                float groundProgress = Mathf.Sin(progress * Mathf.PI * 0.5f);
                Vector3 groundPos = Vector3.Lerp(startPos, endPos, groundProgress);

                // Parabolic vertical arc: 4 * h * p * (1 - p) reaches peak height h at p = 0.5
                float arc = 4f * arcHeight * progress * (1f - progress);

                transform.position = new Vector3(groundPos.x, groundPos.y + arc, groundPos.z);
                yield return null;
            }

            if (this != null && transform != null)
            {
                SnapToGrid(end);
            }
        }

        /// Lunges 35% toward target tile center and returns smoothly to starting center.
        public virtual IEnumerator AnimateAttackLungeRoutine(Vector2Int targetPos, float duration)
        {
            if (duration <= 0f)
            {
                if (this != null && transform != null) SnapToGrid(CurrentGridPosition);
                yield break;
            }

            Vector3 startWorld = GridToWorldCenter(CurrentGridPosition);
            Vector3 targetWorld = GridToWorldCenter(targetPos);
            Vector3 lungeTarget = Vector3.Lerp(startWorld, targetWorld, 0.35f);

            float outDuration = duration * 0.35f;
            float returnDuration = duration * 0.65f;

            // Lunge forward
            float elapsed = 0f;
            while (elapsed < outDuration)
            {
                if (this == null || transform == null) yield break;
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / outDuration);
                float t = Mathf.Sin(progress * Mathf.PI * 0.5f);
                transform.position = Vector3.Lerp(startWorld, lungeTarget, t);
                yield return null;
            }

            // Return to start tile center
            elapsed = 0f;
            while (elapsed < returnDuration)
            {
                if (this == null || transform == null) yield break;
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / returnDuration);
                float t = Mathf.SmoothStep(0f, 1f, progress);
                transform.position = Vector3.Lerp(lungeTarget, startWorld, t);
                yield return null;
            }

            if (this != null && transform != null)
            {
                SnapToGrid(CurrentGridPosition);
            }
        }

        /// High-energy clash lunge toward target tile boundary, brief impact stutter/flash, and sharp recoil bounce back to returnPos tile center.
        public virtual IEnumerator AnimateBlockedRecoilRoutine(Vector2Int targetPos, Vector2Int returnPos, float duration)
        {
            if (duration <= 0f)
            {
                if (this != null && transform != null) SnapToGrid(returnPos);
                yield break;
            }

            Vector3 startWorld = GridToWorldCenter(CurrentGridPosition);
            Vector3 targetWorld = GridToWorldCenter(targetPos);
            Vector3 returnWorld = GridToWorldCenter(returnPos);

            // Clash point at 45% boundary toward target cell
            Vector3 clashPoint = Vector3.Lerp(startWorld, targetWorld, 0.45f);

            float lungeDuration = duration * 0.25f;
            float stutterDuration = duration * 0.20f;
            float recoilDuration = duration * 0.55f;

            // Phase 1: High-energy clash lunge
            float elapsed = 0f;
            while (elapsed < lungeDuration)
            {
                if (this == null || transform == null) yield break;
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / lungeDuration);
                float t = Mathf.Sin(progress * Mathf.PI * 0.5f);
                transform.position = Vector3.Lerp(startWorld, clashPoint, t);
                yield return null;
            }

            if (this == null || transform == null) yield break;

            // Phase 2: Impact stutter and flash
            Color origColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }

            elapsed = 0f;
            while (elapsed < stutterDuration)
            {
                if (this == null || transform == null) yield break;
                elapsed += Time.deltaTime;
                Vector3 jitter = new Vector3(Random.Range(-0.06f, 0.06f), Random.Range(-0.06f, 0.06f), 0f);
                transform.position = clashPoint + jitter;
                yield return null;
            }

            if (this == null || transform == null) yield break;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = origColor;
            }

            // Phase 3: Sharp recoil bounce back to return tile
            elapsed = 0f;
            while (elapsed < recoilDuration)
            {
                if (this == null || transform == null) yield break;
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / recoilDuration);
                float t = Mathf.Sin(progress * Mathf.PI * 0.5f);
                transform.position = Vector3.Lerp(clashPoint, returnWorld, t);
                yield return null;
            }

            if (this != null && transform != null)
            {
                SnapToGrid(returnPos);
            }
        }

        /// Flashes sprite red / white and applies a brief local shake.
        public virtual IEnumerator AnimateDamageRoutine(int damage)
        {
            if (this == null || transform == null) yield break;

            Color origColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.red;
            }

            Vector3 basePos = transform.position;
            float shakeDuration = 0.15f;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                if (this == null || transform == null) yield break;
                elapsed += Time.deltaTime;
                Vector3 jitter = new Vector3(Random.Range(-0.08f, 0.08f), Random.Range(-0.08f, 0.08f), 0f);
                transform.position = basePos + jitter;
                yield return null;
            }

            if (this != null && transform != null)
            {
                transform.position = basePos;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = origColor;
                }
            }
        }

        /// Plays death animation or fade/scale-down, then deactivates and destroys the GameObject.
        public virtual IEnumerator AnimateDeathRoutine()
        {
            if (this == null || transform == null) yield break;

            Vector3 origScale = transform.localScale;
            Color origColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

            float deathDuration = 0.25f;
            float elapsed = 0f;

            while (elapsed < deathDuration)
            {
                if (this == null || transform == null) yield break;
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / deathDuration);

                transform.localScale = Vector3.Lerp(origScale, Vector3.zero, progress);
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(origColor.r, origColor.g, origColor.b, 1f - progress);
                }
                yield return null;
            }

            if (this != null && gameObject != null)
            {
                gameObject.SetActive(false);
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
            }
        }

        /// Triggers chest lid open sprite or animator trigger, spawns visual particle/loot effect.
        public virtual IEnumerator AnimateChestOpenRoutine(string lootInfo)
        {
            yield return new WaitForSeconds(0.2f);
        }

        #endregion

        #region Helper Utilities

        protected static bool HasParameter(Animator anim, int paramHash)
        {
            if (anim == null) return false;
            foreach (var p in anim.parameters)
            {
                if (p.nameHash == paramHash) return true;
            }
            return false;
        }

        #endregion
    }
}
