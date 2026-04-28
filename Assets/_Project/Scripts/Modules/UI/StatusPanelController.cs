#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Modules.UI.ViewModels;
using TMPro;
using UnityEngine;

namespace GeminiLab.Modules.UI
{
    /// <summary>
    /// UI adapter for rendering pet state and work status.
    /// </summary>
    public sealed class StatusPanelController : MonoBehaviour
    {
        [SerializeField] private TMP_Text? _label;

        private PetStatusViewModel? _viewModel;

        public string CurrentStateLabel { get; private set; } = "Unknown";

        public string WorkStateLabel { get; private set; } = "Idle";

        public string WorkDetailLabel { get; private set; } = string.Empty;

        private void Awake()
        {
            EventBus eventBus = ServiceLocator.TryResolve(out EventBus? resolved) && resolved is not null
                ? resolved
                : new EventBus();
            if (!ServiceLocator.TryResolve(out EventBus? _))
            {
                ServiceLocator.Register(eventBus);
            }

            _viewModel = new PetStatusViewModel(eventBus);
            _label ??= GetComponentInChildren<TMP_Text>();
            _viewModel.Changed += RefreshFromViewModel;
            RefreshFromViewModel();
        }

        private void OnDestroy()
        {
            if (_viewModel is not null)
            {
                _viewModel.Changed -= RefreshFromViewModel;
                _viewModel.Dispose();
            }
        }

        private void RefreshFromViewModel()
        {
            if (_viewModel is null)
            {
                return;
            }

            CurrentStateLabel = _viewModel.CurrentState;
            WorkStateLabel = _viewModel.WorkStatus;
            WorkDetailLabel = _viewModel.LastWorkMessage;

            if (_label is not null)
            {
                _label.text =
                    "状态面板\n" +
                    $"State: {_viewModel.CurrentState}\n" +
                    $"Mood: {_viewModel.Mood:0}  Energy: {_viewModel.Energy:0}\n" +
                    $"Satiety: {_viewModel.Satiety:0}  Travel: {(_viewModel.IsTraveling ? "Away" : "Home")}\n" +
                    $"Target: {_viewModel.TargetLabel}\n" +
                    $"Last Interaction: {_viewModel.LastInteractionSummary}\n" +
                    $"Work: {_viewModel.WorkStatus} {_viewModel.LastWorkMessage}".TrimEnd();
            }
        }
    }
}
