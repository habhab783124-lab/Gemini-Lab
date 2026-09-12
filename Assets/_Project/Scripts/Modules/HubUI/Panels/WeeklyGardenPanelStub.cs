#nullable enable
using System;
using System.Globalization;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Time;
using GeminiLab.Core.UI;
using GeminiLab.Modules.EmotionGarden;
using GeminiLab.Modules.HubUI;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 每周培育面板。瓶子、花卉、土壤、集中信息栏和交互高亮都必须已经作者化在 Scene 中；
    /// 运行时只读取数据、切换已有节点状态和填充文本。
    /// </summary>
    public sealed class WeeklyGardenPanelStub : StubPanelBase
    {
        public override PanelId Id => PanelId.WeeklyGardenView;

        private static readonly string[] DayLabels = { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
        private const string CellTemplateName = "CellTemplate";

        [Header("场景作者化引用")]
        [SerializeField] private GameObject? _cellPrefab;
        [SerializeField] private Transform? _gridRoot;
        [SerializeField] private TMP_Text? _weekTitleText;
        [SerializeField] private Button? _nextWeekButton;

        [Header("集中信息栏")]
        [SerializeField] private Transform? _detailBarRoot;
        [SerializeField] private SceneAuthoredImageVariantView? _detailGrowthView;
        [SerializeField] private TMP_Text? _detailDateText;
        [SerializeField] private TMP_Text? _detailEmotionText;
        // 场景字段名沿用既有序列化引用；该文本现在展示 AI 返回的花朵简介。
        [SerializeField] private TMP_Text? _detailFlowerLanguageText;

        // 这些引用保留给 Scene/Inspector 和 authoring 使用；运行时不写入 Image.sprite。
        [Header("已作者化资源索引")]
        [SerializeField] private Sprite[]? _dayLabelSprites;
        [FormerlySerializedAs("_growthSprites")]
        [SerializeField] private Sprite[]? _flowerHeadIconSprites;
        [SerializeField] private Sprite? _uiBarSprite;
        [SerializeField] private EmotionFlowerArtCatalog? _flowerArtCatalog;

        private IEmotionGardenService? _service;
        private EventBus? _eventBus;
        private IDisposable? _submittedSub;
        private IDisposable? _bloomedSub;
        private IDisposable? _clearedSub;
        private readonly GameObject[] _cells = new GameObject[7];
        private int _viewedWeekId;
        private int _selectedDayIndex = -1;

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);
            _service ??= ServiceLocator.TryResolve(out IEmotionGardenService? service) ? service : null;
            _eventBus ??= ServiceLocator.TryResolve(out EventBus? eventBus) ? eventBus : null;
            if (_service == null)
            {
                return;
            }

            EnsureSubscriptions();
            HideCellTemplate();
            _viewedWeekId = _service.GetCurrentWeekId();
            _selectedDayIndex = -1;
            Refresh();
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
            if (_eventBus == null)
            {
                return;
            }

            _submittedSub ??= _eventBus.Subscribe<EmotionFlowerSubmittedEvent>(_ => Refresh());
            _bloomedSub ??= _eventBus.Subscribe<EmotionFlowerBloomedEvent>(_ => Refresh());
            _clearedSub ??= _eventBus.Subscribe<EmotionGardenClearedEvent>(_ => Refresh());
        }

        public void ShowPrevWeek()
        {
            if (_service == null)
            {
                return;
            }

            _viewedWeekId = _service.OffsetWeekId(_viewedWeekId, -1);
            _selectedDayIndex = -1;
            Refresh();
        }

        public void ShowNextWeek()
        {
            if (_service == null || _viewedWeekId >= _service.GetCurrentWeekId())
            {
                return;
            }

            _viewedWeekId = _service.OffsetWeekId(_viewedWeekId, 1);
            _selectedDayIndex = -1;
            Refresh();
        }

        /// <summary>选择某一天的瓶子；再次点击当前瓶子不会取消，取消统一由空白区域处理。</summary>
        public void SelectDay(int dayIndex)
        {
            if (_service == null || dayIndex < 0 || dayIndex >= _cells.Length)
            {
                return;
            }

            _selectedDayIndex = dayIndex;
            ApplySelectionVisuals();
            RefreshDetailBar(_service.GetWeekFlowers(_viewedWeekId));
        }

        /// <summary>点击面板空白区域后恢复默认日期信息。</summary>
        public void ClearDaySelection()
        {
            _selectedDayIndex = -1;
            ApplySelectionVisuals();
            if (_service != null)
            {
                RefreshDetailBar(_service.GetWeekFlowers(_viewedWeekId));
            }
        }

        private void Refresh()
        {
            if (_service == null || _gridRoot == null)
            {
                return;
            }

            int currentWeekId = _service.GetCurrentWeekId();
            if (_weekTitleText != null)
            {
                int year = _viewedWeekId / 100;
                int week = _viewedWeekId % 100;
                DateTime monday = _service.GetWeekStartDate(_viewedWeekId);
                DateTime sunday = monday.AddDays(6);
                string suffix = _viewedWeekId == currentWeekId ? "（本周）" : string.Empty;
                _weekTitleText.text = $"{year} 第 {week} 周 ({monday:MM-dd} ~ {sunday:MM-dd}){suffix}";
            }

            var flowers = _service.GetWeekFlowers(_viewedWeekId);
            EnsureCells();

            for (int i = 0; i < 7; i++)
            {
                var flower = flowers[i];
                GameObject? cell = _cells[i];
                if (cell == null)
                {
                    continue;
                }

                var dayView = cell.transform.Find("DayLabel/DaySprite")?.GetComponent<SceneAuthoredImageVariantView>();
                var dayText = cell.transform.Find("DayLabel/DayText")?.GetComponent<TextMeshProUGUI>();
                if (dayView != null)
                {
                    dayView.Show(SceneAuthoredImageVariantView.BuildKey("day", string.Empty, i.ToString()));
                    if (dayText != null)
                    {
                        dayText.enabled = false;
                    }
                }
                else if (dayText != null)
                {
                    dayText.text = DayLabels[i];
                    dayText.enabled = true;
                }

                var bottleView = cell.transform.Find("Bottle")?.GetComponent<SceneAuthoredImageVariantView>();
                bottleView?.ShowPreview();

                bool isGrowing = flower.HasValue && flower.Value.State == GrowthState.Growing;
                string? flowerVariantKey = flower.HasValue && flower.Value.State == GrowthState.Bloomed &&
                    _flowerArtCatalog?.Resolve(flower.Value.EmotionType, flower.Value.Owner, GrowthState.Bloomed) != null
                    ? SceneAuthoredImageVariantView.BuildFlowerKey(
                        flower.Value.EmotionType,
                        flower.Value.Owner,
                        GrowthState.Bloomed)
                    : null;

                var flowerView = cell.transform.Find("FlowerImage")?.GetComponent<SceneAuthoredImageVariantView>();
                if (flowerView != null)
                {
                    if (flowerVariantKey != null)
                    {
                        flowerView.Show(flowerVariantKey);
                    }
                    else
                    {
                        flowerView.Hide();
                    }
                }

                var soilImage = cell.transform.Find("SoilImage")?.GetComponent<Image>();
                if (soilImage != null)
                {
                    bool showSoil = flower.HasValue && (isGrowing || flowerVariantKey != null);
                    soilImage.enabled = showSoil;
                    soilImage.gameObject.SetActive(showSoil);
                }

                var legacyLabel = cell.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
                if (legacyLabel != null)
                {
                    legacyLabel.gameObject.SetActive(false);
                }
            }

            RefreshDetailBar(flowers);
            ApplySelectionVisuals();
        }

        private void RefreshDetailBar(EmotionFlowerData?[] flowers)
        {
            if (_detailBarRoot == null)
            {
                return;
            }

            int displayIndex = ResolveDisplayDayIndex();
            EmotionFlowerData? flower = displayIndex >= 0 && displayIndex < flowers.Length
                ? flowers[displayIndex]
                : null;

            if (!flower.HasValue)
            {
                _detailGrowthView?.Hide();
                SetDetailText("---", "---", "---");
                return;
            }

            var data = flower.Value;
            _detailGrowthView?.Show(SceneAuthoredImageVariantView.BuildKey(
                "flower-head",
                EmotionFlowerCatalog.NormalizeOwner(data.Owner),
                EmotionFlowerCatalog.NormalizeEmotionType(data.EmotionType)));

            string flowerName = string.IsNullOrWhiteSpace(data.FlowerName)
                ? EmotionFlowerCatalog.ResolveFlowerName(data.EmotionType, data.Owner)
                : data.FlowerName;
            string emotionName = EmotionFlowerCatalog.ResolveEmotionDisplayName(data.EmotionType);
            string stateText = data.State == GrowthState.Bloomed ? "已开花" : "培育中";
            string keywordText = data.EmotionKeywords != null && data.EmotionKeywords.Length > 0
                ? string.Join("、", data.EmotionKeywords)
                : emotionName;
            string descriptionText = string.IsNullOrWhiteSpace(data.FlowerDescription)
                ? $"{flowerName}的简介暂未生成"
                : data.FlowerDescription.Trim();
            SetDetailText(
                FormatFlowerDate(data.DateIso),
                $"{emotionName}\n关键词：{keywordText}",
                $"{flowerName}\n简介：{descriptionText}\n{stateText}");
        }

        private void SetDetailText(string date, string emotion, string flowerDescription)
        {
            if (_detailDateText != null) _detailDateText.text = date;
            if (_detailEmotionText != null) _detailEmotionText.text = emotion;
            if (_detailFlowerLanguageText != null) _detailFlowerLanguageText.text = flowerDescription;
        }

        private int ResolveDisplayDayIndex()
        {
            if (_selectedDayIndex >= 0 && _selectedDayIndex < _cells.Length)
            {
                return _selectedDayIndex;
            }

            if (_service == null || _viewedWeekId != _service.GetCurrentWeekId())
            {
                return 0;
            }

            if (!ServiceLocator.TryResolve(out IGameClock? clock) || clock == null)
            {
                return 0;
            }

            DateTime monday = _service.GetWeekStartDate(_viewedWeekId).Date;
            int dayIndex = (clock.Now.Date - monday).Days;
            return dayIndex >= 0 && dayIndex < _cells.Length ? dayIndex : 0;
        }

        private void ApplySelectionVisuals()
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                if (_cells[i] == null)
                {
                    continue;
                }

                var interaction = _cells[i].transform.Find("Bottle")?.GetComponent<WeeklyGardenBottleInteraction>();
                interaction?.SetSelected(i == _selectedDayIndex);
            }
        }

        private static string FormatFlowerDate(string dateIso)
        {
            if (DateTime.TryParseExact(dateIso, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime date))
            {
                return date.ToString("MM-dd", CultureInfo.InvariantCulture);
            }

            return string.IsNullOrWhiteSpace(dateIso) ? "---" : dateIso;
        }

        private void EnsureCells()
        {
            if (_gridRoot == null)
            {
                return;
            }

            HideCellTemplate();
            for (int i = 0; i < _cells.Length; i++)
            {
                if (_cells[i] != null)
                {
                    continue;
                }

                Transform? existing = _gridRoot.Find($"Day{i}");
                if (existing == null)
                {
                    Debug.LogError($"[WeeklyGardenPanelStub] Scene 缺少作者化格子 Day{i}，不会在运行时创建。", this);
                    continue;
                }

                existing.gameObject.SetActive(true);
                _cells[i] = existing.gameObject;
            }
        }

        private void HideCellTemplate()
        {
            if (_gridRoot == null)
            {
                return;
            }

            Transform? template = _gridRoot.Find(CellTemplateName);
            if (template != null && template.gameObject.activeSelf)
            {
                template.gameObject.SetActive(false);
            }
        }
    }
}
