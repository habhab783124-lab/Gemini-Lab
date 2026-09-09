#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Collection;
using GeminiLab.Modules.Apple;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    public sealed class GachaPanelController : StubPanelBase
    {
        public override PanelId Id => PanelId.Collection;

        [Header("页签按钮")]
        [SerializeField] private Button? _tabAll;
        [SerializeField] private Button? _tabAngel;
        [SerializeField] private Button? _tabDevil;
        [SerializeField] private Button? _tabPartner;

        [Header("Grid 与空态")]
        [SerializeField] private Transform? _gridRoot;
        [SerializeField] private TMP_Text? _emptyHint;

        [Header("资源与抽卡")]
        [SerializeField] private Button? _singleDrawButton;
        [SerializeField] private Button? _multiDrawButton;

        [Header("奖励弹窗")]
        [SerializeField] private GameObject? _rewardWindow;
        [SerializeField] private TMP_Text? _rewardText;
        [SerializeField] private Transform? _rewardGridRoot;
        [SerializeField] private Button? _confirmButton;
        [SerializeField] private Button? _drawAgainButton;

        [Header("动画")]
        [SerializeField] private float _drawAnimDuration = 1.5f;

        [Header("奖励图标")]
        [SerializeField] private Vector2 _rewardIconSize = new(256, 256);

        private IGachaService? _gacha;
        private IAppleService? _apple;
        private EventBus? _eventBus;
        private IDisposable? _appleChangedSub;
        private IDisposable? _collectionChangedSub;

        private string _currentTag = "all_tag";
        private int _lastDrawCount = 1;
        private readonly List<GameObject> _spawned = new();
        private readonly List<GameObject> _rewardSpawned = new();

        protected override void Awake()
        {
            base.Awake();

            if (_tabAll != null) _tabAll.onClick.AddListener(() => SwitchTag("all_tag"));
            if (_tabAngel != null) _tabAngel.onClick.AddListener(() => SwitchTag("angel_tag"));
            if (_tabDevil != null) _tabDevil.onClick.AddListener(() => SwitchTag("devil_tag"));
            if (_tabPartner != null) _tabPartner.onClick.AddListener(() => SwitchTag("partner_tag"));

            if (_singleDrawButton != null) _singleDrawButton.onClick.AddListener(OnSingleDraw);
            if (_multiDrawButton != null) _multiDrawButton.onClick.AddListener(OnMultiDraw);

            if (_confirmButton != null) _confirmButton.onClick.AddListener(CloseReward);
            if (_drawAgainButton != null) _drawAgainButton.onClick.AddListener(DrawAgain);

            if (_rewardWindow != null) _rewardWindow.SetActive(false);
        }

        protected override void OnDestroy()
        {
            _collectionChangedSub?.Dispose();
            _appleChangedSub?.Dispose();
            base.OnDestroy();
        }

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);
            EnsureServices();

            if (_eventBus != null)
            {
                _appleChangedSub ??= _eventBus.Subscribe<AppleChangedEvent>(_ => RefreshBalance());
                _collectionChangedSub ??= _eventBus.Subscribe<CollectionChangedEvent>(_ => RefreshGrid());
            }

            RefreshBalance();
            RefreshGrid();
        }

        public override void OnClose()
        {
            base.OnClose();
            _appleChangedSub?.Dispose();
            _appleChangedSub = null;
            _collectionChangedSub?.Dispose();
            _collectionChangedSub = null;
        }

        private void EnsureServices()
        {
            _gacha ??= ServiceLocator.TryResolve(out IGachaService? g) ? g : null;
            _apple ??= ServiceLocator.TryResolve(out IAppleService? a) ? a : null;
            _eventBus ??= ServiceLocator.TryResolve(out EventBus? eb) ? eb : null;
        }

        private void SwitchTag(string tag)
        {
            _currentTag = tag;
            RefreshGrid();
        }

        private void RefreshBalance()
        {
            if (_balanceText != null && _apple != null)
                _balanceText.text = $"{_apple.Balance}";

            if (_singleDrawButton != null)
                _singleDrawButton.interactable = _apple != null && _apple.Balance >= GachaService.SingleCost;
            if (_multiDrawButton != null)
                _multiDrawButton.interactable = _apple != null && _apple.Balance >= GachaService.MultiCost;
        }

        private void RefreshGrid()
        {
            foreach (var go in _spawned)
            {
                if (go != null) Destroy(go);
            }
            _spawned.Clear();

            if (_gridRoot == null) return;

            int count = 0;
            foreach (var id in GachaService.AllCollectibleIds)
            {
                if (_currentTag != "all_tag")
                {
                    if (!GachaService.CollectibleTags.TryGetValue(id, out string? tag) || tag != _currentTag)
                        continue;
                }

                SpawnGridItem(id);
                count++;
            }

            if (_emptyHint != null)
            {
                _emptyHint.gameObject.SetActive(count == 0);
                _emptyHint.text = count == 0 ? "（此页签暂无收藏品）" : string.Empty;
            }
        }

        private void SpawnGridItem(string id)
        {
            if (_gridRoot == null) return;

            bool isUnlocked = _gacha != null && _gacha.IsUnlocked(id);
            GachaService.CollectibleNames.TryGetValue(id, out string? displayName);

            var go = new GameObject($"Item_{id}");
            go.transform.SetParent(_gridRoot, false);
            int layer = _gridRoot.gameObject.layer;
            go.layer = layer;

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100, 120);

            var img = go.AddComponent<Image>();
            img.color = Color.white;

            if (isUnlocked)
            {
                var sprite = Resources.Load<Sprite>($"Sprites/Collection/collection_system/{displayName}");
                if (sprite != null) img.sprite = sprite;
            }
            else
            {
                var locked = Resources.Load<Sprite>("Sprites/Collection/collection_system/unlocked");
                if (locked != null) img.sprite = locked;
            }

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.layer = layer;
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 0);
            lrt.anchorMax = new Vector2(1, 0);
            lrt.pivot = new Vector2(0.5f, 0);
            lrt.anchoredPosition = new Vector2(0, 4);
            lrt.sizeDelta = new Vector2(0, 24);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = isUnlocked ? displayName ?? id : "???";
            tmp.fontSize = 12;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            labelGo.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();

            _spawned.Add(go);
        }

        private void OnSingleDraw()
        {
            if (_gacha == null) return;
            _lastDrawCount = 1;
            StopAllCoroutines();
            var result = _gacha.PullSingle();
            if (result.Items.Length == 0) return;
            StartCoroutine(ShowRewardAfterAnim(result));
        }

        private void OnMultiDraw()
        {
            if (_gacha == null) return;
            _lastDrawCount = 5;
            StopAllCoroutines();
            StartCoroutine(SequentialDrawRoutine(5, 1.2f, 0.3f));
        }

        private void DrawAgain()
        {
            if (_gacha == null) return;
            int count = _lastDrawCount >= 5 ? _lastDrawCount : 1;
            StopAllCoroutines();
            StartCoroutine(SequentialDrawRoutine(count, 1.2f, 0.3f));
        }

        private IEnumerator SequentialDrawRoutine(int count, float showSeconds, float gapSeconds)
        {
            for (int i = 0; i < count; i++)
            {
                var result = _gacha!.PullSingle();
                if (result.Items.Length == 0) break;
                yield return new WaitForSeconds(_drawAnimDuration);
                ShowReward(result);
                yield return new WaitForSeconds(showSeconds);
                CloseReward();
                if (i < count - 1)
                    yield return new WaitForSeconds(gapSeconds);
            }
        }

        private IEnumerator ShowRewardAfterAnim(GachaResult result)
        {
            yield return new WaitForSeconds(_drawAnimDuration);
            ShowReward(result);
        }

        private void ShowReward(GachaResult result)
        {
            if (_rewardWindow == null) return;

            foreach (var go in _rewardSpawned)
            {
                if (go != null) Destroy(go);
            }
            _rewardSpawned.Clear();

            var parts = new List<string>();
            foreach (var item in result.Items)
            {
                GachaService.CollectibleNames.TryGetValue(item.Id, out string? name);
                parts.Add(item.IsNew
                    ? $"<color=#FFD700>{name ?? item.Id}</color> NEW!"
                    : $"<color=#888>{name ?? item.Id}</color> (重复 +30金币)");

                if (_rewardGridRoot != null)
                {
                    var iconGo = new GameObject($"Reward_{item.Id}");
                    iconGo.transform.SetParent(_rewardGridRoot, false);
                    int layer = _rewardGridRoot.gameObject.layer;
                    iconGo.layer = layer;
                    var irt = iconGo.AddComponent<RectTransform>();
                    irt.sizeDelta = _rewardIconSize;
                    var iimg = iconGo.AddComponent<Image>();
                    var sprite = Resources.Load<Sprite>($"Sprites/Collection/collection_system/{name}");
                    if (sprite != null) iimg.sprite = sprite;
                    _rewardSpawned.Add(iconGo);
                }
            }

            if (result.CoinRefund > 0)
                parts.Add($"返还 {result.CoinRefund} 金币");

            if (_rewardText != null)
                _rewardText.text = string.Join("\n", parts);

            _rewardWindow.SetActive(true);
            RefreshBalance();
            RefreshGrid();
        }

        private void CloseReward()
        {
            if (_rewardWindow != null) _rewardWindow.SetActive(false);
        }
    }
}
