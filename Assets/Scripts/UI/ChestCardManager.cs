using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Chest
{
    public class ChestCardManager : MonoBehaviour
    {
        public static ChestCardManager Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private StatCardDatabase cardDatabase;
        [SerializeField] private ChestCardUI cardPrefab;
        [SerializeField] private Transform cardContainer;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform[] cardPlaceholders;
        [SerializeField] private ChestStatDisplayUI statDisplayUI;

        [Header("Reveal Animation")]
        [SerializeField] private float revealDuration = 0.5f;
        [SerializeField] private float staggerDelay = 0.2f;
        [SerializeField] private float startScale = 0.05f;
        [SerializeField] private Color startColor = Color.black;

        [Header("UI Controls")]
        [SerializeField] private TMP_Text instructionText;

        private readonly List<ChestCardUI> spawnedCards = new List<ChestCardUI>();
        private readonly List<ChestCardUI> selectedCards = new List<ChestCardUI>();
        private int maxPicks = 1;
        private CanvasGroup canvasGroup;
        private PlayerStats cachedPlayerStats;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (panelRoot != null && panelRoot != gameObject)
            {
                panelRoot.SetActive(false);
            }
            else
            {
                SetPanelVisible(false);
            }
        }

        private void OnEnable()
        {
            EventBus<ChestOpenedEvent>.Subscribe(OnChestOpened);
        }

        private void OnDisable()
        {
            EventBus<ChestOpenedEvent>.Unsubscribe(OnChestOpened);
        }

        private void OnChestOpened(ChestOpenedEvent evt)
        {
            OpenReward(evt.IsChestRoom, evt.ChestPosition);
        }

        public void OpenReward(bool isChestRoom)
        {
            OpenReward(isChestRoom, Vector3.zero);
        }

        public void OpenReward(bool isChestRoom, Vector3 chestWorldPos)
        {
            if (cachedPlayerStats == null)
            {
                cachedPlayerStats = FindAnyObjectByType<PlayerStats>();
            }

            if (cachedPlayerStats == null || cardDatabase == null || cardPrefab == null)
            {
                return;
            }

            maxPicks = isChestRoom ? 2 : 1;
            ClearSpawnedCards();

            if (panelRoot != null && panelRoot != gameObject)
            {
                panelRoot.SetActive(true);
            }
            SetPanelVisible(true);

            if (statDisplayUI != null)
            {
                statDisplayUI.Refresh();
            }

            List<CardRewardItem> cards = CardGenerator.GenerateCards(cachedPlayerStats, cardDatabase);
            if (cards.Count == 0) return;

            for (int i = 0; i < cards.Count; i++)
            {
                RectTransform placeholder = (cardPlaceholders != null && i < cardPlaceholders.Length) ? cardPlaceholders[i] : null;
                Transform parentTransform = placeholder != null ? placeholder.parent : cardContainer;

                ChestCardUI cardUI = Instantiate(cardPrefab, parentTransform);
                RectTransform cardRect = cardUI.GetComponent<RectTransform>();

                if (placeholder != null)
                {
                    cardRect.anchorMin = placeholder.anchorMin;
                    cardRect.anchorMax = placeholder.anchorMax;
                    cardRect.pivot = placeholder.pivot;
                    cardRect.anchoredPosition = placeholder.anchoredPosition;
                    cardRect.sizeDelta = placeholder.sizeDelta;
                    cardRect.localRotation = placeholder.localRotation;
                    cardRect.SetSiblingIndex(placeholder.GetSiblingIndex() + 1);

                    placeholder.gameObject.SetActive(false);
                }

                cardUI.Setup(cards[i], OnCardClicked);
                cardUI.PrepareForReveal(startScale, startColor);
                spawnedCards.Add(cardUI);
            }

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                float delay = i * staggerDelay;
                spawnedCards[i].AnimateReveal(delay, revealDuration, startScale, startColor);
            }

            UpdateUIState();
        }

        private void OnCardClicked(ChestCardUI cardUI)
        {
            if (selectedCards.Contains(cardUI))
            {
                return;
            }

            selectedCards.Add(cardUI);
            cardUI.SetSelected(true);

            ApplyReward(cardUI.CurrentItem);

            if (statDisplayUI != null)
            {
                statDisplayUI.Refresh();
            }

            if (selectedCards.Count >= maxPicks)
            {
                CloseReward();
            }
            else
            {
                UpdateUIState();
            }
        }

        private void ApplyReward(CardRewardItem item)
        {
            if (cachedPlayerStats == null)
            {
                cachedPlayerStats = FindAnyObjectByType<PlayerStats>();
            }

            if (cachedPlayerStats != null)
            {
                if (item.IsHeal)
                {
                    cachedPlayerStats.ResetHealth();
                }
                else
                {
                    cachedPlayerStats.UpgradeStat(item.StatType);
                }
            }
        }

        private void UpdateUIState()
        {
            if (instructionText != null)
            {
                instructionText.text = maxPicks > 1 
                    ? $"Choose {maxPicks} cards ({selectedCards.Count}/{maxPicks})" 
                    : "Choose a card";
            }
        }

        public void CloseReward()
        {
            ClearSpawnedCards();
            if (panelRoot != null && panelRoot != gameObject)
            {
                panelRoot.SetActive(false);
            }
            SetPanelVisible(false);
            EventBus<ChestRewardClosedEvent>.Raise(new ChestRewardClosedEvent());
        }

        private void SetPanelVisible(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }
        }

        private void ClearSpawnedCards()
        {
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] != null)
                {
                    Destroy(spawnedCards[i].gameObject);
                }
            }
            spawnedCards.Clear();
            selectedCards.Clear();

            if (cardPlaceholders != null)
            {
                for (int i = 0; i < cardPlaceholders.Length; i++)
                {
                    if (cardPlaceholders[i] != null)
                    {
                        cardPlaceholders[i].gameObject.SetActive(true);
                    }
                }
            }
        }
    }
}
