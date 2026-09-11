#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Furniture;
using GeminiLab.Modules.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI
{
    /// <summary>Displays the six authored pages; all sprites, text and layout live in the scene.</summary>
    public sealed class ApartmentTutorialController : MonoBehaviour
    {
        [SerializeField] private GameObject? _panel;
        [SerializeField] private GameObject[] _pages = Array.Empty<GameObject>();
        [SerializeField] private Button? _previous;
        [SerializeField] private Button? _next;
        [SerializeField] private Button? _finish;
        [SerializeField] private Button? _openButton;
        [SerializeField] private TMP_Text? _pageNumber;
        [SerializeField] private BuildModeController? _buildMode;
        private ApartmentTutorialProgress? _progress;
        private IDisposable? _inputLease;
        private bool _initialChecked;
        private int _page;
        public bool IsOpen => _panel != null && _panel.activeSelf;
        public int PageIndex => _page;

        private bool CanOpen => !BuildModeController.IsAnyBuildModeEnabled && (_buildMode == null || !_buildMode.IsBuildModeEnabled) &&
            (!ServiceLocator.TryResolve(out IUIRouter? router) || router == null || router.Top == PanelId.SpaceSys);

        private void Update()
        {
            if (_openButton != null) _openButton.gameObject.SetActive(CanOpen && AutoSaveManager.InitialLoadCompleted);
            if (_progress == null && ServiceLocator.TryResolve(out IPersistentServiceRegistry? registry) && registry != null)
            {
                _progress = ApartmentTutorialProgress.EnsureRegistered();
                _progress.Changed += OnProgressChanged;
            }
            if (IsOpen && !CanOpen) { Close(); return; }
            if (!_initialChecked && _progress != null && AutoSaveManager.InitialLoadCompleted && CanOpen)
            {
                _initialChecked = true;
                if (_progress.ShouldShow) Open();
            }
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Skip();
        }
        private void OnProgressChanged()
        {
            _initialChecked = false;
            if (IsOpen) Close();
        }
        public void Open()
        {
            if (!AutoSaveManager.InitialLoadCompleted || !CanOpen || _panel == null || _pages.Length == 0) return;
            if (_inputLease == null) _inputLease = GameplayInputBlock.Acquire();
            _panel.SetActive(true);
            ShowPage(0);
        }
        public void Next() { if (IsOpen) ShowPage(_page + 1); }
        public void Previous() { if (IsOpen) ShowPage(_page - 1); }
        public void Skip() => Dismiss(false);
        public void Finish() { if (IsOpen && _page == _pages.Length - 1) Dismiss(true); }
        private void Dismiss(bool completed)
        {
            if (!IsOpen) return;
            Close();
            if (_progress != null) { _progress.Dismiss(completed); _ = _progress.SaveAsync(); }
            _initialChecked = true;
        }
        private void ShowPage(int index)
        {
            _page = Mathf.Clamp(index, 0, _pages.Length - 1);
            for (int i = 0; i < _pages.Length; i++) if (_pages[i] != null) _pages[i].SetActive(i == _page);
            if (_previous != null) _previous.interactable = _page > 0;
            if (_next != null) _next.gameObject.SetActive(_page < _pages.Length - 1);
            if (_finish != null) _finish.gameObject.SetActive(_page == _pages.Length - 1);
            if (_pageNumber != null) _pageNumber.text = $"{_page + 1} / {_pages.Length}";
        }
        private void Close()
        {
            if (_panel != null) _panel.SetActive(false);
            _inputLease?.Dispose();
            _inputLease = null;
        }
        private void OnDisable()
        {
            Close();
            if (_progress != null) _progress.Changed -= OnProgressChanged;
            _progress = null;
            _initialChecked = false;
        }
    }
}
