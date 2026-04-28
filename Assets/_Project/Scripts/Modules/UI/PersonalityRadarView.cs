#nullable enable
using GeminiLab.Core;
using GeminiLab.Modules.Pet;
using TMPro;
using UnityEngine;

namespace GeminiLab.Modules.UI
{
    /// <summary>
    /// Lightweight radar data provider for personality/status panel.
    /// </summary>
    public sealed class PersonalityRadarView : MonoBehaviour
    {
        [SerializeField] private TMP_Text? _label;
        [SerializeField] private Vector4 _radarValues;

        public Vector4 RadarValues => _radarValues;

        private void Awake()
        {
            _label ??= GetComponentInChildren<TMP_Text>();
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void Start()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        private System.IDisposable? _subscription;

        private void OnSnapshotChanged(PetRuntimeSnapshotChangedEvent payload)
        {
            _radarValues = new Vector4(
                payload.Mood / 100f,
                payload.Energy / 100f,
                payload.Satiety / 100f,
                payload.WorkRequested ? 1f : 0f);

            if (_label is null)
            {
                return;
            }

            _label.text =
                "状态概览\n" +
                $"Mood: {_radarValues.x:P0}\n" +
                $"Energy: {_radarValues.y:P0}\n" +
                $"Satiety: {_radarValues.z:P0}\n" +
                $"Work Focus: {_radarValues.w:P0}";
        }

        private void TrySubscribe()
        {
            if (_subscription is not null)
            {
                return;
            }

            if (ServiceLocator.TryResolve(out GeminiLab.Core.Events.EventBus? eventBus) && eventBus is not null)
            {
                _subscription = eventBus.Subscribe<PetRuntimeSnapshotChangedEvent>(OnSnapshotChanged);
            }
        }
    }
}
