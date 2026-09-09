#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Pet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    public sealed class SpaceSysPanelStub : StubPanelBase
    {
        public override PanelId Id => PanelId.SpaceSys;

        [Header("Angel 数值条")]
        [SerializeField] private Image? _angelMoodFill;
        [SerializeField] private TMP_Text? _angelMoodText;
        [SerializeField] private Image? _angelEnergyFill;
        [SerializeField] private TMP_Text? _angelEnergyText;

        [Header("Devil 数值条")]
        [SerializeField] private Image? _devilMoodFill;
        [SerializeField] private TMP_Text? _devilMoodText;
        [SerializeField] private Image? _devilEnergyFill;
        [SerializeField] private TMP_Text? _devilEnergyText;

        [Header("主控标识")]
        [SerializeField] private SpriteRenderer? _angelControlIndicator;
        [SerializeField] private SpriteRenderer? _devilControlIndicator;

        private IPetRoster? _roster;
        private IDisposable? _snapshotSub;
        private PetId? _lastControlId;

        protected override void OnDestroy()
        {
            _snapshotSub?.Dispose();
            base.OnDestroy();
        }

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);
            EnsureServices();
            SubscribeIfNeeded();
            RefreshAll();
            RefreshControlIndicator(true);
        }

        public override void OnClose()
        {
            if (_angelControlIndicator != null) _angelControlIndicator.gameObject.SetActive(false);
            if (_devilControlIndicator != null) _devilControlIndicator.gameObject.SetActive(false);
            _lastControlId = null;
            base.OnClose();
            _snapshotSub?.Dispose();
            _snapshotSub = null;
        }

        private void EnsureServices()
        {
            if (_roster == null) ServiceLocator.TryResolve(out _roster);
        }

        private void SubscribeIfNeeded()
        {
            if (_snapshotSub != null) return;
            if (ServiceLocator.TryResolve(out EventBus? bus) && bus is not null)
            {
                _snapshotSub = bus.Subscribe<PetRuntimeSnapshotChangedEvent>(_ => RefreshAll());
            }
        }

        private void RefreshAll()
        {
            if (_roster == null) return;

            RefreshPet(_roster.TryGet(PetId.Angel), _angelMoodFill, _angelMoodText, _angelEnergyFill, _angelEnergyText);
            RefreshPet(_roster.TryGet(PetId.Devil), _devilMoodFill, _devilMoodText, _devilEnergyFill, _devilEnergyText);
        }

        private void Update()
        {
            RefreshControlIndicator();
        }

        private void RefreshControlIndicator(bool force = false)
        {
            Transform? activeTransform = PetPlayerInputController.ActiveTransform;
            PetController? controller = activeTransform != null
                ? activeTransform.GetComponent<PetController>()
                : null;
            PetId? activeId = controller != null ? controller.PetId : (PetId?)null;

            if (!force && activeId == _lastControlId)
            {
                return;
            }

            _lastControlId = activeId;

            if (_angelControlIndicator != null)
            {
                _angelControlIndicator.gameObject.SetActive(activeId == PetId.Angel);
            }

            if (_devilControlIndicator != null)
            {
                _devilControlIndicator.gameObject.SetActive(activeId == PetId.Devil);
            }
        }

        private static void RefreshPet(PetRuntimeData? data,
            Image? moodFill, TMP_Text? moodText,
            Image? energyFill, TMP_Text? energyText)
        {
            if (data != null)
            {
                if (moodFill != null) moodFill.fillAmount = data.Mood / 100f;
                if (moodText != null) moodText.text = Mathf.RoundToInt(data.Mood).ToString();
                if (energyFill != null) energyFill.fillAmount = data.Energy / 100f;
                if (energyText != null) energyText.text = Mathf.RoundToInt(data.Energy).ToString();
            }
            else
            {
                if (moodText != null) moodText.text = "--";
                if (energyText != null) energyText.text = "--";
            }
        }
    }
}
