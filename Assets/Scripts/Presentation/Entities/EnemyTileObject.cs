using System.Collections;
using UnityEngine;

namespace Presentation.Entities
{
    /// Presenter view for enemies on the game board.
    /// Manages directional sprite flipping and Animator trigger synchronization.
    public class EnemyTileObject : TileObject
    {
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int WalkHash = Animator.StringToHash("Walk");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
        private static readonly int TakeDamageHash = Animator.StringToHash("TakeDamage");
        private static readonly int HurtHash = Animator.StringToHash("Hurt");
        private static readonly int DieHash = Animator.StringToHash("Die");

        /// Flips the sprite based on horizontal movement direction.
        public virtual void UpdateFacingDirection(Vector2 direction)
        {
            EnsureComponents();
            if (spriteRenderer == null) return;

            if (direction.x > 0.01f)
            {
                spriteRenderer.flipX = false;
            }
            else if (direction.x < -0.01f)
            {
                spriteRenderer.flipX = true;
            }
        }

        public override IEnumerator AnimateMoveRoutine(Vector2Int[] path, float duration)
        {
            if (path == null || path.Length == 0)
            {
                yield break;
            }

            if (path.Length > 1)
            {
                Vector2 initialDir = path[1] - path[0];
                UpdateFacingDirection(initialDir);
            }

            SetAnimatorBool(IsMovingHash, true);
            SetAnimatorTrigger(WalkHash);

            // Execute base path interpolation, updating facing direction on each segment
            int segmentCount = path.Length - 1;
            if (segmentCount > 0 && duration > 0f)
            {
                float segDuration = duration / segmentCount;

                for (int i = 0; i < segmentCount; i++)
                {
                    Vector2 segDir = path[i + 1] - path[i];
                    UpdateFacingDirection(segDir);

                    Vector3 segStart = (i == 0) ? transform.position : GridToWorldCenter(path[i]);
                    Vector3 segEnd = GridToWorldCenter(path[i + 1]);

                    float elapsed = 0f;
                    while (elapsed < segDuration)
                    {
                        elapsed += Time.deltaTime;
                        float progress = Mathf.Clamp01(elapsed / segDuration);
                        float t = (i == segmentCount - 1)
                            ? Mathf.Sin(progress * Mathf.PI * 0.5f)
                            : progress;

                        transform.position = Vector3.Lerp(segStart, segEnd, t);
                        yield return null;
                    }

                    CurrentGridPosition = path[i + 1];
                }

                SnapToGrid(path[path.Length - 1]);
            }
            else
            {
                yield return base.AnimateMoveRoutine(path, duration);
            }

            SetAnimatorBool(IsMovingHash, false);
        }

        public override IEnumerator AnimateJumpRoutine(Vector2Int start, Vector2Int end, float duration, float arcHeight = 1.0f)
        {
            UpdateFacingDirection(end - start);
            SetAnimatorTrigger(WalkHash);
            yield return base.AnimateJumpRoutine(start, end, duration, arcHeight);
        }

        public override IEnumerator AnimateAttackLungeRoutine(Vector2Int targetPos, float duration)
        {
            UpdateFacingDirection(targetPos - CurrentGridPosition);

            SetAnimatorTrigger(AttackHash);
            SetAnimatorTrigger(IsAttackingHash);

            yield return base.AnimateAttackLungeRoutine(targetPos, duration);
        }

        public override IEnumerator AnimateBlockedRecoilRoutine(Vector2Int targetPos, Vector2Int returnPos, float duration)
        {
            UpdateFacingDirection(targetPos - CurrentGridPosition);

            SetAnimatorTrigger(AttackHash);
            SetAnimatorTrigger(IsAttackingHash);

            yield return base.AnimateBlockedRecoilRoutine(targetPos, returnPos, duration);
        }

        public override IEnumerator AnimateDamageRoutine(int damage)
        {
            SetAnimatorTrigger(TakeDamageHash);
            SetAnimatorTrigger(HurtHash);

            yield return base.AnimateDamageRoutine(damage);
        }

        [Header("Stun Visual")]
        [SerializeField] private Sprite stunSprite;
        private GameObject stunIndicatorObj;
        private Core.Occupants.EnemyOccupant boundOccupant;

        public void BindOccupant(Core.Occupants.EnemyOccupant occupant)
        {
            if (boundOccupant != null)
            {
                boundOccupant.OnStunChanged -= OnStunChanged;
            }
            boundOccupant = occupant;
            if (boundOccupant != null)
            {
                boundOccupant.OnStunChanged += OnStunChanged;
                SetStunned(boundOccupant.IsStunned);
            }
        }

        private void OnDestroy()
        {
            if (boundOccupant != null)
            {
                boundOccupant.OnStunChanged -= OnStunChanged;
            }
        }

        private void OnStunChanged(bool isStunned)
        {
            SetStunned(isStunned);
        }

        public void SetStunned(bool stunned)
        {
            if (stunned)
            {
                EnsureComponents();
                if (stunIndicatorObj == null)
                {
                    Transform existing = transform.Find("StunIndicator");
                    if (existing != null)
                    {
                        stunIndicatorObj = existing.gameObject;
                    }
                    else
                    {
                        stunIndicatorObj = new GameObject("StunIndicator");
                        stunIndicatorObj.transform.SetParent(transform, false);
                    }

                    if (!stunIndicatorObj.TryGetComponent<SpriteRenderer>(out var sr))
                    {
                        sr = stunIndicatorObj.AddComponent<SpriteRenderer>();
                    }

                    if (stunSprite == null)
                    {
                        var eb = GetComponent<EnemyBase>();
                        if (eb != null && eb.Settings != null)
                        {
                            stunSprite = eb.Settings.StunSprite;
                        }
                    }

#if UNITY_EDITOR
                    if (stunSprite == null)
                    {
                        stunSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Indicators/Stun.png");
                    }
#endif

                    sr.sprite = stunSprite;

                    if (spriteRenderer != null)
                    {
                        sr.sortingLayerID = spriteRenderer.sortingLayerID;
                        sr.sortingOrder = spriteRenderer.sortingOrder + 10;
                    }

                    Vector3 pos = new Vector3(0f, 0.75f, 0f);
                    if (spriteRenderer != null && spriteRenderer.sprite != null)
                    {
                        pos = new Vector3(0f, spriteRenderer.bounds.size.y * 0.6f + 0.1f, 0f);
                    }
                    stunIndicatorObj.transform.localPosition = pos;
                }

                stunIndicatorObj.SetActive(true);
            }
            else
            {
                if (stunIndicatorObj != null)
                {
                    stunIndicatorObj.SetActive(false);
                }
            }
        }

        public override IEnumerator AnimateDeathRoutine()
        {
            SetStunned(false);
            SetAnimatorTrigger(DieHash);
            yield return base.AnimateDeathRoutine();
        }

        private void SetAnimatorBool(int hash, bool value)
        {
            if (animator != null && HasParameter(animator, hash))
            {
                animator.SetBool(hash, value);
            }
        }

        private void SetAnimatorTrigger(int hash)
        {
            if (animator != null && HasParameter(animator, hash))
            {
                animator.SetTrigger(hash);
            }
        }
    }
}
