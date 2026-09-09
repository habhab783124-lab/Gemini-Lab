#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using GeminiLab.Modules.EmotionGarden;
using GeminiLab.Modules.HubUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// WorldMap flower codex panel. The visual hierarchy is authored in the scene;
    /// runtime only fills data and switches between list/detail views.
    /// </summary>
    public sealed class FlowerCollectionPanelStub : StubPanelBase
    {
        public override PanelId Id => PanelId.EmotionCollection;

        private const int FirstFlowerNumber = 27;

        [Header("Views")]
        [SerializeField] private GameObject? _codexView;
        [SerializeField] private GameObject? _detailView;

        [Header("Codex")]
        [SerializeField] private FlowerCodexCardSlot[] _cardSlots = Array.Empty<FlowerCodexCardSlot>();
        [SerializeField] private TMP_Text? _progressText;
        [SerializeField] private TMP_Text? _pageText;
        [SerializeField] private Button? _previousPageButton;
        [SerializeField] private Button? _nextPageButton;

        [Header("Detail")]
        [SerializeField] private SceneAuthoredImageVariantView? _detailFlowerView;
        [SerializeField] private Image? _detailSoilImage;
        [SerializeField] private TMP_Text? _detailNumberText;
        [SerializeField] private TMP_Text? _detailNameText;
        [SerializeField] private TMP_Text? _detailCreatedText;
        [SerializeField] private TMP_Text? _detailEmotionText;
        [SerializeField] private TMP_Text? _detailOwnerText;
        [SerializeField] private TMP_Text? _detailPhraseTitleText;
        [SerializeField] private TMP_Text? _detailPhraseBodyText;
        [SerializeField] private TMP_Text? _detailStockText;
        [SerializeField] private Button? _detailBackButton;
        [SerializeField] private Button? _detailPreviousButton;
        [SerializeField] private Button? _detailNextButton;
        [SerializeField] private Button? _detailCloseButton;

        [Header("Art")]
        [SerializeField] private EmotionFlowerArtCatalog? _flowerArtCatalog;

        private IEmotionGardenService? _service;
        private EventBus? _eventBus;
        private IDisposable? _submittedSub;
        private IDisposable? _bloomedSub;
        private IDisposable? _clearedSub;
        private readonly List<ClusterProgress> _clusters = new();
        private int _currentPage;
        private int _selectedClusterIndex = -1;

        protected override void Awake()
        {
            base.Awake();

            if (_previousPageButton != null) _previousPageButton.onClick.AddListener(ShowPreviousPage);
            if (_nextPageButton != null) _nextPageButton.onClick.AddListener(ShowNextPage);
            if (_detailBackButton != null) _detailBackButton.onClick.AddListener(ShowCodexView);
            if (_detailPreviousButton != null) _detailPreviousButton.onClick.AddListener(ShowPreviousDetail);
            if (_detailNextButton != null) _detailNextButton.onClick.AddListener(ShowNextDetail);
            if (_detailCloseButton != null) _detailCloseButton.onClick.AddListener(CloseSelf);

            for (int i = 0; i < _cardSlots.Length; i++)
            {
                int slotIndex = i;
                _cardSlots[i].BindClick(() => OnCardClicked(slotIndex));
            }
        }

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);
            _service ??= ServiceLocator.TryResolve(out IEmotionGardenService? service) ? service : null;
            _eventBus ??= ServiceLocator.TryResolve(out EventBus? eventBus) ? eventBus : null;
            _currentPage = 0;
            _selectedClusterIndex = -1;
            EnsureSubscriptions();
            RefreshClusters();
            ShowCodexView();
        }

        public override void OnClose()
        {
            _submittedSub?.Dispose();
            _bloomedSub?.Dispose();
            _clearedSub?.Dispose();
            _submittedSub = null;
            _bloomedSub = null;
            _clearedSub = null;
            base.OnClose();
        }

        protected override void OnDestroy()
        {
            _submittedSub?.Dispose();
            _bloomedSub?.Dispose();
            _clearedSub?.Dispose();
            base.OnDestroy();
        }

        private void EnsureSubscriptions()
        {
            if (_eventBus == null) return;

            _submittedSub ??= _eventBus.Subscribe<EmotionFlowerSubmittedEvent>(_ =>
            {
                RefreshClusters();
                RefreshVisibleView();
            });
            _bloomedSub ??= _eventBus.Subscribe<EmotionFlowerBloomedEvent>(_ =>
            {
                RefreshClusters();
                RefreshVisibleView();
            });
            _clearedSub ??= _eventBus.Subscribe<EmotionGardenClearedEvent>(_ =>
            {
                RefreshClusters();
                RefreshVisibleView();
            });
        }

        private void RefreshClusters()
        {
            _clusters.Clear();

            if (_service != null)
            {
                // Empty catalog entries are not authored placeholder cards.
                // Keep only flowers that have actually been collected so the
                // runtime never exposes a grid of unknown/empty cards.
                foreach (ClusterProgress cluster in _service.GetAllClusters())
                {
                    if (cluster.TotalCount > 0 && cluster.UnlockedStage > 0)
                    {
                        _clusters.Add(cluster);
                    }
                }
                _clusters.Sort(CompareClusters);
            }

            int maxPage = GetMaxPage();
            _currentPage = Mathf.Clamp(_currentPage, 0, maxPage);
        }

        private void RefreshVisibleView()
        {
            if (_detailView != null && _detailView.activeSelf)
            {
                RefreshDetail();
                return;
            }

            RefreshCodexCards();
        }

        private void ShowCodexView()
        {
            if (_codexView != null) _codexView.SetActive(true);
            if (_detailView != null) _detailView.SetActive(false);
            RefreshCodexCards();
        }

        private void RefreshCodexCards()
        {
            int slotCount = Mathf.Max(1, _cardSlots.Length);
            int startIndex = _currentPage * slotCount;
            int displayTotal = Mathf.Max(slotCount, _clusters.Count);

            for (int i = 0; i < _cardSlots.Length; i++)
            {
                int clusterIndex = startIndex + i;
                bool hasDisplaySlot = clusterIndex >= 0 && clusterIndex < displayTotal;
                bool hasCluster = clusterIndex >= 0 && clusterIndex < _clusters.Count;
                ClusterProgress cluster = hasCluster ? _clusters[clusterIndex] : default;
                bool unlocked = hasCluster && cluster.TotalCount > 0 && cluster.UnlockedStage > 0;
                int displayNumber = FirstFlowerNumber + clusterIndex;

                _cardSlots[i].Set(
                    hasDisplaySlot,
                    displayNumber,
                    unlocked,
                    unlocked ? BuildFlowerName(cluster) : "未知花卉",
                    unlocked ? BuildCardMeta(cluster) : string.Empty,
                    unlocked ? ResolveBloomedFlowerVariantKey(cluster) : null);
            }

            int collected = 0;
            for (int i = 0; i < _clusters.Count; i++)
            {
                if (_clusters[i].TotalCount > 0 && _clusters[i].UnlockedStage > 0)
                {
                    collected++;
                }
            }

            if (_progressText != null)
            {
                _progressText.text = $"收集进度：{collected} / {displayTotal}";
            }

            if (_pageText != null)
            {
                _pageText.text = $"{_currentPage + 1} / {GetMaxPage() + 1}";
            }

            if (_previousPageButton != null)
            {
                _previousPageButton.interactable = _currentPage > 0;
            }

            if (_nextPageButton != null)
            {
                _nextPageButton.interactable = _currentPage < GetMaxPage();
            }
        }

        private void OnCardClicked(int slotIndex)
        {
            int clusterIndex = _currentPage * Mathf.Max(1, _cardSlots.Length) + slotIndex;
            if (clusterIndex < 0 || clusterIndex >= _clusters.Count)
            {
                return;
            }

            ClusterProgress cluster = _clusters[clusterIndex];
            if (cluster.TotalCount <= 0 || cluster.UnlockedStage <= 0)
            {
                return;
            }

            ShowDetail(clusterIndex);
        }

        private void ShowDetail(int clusterIndex)
        {
            if (clusterIndex < 0 || clusterIndex >= _clusters.Count)
            {
                return;
            }

            _selectedClusterIndex = clusterIndex;
            if (_codexView != null) _codexView.SetActive(false);
            if (_detailView != null) _detailView.SetActive(true);
            RefreshDetail();
        }

        private void RefreshDetail()
        {
            if (_selectedClusterIndex < 0 || _selectedClusterIndex >= _clusters.Count)
            {
                return;
            }

            ClusterProgress cluster = _clusters[_selectedClusterIndex];
            int displayNumber = FirstFlowerNumber + _selectedClusterIndex;
            string flowerName = BuildFlowerName(cluster);
            string owner = ResolveOwnerName(cluster.Owner);
            string emotion = ResolveEmotionName(cluster.EmotionType);

            if (_detailNumberText != null) _detailNumberText.text = $"No. {displayNumber:000}";
            if (_detailNameText != null) _detailNameText.text = flowerName;
            if (_detailCreatedText != null) _detailCreatedText.text = $"累计收集 {cluster.TotalCount} 朵";
            if (_detailEmotionText != null) _detailEmotionText.text = emotion;
            if (_detailOwnerText != null) _detailOwnerText.text = owner;
            if (_detailPhraseTitleText != null) _detailPhraseTitleText.text = BuildPhraseTitle(cluster);
            if (_detailPhraseBodyText != null) _detailPhraseBodyText.text = BuildPhraseBody(cluster);
            if (_detailStockText != null) _detailStockText.text = $"{cluster.TotalCount}";

            string? detailFlowerVariantKey = ResolveBloomedFlowerVariantKey(cluster);
            if (_detailFlowerView != null)
            {
                if (detailFlowerVariantKey != null)
                {
                    _detailFlowerView.Show(detailFlowerVariantKey);
                }
                else
                {
                    _detailFlowerView.Hide();
                }
            }

            if (_detailSoilImage != null)
            {
                // 土壤只属于每周培育面板；图鉴详情中的花朵不显示土壤。
                _detailSoilImage.enabled = false;
                _detailSoilImage.gameObject.SetActive(false);
            }

            if (_detailPreviousButton != null)
            {
                _detailPreviousButton.interactable = FindPreviousUnlockedIndex(_selectedClusterIndex) >= 0;
            }

            if (_detailNextButton != null)
            {
                _detailNextButton.interactable = FindNextUnlockedIndex(_selectedClusterIndex) >= 0;
            }
        }

        private void ShowPreviousPage()
        {
            if (_currentPage <= 0) return;
            _currentPage--;
            RefreshCodexCards();
        }

        private void ShowNextPage()
        {
            if (_currentPage >= GetMaxPage()) return;
            _currentPage++;
            RefreshCodexCards();
        }

        private void ShowPreviousDetail()
        {
            int previous = FindPreviousUnlockedIndex(_selectedClusterIndex);
            if (previous >= 0)
            {
                ShowDetail(previous);
            }
        }

        private void ShowNextDetail()
        {
            int next = FindNextUnlockedIndex(_selectedClusterIndex);
            if (next >= 0)
            {
                ShowDetail(next);
            }
        }

        private int FindPreviousUnlockedIndex(int fromIndex)
        {
            for (int i = fromIndex - 1; i >= 0; i--)
            {
                if (_clusters[i].TotalCount > 0 && _clusters[i].UnlockedStage > 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindNextUnlockedIndex(int fromIndex)
        {
            for (int i = fromIndex + 1; i < _clusters.Count; i++)
            {
                if (_clusters[i].TotalCount > 0 && _clusters[i].UnlockedStage > 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private int GetMaxPage()
        {
            int slotCount = Mathf.Max(1, _cardSlots.Length);
            int itemCount = Mathf.Max(slotCount, _clusters.Count);
            return Mathf.Max(0, (itemCount - 1) / slotCount);
        }

        private static int CompareClusters(ClusterProgress a, ClusterProgress b)
        {
            int emotion = EmotionFlowerCatalog.GetEmotionSortIndex(a.EmotionType)
                .CompareTo(EmotionFlowerCatalog.GetEmotionSortIndex(b.EmotionType));
            if (emotion != 0) return emotion;

            int owner = EmotionFlowerCatalog.GetOwnerSortIndex(a.Owner)
                .CompareTo(EmotionFlowerCatalog.GetOwnerSortIndex(b.Owner));
            if (owner != 0) return owner;

            return string.Compare(a.Owner, b.Owner, StringComparison.Ordinal);
        }

        private static string BuildFlowerName(ClusterProgress cluster)
        {
            return EmotionFlowerCatalog.ResolveFlowerName(cluster.EmotionType, cluster.Owner);
        }

        private static string BuildCardMeta(ClusterProgress cluster)
        {
            return $"{ResolveOwnerName(cluster.Owner)} · {cluster.TotalCount}";
        }

        private string? ResolveBloomedFlowerVariantKey(ClusterProgress cluster)
        {
            if (_flowerArtCatalog?.Resolve(cluster.EmotionType, cluster.Owner, GrowthState.Bloomed) == null)
            {
                return null;
            }

            return SceneAuthoredImageVariantView.BuildFlowerKey(
                cluster.EmotionType,
                cluster.Owner,
                GrowthState.Bloomed);
        }

        private static string BuildPhraseTitle(ClusterProgress cluster)
        {
            return cluster.UnlockedStage >= 3 ? "花丛已盛放" : "花朵已记录";
        }

        private static string BuildPhraseBody(ClusterProgress cluster)
        {
            string emotion = ResolveEmotionName(cluster.EmotionType);
            string owner = ResolveOwnerName(cluster.Owner);
            return $"{owner}培育出的{emotion}之花，记录着一次被看见的心情。继续收集同类情绪花，可提升该花卉的收集进度。";
        }

        private static string ResolveOwnerName(string owner)
        {
            return EmotionFlowerCatalog.ResolveOwnerDisplayName(owner);
        }

        private static string ResolveEmotionName(string emotionType)
        {
            return EmotionFlowerCatalog.ResolveEmotionDisplayName(emotionType);
        }

        [Serializable]
        private sealed class FlowerCodexCardSlot
        {
            [SerializeField] private Button? _button;
            [SerializeField] private Image? _cardImage;
            [SerializeField] private Image? _lockedImage;
            [SerializeField] private SceneAuthoredImageVariantView? _flowerView;
            [SerializeField] private Image? _soilImage;
            [SerializeField] private GameObject? _unlockedContent;
            [SerializeField] private TMP_Text? _numberText;
            [SerializeField] private TMP_Text? _nameText;
            [SerializeField] private TMP_Text? _metaText;

            public void BindClick(UnityEngine.Events.UnityAction action)
            {
                if (_button != null)
                {
                    _button.onClick.AddListener(action);
                }
            }

            public void Set(bool visible, int number, bool unlocked, string name, string meta, string? flowerVariantKey)
            {
                if (_button != null) _button.gameObject.SetActive(visible);
                if (_button != null) _button.interactable = visible;
                if (_cardImage != null) _cardImage.enabled = visible && unlocked;
                if (_lockedImage != null) _lockedImage.gameObject.SetActive(visible && !unlocked);
                if (_flowerView != null)
                {
                    bool showFlower = visible && unlocked && flowerVariantKey != null;
                    if (showFlower)
                    {
                        _flowerView.gameObject.SetActive(true);
                        _flowerView.Show(flowerVariantKey!);
                    }
                    else
                    {
                        _flowerView.Hide();
                        _flowerView.gameObject.SetActive(false);
                    }
                }
                if (_soilImage != null)
                {
                    // 图鉴卡片只展示花朵本身，土壤不属于图鉴视觉。
                    _soilImage.enabled = false;
                    _soilImage.gameObject.SetActive(false);
                }
                if (_unlockedContent != null) _unlockedContent.SetActive(visible && unlocked);
                if (_numberText != null) _numberText.text = $"No. {number:000}";
                if (_nameText != null) _nameText.text = name;
                if (_metaText != null) _metaText.text = meta;
            }
        }
    }
}
