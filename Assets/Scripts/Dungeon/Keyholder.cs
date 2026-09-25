using System;
using UnityEngine;
using Presentation.Effects;

namespace Dungeon
{
    /// <summary>
    /// Component marking an enemy as a Keyholder for a locked child chest room.
    /// Attaches an active golden key particle / visual aura to the enemy.
    /// On death, emits a flying key particle directly to the player without any physical ground pickup.
    /// </summary>
    public class Keyholder : MonoBehaviour
    {
        [Header("Keyholder Configuration")]
        [SerializeField] private int targetChestRoomIndex = -1;
        [SerializeField] private float auraBobSpeed = 3f;
        [SerializeField] private float auraBobHeight = 0.15f;

        private GameObject _auraRoot;
        private Transform _auraTransform;
        private Vector3 _auraBaseLocalPos = new Vector3(0f, 1.2f, 0f);
        private bool _hasEmittedKey = false;

        public int TargetChestRoomIndex
        {
            get => targetChestRoomIndex;
            set => targetChestRoomIndex = value;
        }

        public GameObject AuraObject => _auraRoot;

        public void Initialize(int chestRoomIndex)
        {
            targetChestRoomIndex = chestRoomIndex;
            CreateKeyAura();
        }

        private void Start()
        {
            if (_auraRoot == null)
            {
                CreateKeyAura();
            }
        }

        private void OnEnable()
        {
            EventBus<EntityDiedEvent>.Subscribe(OnEntityDied);
        }

        private void OnDisable()
        {
            EventBus<EntityDiedEvent>.Unsubscribe(OnEntityDied);
        }

        private void Update()
        {
            if (_auraTransform != null)
            {
                float bob = Mathf.Sin(Time.time * auraBobSpeed) * auraBobHeight;
                _auraTransform.localPosition = _auraBaseLocalPos + new Vector3(0f, bob, 0f);
            }
        }

        private void CreateKeyAura()
        {
            if (_auraRoot != null) return;

            _auraRoot = new GameObject("KeyAura");
            _auraRoot.transform.SetParent(transform, false);
            _auraRoot.transform.localPosition = _auraBaseLocalPos;
            _auraTransform = _auraRoot.transform;

            var sr = _auraRoot.AddComponent<SpriteRenderer>();
            sr.sprite = FlyingKeyParticle.GetOrCreateKeySprite();
            sr.sortingOrder = 90; // Above enemy body
            sr.color = new Color(1f, 0.9f, 0.25f, 0.95f);
            _auraRoot.transform.localScale = Vector3.one * 0.7f;
        }

        private void OnEntityDied(EntityDiedEvent evt)
        {
            if (evt.Entity == gameObject)
            {
                EmitKeyToPlayer();
            }
        }

        public void EmitKeyToPlayer()
        {
            if (_hasEmittedKey) return;
            _hasEmittedKey = true;

            if (_auraRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_auraRoot);
                }
                else
                {
                    DestroyImmediate(_auraRoot);
                }
                _auraRoot = null;
            }

            Transform playerTransform = null;
            var playerMove = FindAnyObjectByType<PlayerMovement>();
            if (playerMove != null)
            {
                playerTransform = playerMove.transform;
            }

            FlyingKeyParticle.Spawn(transform.position, playerTransform, keys: 1);
        }

        private void OnDestroy()
        {
            if (_auraRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_auraRoot);
                }
                else
                {
                    DestroyImmediate(_auraRoot);
                }
            }
        }
    }
}
