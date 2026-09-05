#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// Controls the authored wish-tree panel. Runtime code only changes view state and text;
    /// all final UI art and layout are authored by WorldMapWishSystemAuthoring.
    /// </summary>
    public sealed class WorldMapWishSystemController : MonoBehaviour
    {
        [Header("Authored views")]
        [SerializeField] private GameObject? _panelRoot;
        [SerializeField] private GameObject? _mainView;
        [SerializeField] private GameObject? _inputView;
        [SerializeField] private GameObject? _detailView;
        [SerializeField] private GameObject? _memoryListView;

        [Header("Authored controls")]
        [SerializeField] private Button? _wishButton;
        [SerializeField] private Button? _allButton;
        [SerializeField] private Button? _closeButton;
        [SerializeField] private Button? _submitButton;
        [SerializeField] private Button? _cancelInputButton;
        [SerializeField] private Button? _detailCloseButton;
        [SerializeField] private Button? _memoryCloseButton;
        [SerializeField] private Button? _fulfillButton;
        [SerializeField] private Button? _deleteButton;

        [Header("Authored text")]
        [SerializeField] private TMP_InputField? _inputField;
        [SerializeField] private TMP_Text? _dialogueText;
        [SerializeField] private TMP_Text? _detailContentText;
        [SerializeField] private TMP_Text? _detailCreatedText;
        [SerializeField] private TMP_Text? _detailStateText;
        [SerializeField] private TMP_Text? _detailFulfilledText;
        [SerializeField] private TMP_Text[] _memoryEntryTexts = Array.Empty<TMP_Text>();

        [Header("Authored wish-star slots on the panel tree")]
        [SerializeField] private Button[] _starSlots = Array.Empty<Button>();

        [Header("Legacy world-space slots (intentionally unused)")]
        [SerializeField] private Button[] _worldStarSlots = Array.Empty<Button>();

        [Header("Authored handbook list")]
        [SerializeField] private Button[] _memoryEntryButtons = Array.Empty<Button>();
        [SerializeField] private GameObject[] _memoryEntrySelectedVisuals = Array.Empty<GameObject>();
        [SerializeField] private ScrollRect? _memoryScrollRect;

        private WorldMapWishService? _service;
        private string _selectedWishId = string.Empty;
        private bool _bound;

        public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;
        public int VisibleWishCount => _service == null ? 0 : CountVisibleRecords();

        private void Awake()
        {
            _service = new WorldMapWishService();
            BindControls();
            ClosePanel();
        }

        private void OnDestroy() => UnbindControls();

        public void OpenPanel()
        {
            _service ??= new WorldMapWishService();
            if (_panelRoot != null) _panelRoot.SetActive(true);
            ShowMainView();
        }

        public void ClosePanel()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);
            _selectedWishId = string.Empty;
            HideAuxiliaryViews();
        }

        private void BindControls()
        {
            if (_bound) return;
            _wishButton?.onClick.AddListener(BeginWishInput);
            _allButton?.onClick.AddListener(OpenHandbook);
            _closeButton?.onClick.AddListener(ClosePanel);
            _submitButton?.onClick.AddListener(SubmitWish);
            _cancelInputButton?.onClick.AddListener(ShowMainView);
            _detailCloseButton?.onClick.AddListener(ShowMainView);
            _memoryCloseButton?.onClick.AddListener(ShowMainView);
            _fulfillButton?.onClick.AddListener(FulfillSelectedWish);
            _deleteButton?.onClick.AddListener(DeleteSelectedWish);
            BindMemoryEntryButtons();
            _bound = true;
        }

        private void UnbindControls()
        {
            if (!_bound) return;
            _wishButton?.onClick.RemoveListener(BeginWishInput);
            _allButton?.onClick.RemoveListener(OpenHandbook);
            _closeButton?.onClick.RemoveListener(ClosePanel);
            _submitButton?.onClick.RemoveListener(SubmitWish);
            _cancelInputButton?.onClick.RemoveListener(ShowMainView);
            _detailCloseButton?.onClick.RemoveListener(ShowMainView);
            _memoryCloseButton?.onClick.RemoveListener(ShowMainView);
            _fulfillButton?.onClick.RemoveListener(FulfillSelectedWish);
            _deleteButton?.onClick.RemoveListener(DeleteSelectedWish);
            UnbindMemoryEntryButtons();
            _bound = false;
        }

        private void BeginWishInput()
        {
            ShowInputView();
            if (_inputField == null) return;
            _inputField.text = string.Empty;
            _inputField.Select();
            _inputField.ActivateInputField();
        }

        private void SubmitWish()
        {
            if (_service == null || _inputField == null) return;
            WorldMapWishRecord? created = _service.CreateWish(_inputField.text);
            if (created == null)
            {
                SetText(_dialogueText, "Please write a wish first.");
                return;
            }

            SetText(_dialogueText, "Your wish has been placed on the tree.");
            ShowMainView();
        }

        /// <summary>Opens the single handbook/detail page from the authored item_button.</summary>
        private void OpenHandbook()
        {
            if (_detailView != null) _detailView.SetActive(true);
            if (_mainView != null) _mainView.SetActive(false);
            if (_inputView != null) _inputView.SetActive(false);
            if (_memoryListView != null) _memoryListView.SetActive(false);

            List<WorldMapWishRecord> history = BuildHistory();
            WorldMapWishRecord? initial = FindInitialWish(history);
            _selectedWishId = initial?.Id ?? string.Empty;
            RefreshMemoryList();
            if (initial != null) RefreshDetail(initial);
            else ClearDetail();
        }

        private void FulfillSelectedWish()
        {
            if (_service == null || string.IsNullOrWhiteSpace(_selectedWishId)) return;
            if (_service.Fulfill(_selectedWishId))
                SetText(_dialogueText, "Your wish has been fulfilled.");

            WorldMapWishRecord? record = _service.GetById(_selectedWishId);
            if (record != null) RefreshDetail(record);
            RefreshMemoryList();
            RefreshStars();
        }

        private void DeleteSelectedWish()
        {
            if (_service == null || string.IsNullOrWhiteSpace(_selectedWishId)) return;
            _service.Archive(_selectedWishId);
            _selectedWishId = string.Empty;
            SetText(_dialogueText, "The wish was archived.");
            ShowMainView();
        }

        private void ShowMainView()
        {
            if (_mainView != null) _mainView.SetActive(true);
            if (_inputView != null) _inputView.SetActive(false);
            if (_detailView != null) _detailView.SetActive(false);
            if (_memoryListView != null) _memoryListView.SetActive(false);
            RefreshStars();
            RefreshMemoryList();
        }

        private void ShowInputView()
        {
            if (_mainView != null) _mainView.SetActive(false);
            if (_inputView != null) _inputView.SetActive(true);
            if (_detailView != null) _detailView.SetActive(false);
            if (_memoryListView != null) _memoryListView.SetActive(false);
        }

        private void HideAuxiliaryViews()
        {
            if (_mainView != null) _mainView.SetActive(true);
            if (_inputView != null) _inputView.SetActive(false);
            if (_detailView != null) _detailView.SetActive(false);
            if (_memoryListView != null) _memoryListView.SetActive(false);
        }

        private void RefreshStars()
        {
            // Stars belong to authored slots on the UI tree only. Legacy world-space slots are not used.
            RefreshStarSlots(_starSlots);
            for (int index = 0; index < _worldStarSlots.Length; index++)
                _worldStarSlots[index]?.gameObject.SetActive(false);
        }

        private void RefreshStarSlots(Button[] slots)
        {
            for (int index = 0; index < slots.Length; index++)
            {
                Button? button = slots[index];
                if (button == null) continue;
                WorldMapWishRecord? record = _service?.GetVisibleAtSlot(index);
                button.gameObject.SetActive(record != null);
                // No listener is bound: clicking a star never opens a detail page.
                button.interactable = record != null;
            }
        }

        private void BindMemoryEntryButtons()
        {
            for (int index = 0; index < _memoryEntryButtons.Length; index++)
            {
                int capturedIndex = index;
                _memoryEntryButtons[index]?.onClick.AddListener(() => SelectMemoryEntry(capturedIndex));
            }
        }

        private void UnbindMemoryEntryButtons()
        {
            for (int index = 0; index < _memoryEntryButtons.Length; index++)
                _memoryEntryButtons[index]?.onClick.RemoveAllListeners();
        }

        private void SelectMemoryEntry(int index)
        {
            List<WorldMapWishRecord> history = BuildHistory();
            if (index < 0 || index >= history.Count) return;
            _selectedWishId = history[index].Id;
            RefreshMemoryList();
            RefreshDetail(history[index]);
        }

        private void RefreshMemoryList()
        {
            List<WorldMapWishRecord> history = BuildHistory();
            for (int index = 0; index < _memoryEntryTexts.Length; index++)
            {
                TMP_Text? text = _memoryEntryTexts[index];
                if (text == null) continue;
                bool hasRecord = index < history.Count;
                text.gameObject.SetActive(hasRecord);
                if (index < _memoryEntryButtons.Length && _memoryEntryButtons[index] != null)
                    _memoryEntryButtons[index].gameObject.SetActive(hasRecord);
                if (index < _memoryEntrySelectedVisuals.Length && _memoryEntrySelectedVisuals[index] != null)
                    _memoryEntrySelectedVisuals[index].SetActive(hasRecord && history[index].Id == _selectedWishId);
                text.text = hasRecord ? FormatMemoryEntry(history[index]) : string.Empty;
                text.raycastTarget = false;
            }
        }

        private List<WorldMapWishRecord> BuildHistory()
        {
            var history = new List<WorldMapWishRecord>();
            if (_service != null)
            {
                for (int index = 0; index < _service.Records.Count; index++)
                    history.Add(_service.Records[index]);
            }

            history.Sort((left, right) => right.CreatedAtUtc.CompareTo(left.CreatedAtUtc));
            return history;
        }

        private static WorldMapWishRecord? FindInitialWish(List<WorldMapWishRecord> history)
        {
            DateTime today = DateTime.Now.Date;
            for (int index = 0; index < history.Count; index++)
            {
                WorldMapWishRecord record = history[index];
                if (record.State == WorldMapWishState.Archived) continue;
                if (DateTime.TryParse(record.CreatedAtIso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime created)
                    && created.ToLocalTime().Date == today)
                    return record;
            }

            for (int index = 0; index < history.Count; index++)
            {
                if (history[index].State != WorldMapWishState.Archived) return history[index];
            }

            return null;
        }

        private void ClearDetail()
        {
            SetText(_detailContentText, string.Empty);
            SetText(_detailCreatedText, "-");
            SetText(_detailStateText, "\u72B6\u6001\uFF1A-");
            SetText(_detailFulfilledText, "-");
            if (_fulfillButton != null) _fulfillButton.interactable = false;
        }

        private static string FormatMemoryEntry(WorldMapWishRecord record)
            => $"{FormatDate(record.CreatedAtIso)} [{FormatState(record.State)}]\n{record.Content}";

        private void RefreshDetail(WorldMapWishRecord record)
        {
            SetText(_detailContentText, record.Content);
            SetText(_detailCreatedText, FormatDate(record.CreatedAtIso));
            SetText(_detailStateText, $"\u72B6\u6001\uFF1A{FormatState(record.State)}");
            SetText(_detailFulfilledText, string.IsNullOrWhiteSpace(record.FulfilledAtIso)
                ? "-"
                : FormatDate(record.FulfilledAtIso));
            if (_fulfillButton != null) _fulfillButton.interactable = record.State == WorldMapWishState.Active;
        }

        private int CountVisibleRecords()
        {
            int count = 0;
            if (_service == null) return count;
            for (int index = 0; index < _service.Records.Count; index++)
                if (_service.Records[index].State != WorldMapWishState.Archived) count++;
            return count;
        }

        private static string FormatDate(string iso)
            => DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime date)
                ? date.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                : iso;

        private static string FormatState(WorldMapWishState state)
            => state switch
            {
                WorldMapWishState.Fulfilled => "\u5DF2\u5B9E\u73B0",
                WorldMapWishState.Archived => "\u5DF2\u5F52\u6863",
                _ => "\u8FDB\u884C\u4E2D"
            };

        private static void SetText(TMP_Text? target, string value)
        {
            if (target != null) target.text = value ?? string.Empty;
        }
    }
}
