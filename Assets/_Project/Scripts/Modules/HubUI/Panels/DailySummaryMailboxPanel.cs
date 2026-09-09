#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using GeminiLab.Modules.EmotionGarden;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 邮箱里的 AI 每日小结。面板和文本节点由 Apartment Scene 作者化，运行时只读取存档并刷新文字。
    /// </summary>
    public sealed class DailySummaryMailboxPanel : StubPanelBase
    {
        public override PanelId Id => PanelId.DailySummaryMailbox;

        [SerializeField] private TMP_Text? _summaryText;
        [SerializeField] private TMP_Text? _angelNoteText;
        [SerializeField] private TMP_Text? _devilNoteText;

        [Header("AI diary date list")]
        [SerializeField] private DailySummaryDateOption[] _dateOptions = Array.Empty<DailySummaryDateOption>();

        [Header("AI diary detail targets")]
        [SerializeField] private Button? _angelNoteButton;
        [SerializeField] private Button? _summaryButton;
        [SerializeField] private Button? _devilNoteButton;
        [SerializeField] private Button? _angelCardButton;
        [SerializeField] private Button? _devilCardButton;
        [SerializeField] private Button? _popupButton;
        [SerializeField] private DailySummaryDetailPopup? _detailPopup;

        private IEmotionGardenService? _gardenService;
        private EventBus? _eventBus;
        private IDisposable? _submittedSubscription;
        private string _selectedDateIso = string.Empty;

        protected override void Awake()
        {
            base.Awake();
            BindDetailButtons();
        }

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);
            ResolveServices();
            SubscribeToFlowerSubmission();
            Refresh();
        }

        public override void OnClose()
        {
            _detailPopup?.Hide();
            _submittedSubscription?.Dispose();
            _submittedSubscription = null;
            base.OnClose();
        }

        protected override void OnDestroy()
        {
            UnbindDetailButtons();
            _submittedSubscription?.Dispose();
            _submittedSubscription = null;
            base.OnDestroy();
        }

        private void BindDetailButtons()
        {
            foreach (DailySummaryDateOption option in _dateOptions)
            {
                if (option.Button == null) continue;
                DailySummaryDateOption capturedOption = option;
                option.Button.onClick.AddListener(() => OnDateOptionClicked(capturedOption));
            }

            _angelNoteButton?.onClick.AddListener(OpenAngelNote);
            _summaryButton?.onClick.AddListener(OpenSummary);
            _devilNoteButton?.onClick.AddListener(OpenDevilNote);
            _angelCardButton?.onClick.AddListener(OpenAngelCard);
            _devilCardButton?.onClick.AddListener(OpenDevilCard);
            _popupButton?.onClick.AddListener(OpenPopup);
        }

        private void UnbindDetailButtons()
        {
            foreach (DailySummaryDateOption option in _dateOptions)
            {
                if (option.Button == null) continue;
                option.Button.onClick.RemoveAllListeners();
            }

            _angelNoteButton?.onClick.RemoveListener(OpenAngelNote);
            _summaryButton?.onClick.RemoveListener(OpenSummary);
            _devilNoteButton?.onClick.RemoveListener(OpenDevilNote);
            _angelCardButton?.onClick.RemoveListener(OpenAngelCard);
            _devilCardButton?.onClick.RemoveListener(OpenDevilCard);
            _popupButton?.onClick.RemoveListener(OpenPopup);
        }

        private void OnDateOptionClicked(DailySummaryDateOption option)
        {
            if (option == null || string.IsNullOrWhiteSpace(option.DateIso))
            {
                return;
            }

            _selectedDateIso = option.DateIso;
            Refresh();
        }

        private void OpenAngelNote()
        {
            _detailPopup?.Show(
                DailySummaryDetailPopup.DetailKind.AngelNote,
                "天使便签",
                _angelNoteText != null ? _angelNoteText.text : string.Empty);
        }

        private void OpenSummary()
        {
            _detailPopup?.Show(
                DailySummaryDetailPopup.DetailKind.Summary,
                string.IsNullOrWhiteSpace(_selectedDateIso) ? "今日小结" : _selectedDateIso,
                _summaryText != null ? _summaryText.text : string.Empty);
        }

        private void OpenDevilNote()
        {
            _detailPopup?.Show(
                DailySummaryDetailPopup.DetailKind.DevilNote,
                "恶魔便签",
                _devilNoteText != null ? _devilNoteText.text : string.Empty);
        }

        private void OpenAngelCard()
        {
            _detailPopup?.Show(DailySummaryDetailPopup.DetailKind.AngelCard, "天使卡", string.Empty);
        }

        private void OpenDevilCard()
        {
            _detailPopup?.Show(DailySummaryDetailPopup.DetailKind.DevilCard, "恶魔卡", string.Empty);
        }

        private void OpenPopup()
        {
            _detailPopup?.Show(DailySummaryDetailPopup.DetailKind.Popup, "", string.Empty);
        }

        private void ResolveServices()
        {
            if (_gardenService == null)
            {
                ServiceLocator.TryResolve(out _gardenService);
            }

            if (_eventBus == null)
            {
                ServiceLocator.TryResolve(out _eventBus);
            }
        }

        private void SubscribeToFlowerSubmission()
        {
            if (_eventBus == null || _submittedSubscription != null)
            {
                return;
            }

            _submittedSubscription = _eventBus.Subscribe<EmotionFlowerSubmittedEvent>(_ => Refresh());
        }

        private void Refresh()
        {
            ResolveServices();
            if (_gardenService == null)
            {
                SetEmptyState("情绪花园尚未准备好");
                return;
            }

            IReadOnlyList<string> dates = _gardenService.GetDailySummaryDates();
            if (dates.Count == 0)
            {
                SetEmptyState();
                return;
            }

            if (string.IsNullOrWhiteSpace(_selectedDateIso) || !ContainsDate(dates, _selectedDateIso))
            {
                _selectedDateIso = dates[0];
            }

            ApplyDateOptions(dates);
            EmotionDailySummaryData? summary = _gardenService.GetDailySummary(_selectedDateIso);
            if (!summary.HasValue)
            {
                SetEmptyState();
                return;
            }

            EmotionDailySummaryData data = summary.Value;
            SetText(_summaryText, string.IsNullOrWhiteSpace(data.Summary) ? "---" : data.Summary);
            SetText(_angelNoteText, string.IsNullOrWhiteSpace(data.AngelNote) ? "---" : data.AngelNote);
            SetText(_devilNoteText, string.IsNullOrWhiteSpace(data.DevilNote) ? "---" : data.DevilNote);
        }

        private void ApplyDateOptions(IReadOnlyList<string> dates)
        {
            for (int index = 0; index < _dateOptions.Length; index++)
            {
                DailySummaryDateOption option = _dateOptions[index];
                if (index >= dates.Count)
                {
                    option.Clear();
                    continue;
                }

                string dateIso = dates[index];
                option.SetDate(dateIso, string.Equals(dateIso, _selectedDateIso, StringComparison.Ordinal), true);
                if (option.Button != null)
                {
                    option.Button.interactable = true;
                }
            }
        }

        private void SetEmptyState(string summaryPlaceholder = "---")
        {
            foreach (DailySummaryDateOption option in _dateOptions)
            {
                option.Clear();
            }

            SetText(_summaryText, summaryPlaceholder);
            SetText(_angelNoteText, "---");
            SetText(_devilNoteText, "---");
        }

        private static bool ContainsDate(IReadOnlyList<string> dates, string dateIso)
        {
            for (int index = 0; index < dates.Count; index++)
            {
                if (string.Equals(dates[index], dateIso, StringComparison.Ordinal)) return true;
            }

            return false;
        }

        private static void SetText(TMP_Text? target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
