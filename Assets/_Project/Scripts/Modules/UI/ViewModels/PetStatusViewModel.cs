#nullable enable
using System;
using GeminiLab.Core.Events;
using GeminiLab.Modules.Furniture;
using GeminiLab.Modules.Pet;

namespace GeminiLab.Modules.UI.ViewModels
{
    /// <summary>
    /// Tracks pet state transitions and work status for status panel rendering.
    /// </summary>
    public sealed class PetStatusViewModel : IDisposable
    {
        private readonly IDisposable _snapshotSub;
        private readonly IDisposable _workStartedSub;
        private readonly IDisposable _workCompletedSub;
        private readonly IDisposable _workFailedSub;

        public PetStatusViewModel(EventBus eventBus)
        {
            _snapshotSub = eventBus.Subscribe<PetRuntimeSnapshotChangedEvent>(OnSnapshotChanged);
            _workStartedSub = eventBus.Subscribe<PetWorkStartedEvent>(OnWorkStarted);
            _workCompletedSub = eventBus.Subscribe<PetWorkCompletedEvent>(OnWorkCompleted);
            _workFailedSub = eventBus.Subscribe<PetWorkFailedEvent>(OnWorkFailed);
        }

        public string CurrentState { get; private set; } = "Unknown";

        public string WorkStatus { get; private set; } = "Idle";

        public string LastWorkMessage { get; private set; } = string.Empty;

        public float Mood { get; private set; }

        public float Energy { get; private set; }

        public float Satiety { get; private set; }

        public string TargetLabel { get; private set; } = "None";

        public string LastInteractionSummary { get; private set; } = "None";

        public bool IsTraveling { get; private set; }

        public event Action? Changed;

        public void Dispose()
        {
            _snapshotSub.Dispose();
            _workStartedSub.Dispose();
            _workCompletedSub.Dispose();
            _workFailedSub.Dispose();
        }

        private void OnSnapshotChanged(PetRuntimeSnapshotChangedEvent payload)
        {
            CurrentState = payload.CurrentState;
            Mood = payload.Mood;
            Energy = payload.Energy;
            Satiety = payload.Satiety;
            IsTraveling = payload.IsTraveling;
            TargetLabel = BuildTargetLabel(payload.TargetFurnitureId, payload.TargetFurnitureCategory, payload.TargetFurnitureInteractionType, payload.WorkRequested);
            if (!string.IsNullOrWhiteSpace(payload.LastInteractionSummary))
            {
                LastInteractionSummary = payload.LastInteractionSummary;
            }

            Changed?.Invoke();
        }

        private void OnWorkStarted(PetWorkStartedEvent payload)
        {
            WorkStatus = "Working";
            LastWorkMessage = $"At {payload.FurnitureId}";
            Changed?.Invoke();
        }

        private void OnWorkCompleted(PetWorkCompletedEvent payload)
        {
            WorkStatus = "Completed";
            LastWorkMessage = payload.Message;
            Changed?.Invoke();
        }

        private void OnWorkFailed(PetWorkFailedEvent payload)
        {
            WorkStatus = "Failed";
            LastWorkMessage = payload.Reason;
            Changed?.Invoke();
        }

        private static string BuildTargetLabel(string furnitureId, FurnitureCategory category, FurnitureInteractionType interactionType, bool workRequested)
        {
            if (string.IsNullOrWhiteSpace(furnitureId))
            {
                return workRequested ? "Searching for work target" : "None";
            }

            string categoryLabel = category == FurnitureCategory.Unknown ? "Furniture" : category.ToString();
            string interactionLabel = interactionType == FurnitureInteractionType.Unknown ? "普通交互" : interactionType.ToDisplayLabel();
            return $"{categoryLabel}/{interactionLabel} ({furnitureId})";
        }
    }
}
