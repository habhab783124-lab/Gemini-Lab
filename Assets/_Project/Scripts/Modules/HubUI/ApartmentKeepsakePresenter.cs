#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Modules.ApartmentKeepsake;
using GeminiLab.Modules.Persistence;
using GeminiLab.Modules.Pet;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI
{
    /// <summary>
    /// Presents Apartment notes, temporary mementos, and permanent gifts through Scene-authored nodes.
    /// Runtime code only selects existing visual variants, fills text, and controls authored panels.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ApartmentKeepsakePresenter : MonoBehaviour, IApartmentWorldPointInteractable
    {
        [Serializable]
        public struct NoteSpawnBinding
        {
            public GameObject? Root;
            public Collider2D? ClickCollider;
        }

        [Serializable]
        public struct MementoVisualBinding
        {
            public string ItemId;
            public GameObject? Root;
            public Collider2D? ClickCollider;
        }

        [Serializable]
        public struct GiftSlotBinding
        {
            public string ItemId;
            public GameObject? Root;
            public GameObject? OwnedVisual;
            public GameObject? LockedVisual;
            public Button? DetailButton;
            public TMP_Text? TitleText;
        }

        private const float MementoUnlockRelation = 45f;
        private const float GiftUnlockRelation = 80f;

        [Header("Scene 中的纸条落点（顺序必须与 SpawnIndex 对应）")]
        [SerializeField] private NoteSpawnBinding[] _noteSpawns = Array.Empty<NoteSpawnBinding>();

        [Header("Scene 中预先绑定 Sprite 的临时遗留物变体")]
        [SerializeField] private MementoVisualBinding[] _mementoVisuals = Array.Empty<MementoVisualBinding>();

        [Header("纸条 / 遗留物详情弹窗")]
        [SerializeField] private GameObject? _detailPopupRoot;
        [SerializeField] private TMP_Text? _detailTitleText;
        [SerializeField] private TMP_Text? _detailBodyText;
        [SerializeField] private GameObject? _angelWoodSign;
        [SerializeField] private GameObject? _devilWoodSign;
        [SerializeField] private Button? _detailCloseButton;

        [Header("永久赠礼收藏")]
        [SerializeField] private GameObject? _giftCollectionButtonRoot;
        [SerializeField] private Button? _giftCollectionButton;
        [SerializeField] private GameObject? _giftCollectionPanelRoot;
        [SerializeField] private Button? _giftCollectionCloseButton;
        [SerializeField] private GiftSlotBinding[] _giftSlots = Array.Empty<GiftSlotBinding>();

        private IApartmentKeepsakeService? _service;
        private IPetRoster? _petRoster;
        private EventBus? _eventBus;
        private IDisposable? _keepsakeChangedSubscription;
        private IDisposable? _petSnapshotSubscription;
        private IDisposable? _initialLoadSubscription;
        private UnityAction[] _giftSlotActions = Array.Empty<UnityAction>();

        private bool _beginRequested;
        private bool _initialized;
        private bool _hasAngelRelation;
        private bool _hasDevilRelation;
        private float _angelRelation;
        private float _devilRelation;

        private void Awake()
        {
            WireButtons();
            ResolveRuntimeDependencies();
            EnsureEventSubscriptions();
            HideAllWorldVisuals();
            CloseDetailPopup();
            CloseGiftCollection();
        }

        private void Start()
        {
            ResolveRuntimeDependencies();
            EnsureEventSubscriptions();

            if (AutoSaveManager.InitialLoadCompleted)
            {
                BeginAfterPersistenceLoad();
            }
        }

        private void Update()
        {
            if (!_initialized && _beginRequested)
            {
                TryBeginAfterPersistenceLoad();
            }
        }

        private void OnDestroy()
        {
            _keepsakeChangedSubscription?.Dispose();
            _petSnapshotSubscription?.Dispose();
            _initialLoadSubscription?.Dispose();
            UnwireButtons();
        }

        /// <summary>
        /// Starts the once-per-indoor-entry evaluation after every persistent service has been restored.
        /// This method is idempotent and can also be invoked by a Scene bootstrap in custom startup flows.
        /// </summary>
        public void BeginAfterPersistenceLoad()
        {
            if (_initialized)
            {
                RefreshPresentation();
                return;
            }

            _beginRequested = true;
            TryBeginAfterPersistenceLoad();
        }

        public bool TryHandleWorldPoint(Vector2 worldPoint)
        {
            if (!_initialized || _service == null ||
                !ClickOcclusionUtility.TryGetTopmostColliderAtWorldPoint(worldPoint, out Collider2D? topmostCollider) ||
                topmostCollider == null)
            {
                return false;
            }

            ApartmentNoteState note = _service.CurrentNote;
            int noteIndex = GetActiveNoteSpawnIndex(note);
            if (noteIndex >= 0 &&
                ReferenceEquals(_noteSpawns[noteIndex].ClickCollider, topmostCollider))
            {
                ShowNoteDetail(note);
                return true;
            }

            ApartmentMementoState memento = _service.CurrentMemento;
            if (!memento.IsPresent)
            {
                return false;
            }

            for (int i = 0; i < _mementoVisuals.Length; i++)
            {
                MementoVisualBinding binding = _mementoVisuals[i];
                if (string.Equals(binding.ItemId, memento.ItemId, StringComparison.Ordinal) &&
                    ReferenceEquals(binding.ClickCollider, topmostCollider))
                {
                    ShowItemDetail(memento.ItemId, memento.Owner);
                    return true;
                }
            }

            return false;
        }

        private void TryBeginAfterPersistenceLoad()
        {
            ResolveRuntimeDependencies();
            EnsureEventSubscriptions();

            if (_service == null || !TryCaptureCurrentRelations(out float angelRelation, out float devilRelation))
            {
                return;
            }

            _initialized = true;
            _beginRequested = false;
            _service.ProcessFirstIndoorEntry(angelRelation, devilRelation);
            RefreshPresentation();
        }

        private void ResolveRuntimeDependencies()
        {
            if (_service == null)
            {
                ServiceLocator.TryResolve(out _service);
            }

            if (_petRoster == null)
            {
                ServiceLocator.TryResolve(out _petRoster);
            }

            if (_eventBus == null)
            {
                ServiceLocator.TryResolve(out _eventBus);
            }
        }

        private void EnsureEventSubscriptions()
        {
            if (_eventBus == null)
            {
                return;
            }

            _keepsakeChangedSubscription ??=
                _eventBus.Subscribe<ApartmentKeepsakeStateChangedEvent>(OnKeepsakeStateChanged);
            _petSnapshotSubscription ??=
                _eventBus.Subscribe<PetRuntimeSnapshotChangedEvent>(OnPetRuntimeSnapshotChanged);
            _initialLoadSubscription ??=
                _eventBus.Subscribe<AutoSaveInitialLoadFinishedEvent>(_ => BeginAfterPersistenceLoad());
        }

        private bool TryCaptureCurrentRelations(out float angelRelation, out float devilRelation)
        {
            if (_petRoster != null)
            {
                PetRuntimeData? angel = _petRoster.TryGet(PetId.Angel);
                if (angel != null)
                {
                    _angelRelation = angel.Relation;
                    _hasAngelRelation = true;
                }

                PetRuntimeData? devil = _petRoster.TryGet(PetId.Devil);
                if (devil != null)
                {
                    _devilRelation = devil.Relation;
                    _hasDevilRelation = true;
                }
            }

            angelRelation = _angelRelation;
            devilRelation = _devilRelation;
            return _hasAngelRelation && _hasDevilRelation;
        }

        private void OnKeepsakeStateChanged(ApartmentKeepsakeStateChangedEvent _)
        {
            if (_initialized)
            {
                RefreshPresentation();
            }
        }

        private void OnPetRuntimeSnapshotChanged(PetRuntimeSnapshotChangedEvent snapshot)
        {
            bool hadPrevious;
            float previousRelation;

            if (snapshot.PetId == PetId.Devil)
            {
                hadPrevious = _hasDevilRelation;
                previousRelation = _devilRelation;
                _devilRelation = snapshot.Relation;
                _hasDevilRelation = true;
            }
            else
            {
                hadPrevious = _hasAngelRelation;
                previousRelation = _angelRelation;
                _angelRelation = snapshot.Relation;
                _hasAngelRelation = true;
            }

            if (!_initialized || !hadPrevious ||
                !CrossedUnlockThreshold(previousRelation, snapshot.Relation))
            {
                return;
            }

            ResolveRuntimeDependencies();
            if (_service != null &&
                _service.ProcessRelationThreshold(ToKeepsakeOwner(snapshot.PetId), previousRelation, snapshot.Relation))
            {
                RefreshPresentation();
            }
        }

        private static bool CrossedUnlockThreshold(float previousRelation, float currentRelation)
        {
            if (currentRelation <= previousRelation)
            {
                return false;
            }

            return previousRelation < MementoUnlockRelation && currentRelation >= MementoUnlockRelation ||
                   previousRelation < GiftUnlockRelation && currentRelation >= GiftUnlockRelation;
        }

        private void RefreshPresentation()
        {
            ResolveRuntimeDependencies();
            if (!_initialized || _service == null)
            {
                HideAllWorldVisuals();
                SetGiftCollectionAvailability(false);
                return;
            }

            RefreshNoteVisual(_service.CurrentNote);
            RefreshMementoVisual(_service.CurrentMemento);
            RefreshGiftCollection();
        }

        private void RefreshNoteVisual(ApartmentNoteState note)
        {
            int activeIndex = GetActiveNoteSpawnIndex(note);
            for (int i = 0; i < _noteSpawns.Length; i++)
            {
                SetBindingActive(_noteSpawns[i].Root, _noteSpawns[i].ClickCollider, i == activeIndex);
            }
        }

        private int GetActiveNoteSpawnIndex(ApartmentNoteState note)
        {
            if (!note.IsPresent || _noteSpawns.Length == 0)
            {
                return -1;
            }

            return (note.SpawnIndex & int.MaxValue) % _noteSpawns.Length;
        }

        private void RefreshMementoVisual(ApartmentMementoState memento)
        {
            bool activated = false;
            for (int i = 0; i < _mementoVisuals.Length; i++)
            {
                MementoVisualBinding binding = _mementoVisuals[i];
                bool shouldActivate = memento.IsPresent && !activated &&
                    string.Equals(binding.ItemId, memento.ItemId, StringComparison.Ordinal);
                SetBindingActive(binding.Root, binding.ClickCollider, shouldActivate);
                activated |= shouldActivate;
            }
        }

        private void RefreshGiftCollection()
        {
            if (_service == null)
            {
                SetGiftCollectionAvailability(false);
                return;
            }

            bool hasOwnedGift = _service.OwnedGifts.Count > 0;
            SetGiftCollectionAvailability(hasOwnedGift);

            for (int i = 0; i < _giftSlots.Length; i++)
            {
                GiftSlotBinding slot = _giftSlots[i];
                bool configured = !string.IsNullOrWhiteSpace(slot.ItemId);
                bool owned = configured && _service.IsGiftOwned(slot.ItemId);

                if (slot.Root != null)
                {
                    slot.Root.SetActive(configured);
                }

                if (slot.OwnedVisual != null)
                {
                    slot.OwnedVisual.SetActive(owned);
                }

                if (slot.LockedVisual != null)
                {
                    slot.LockedVisual.SetActive(configured && !owned);
                }

                if (slot.DetailButton != null)
                {
                    slot.DetailButton.interactable = owned;
                }

                if (slot.TitleText != null)
                {
                    slot.TitleText.text = owned &&
                        ApartmentKeepsakeCatalog.TryGet(slot.ItemId, out ApartmentKeepsakeItemDefinition definition)
                            ? definition.Title
                            : "未解锁";
                }
            }
        }

        private void SetGiftCollectionAvailability(bool available)
        {
            if (_giftCollectionButtonRoot != null)
            {
                _giftCollectionButtonRoot.SetActive(available);
            }

            if (_giftCollectionButton != null)
            {
                _giftCollectionButton.interactable = available;
            }

            if (!available)
            {
                CloseGiftCollection();
            }
        }

        private void HideAllWorldVisuals()
        {
            for (int i = 0; i < _noteSpawns.Length; i++)
            {
                SetBindingActive(_noteSpawns[i].Root, _noteSpawns[i].ClickCollider, false);
            }

            for (int i = 0; i < _mementoVisuals.Length; i++)
            {
                SetBindingActive(_mementoVisuals[i].Root, _mementoVisuals[i].ClickCollider, false);
            }
        }

        private static void SetBindingActive(GameObject? root, Collider2D? clickCollider, bool active)
        {
            if (root != null)
            {
                root.SetActive(active);
                return;
            }

            if (clickCollider != null)
            {
                clickCollider.gameObject.SetActive(active);
            }
        }

        private void ShowNoteDetail(ApartmentNoteState note)
        {
            string sender = GetPetDisplayName(note.Sender);
            string recipient = GetPetDisplayName(note.Recipient);
            SetText(_detailTitleText, $"{sender}留给{recipient}的纸条");
            SetText(_detailBodyText, string.IsNullOrWhiteSpace(note.Content) ? "纸条上没有留下文字。" : note.Content);
            SetWoodSign(note.Sender);
            _detailPopupRoot?.SetActive(true);
        }

        private void ShowItemDetail(string itemId, ApartmentKeepsakeOwner fallbackOwner)
        {
            if (ApartmentKeepsakeCatalog.TryGet(itemId, out ApartmentKeepsakeItemDefinition definition))
            {
                SetText(_detailTitleText, definition.Title);
                SetText(_detailBodyText, definition.Description);
                SetWoodSign(definition.Owner);
            }
            else
            {
                SetText(_detailTitleText, "遗留物");
                SetText(_detailBodyText, "暂时没有可显示的说明。");
                SetWoodSign(fallbackOwner);
            }

            _detailPopupRoot?.SetActive(true);
        }

        private void SetWoodSign(ApartmentKeepsakeOwner owner)
        {
            _angelWoodSign?.SetActive(owner == ApartmentKeepsakeOwner.Angel);
            _devilWoodSign?.SetActive(owner == ApartmentKeepsakeOwner.Devil);
        }

        private static string GetPetDisplayName(ApartmentKeepsakeOwner id)
        {
            return id == ApartmentKeepsakeOwner.Devil ? "恶魔" : "天使";
        }

        private static void SetText(TMP_Text? target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private void OpenGiftCollection()
        {
            if (!_initialized || _service == null || _service.OwnedGifts.Count == 0)
            {
                return;
            }

            RefreshGiftCollection();
            _giftCollectionPanelRoot?.SetActive(true);
        }

        private void CloseGiftCollection()
        {
            _giftCollectionPanelRoot?.SetActive(false);
        }

        private void CloseDetailPopup()
        {
            _detailPopupRoot?.SetActive(false);
        }

        private void OpenGiftSlot(int index)
        {
            if (!_initialized || _service == null || index < 0 || index >= _giftSlots.Length)
            {
                return;
            }

            GiftSlotBinding slot = _giftSlots[index];
            if (string.IsNullOrWhiteSpace(slot.ItemId) || !_service.IsGiftOwned(slot.ItemId))
            {
                return;
            }

            ApartmentKeepsakeOwner owner = ApartmentKeepsakeOwner.Angel;
            if (ApartmentKeepsakeCatalog.TryGet(slot.ItemId, out ApartmentKeepsakeItemDefinition definition))
            {
                owner = definition.Owner;
            }

            ShowItemDetail(slot.ItemId, owner);
        }

        private static ApartmentKeepsakeOwner ToKeepsakeOwner(PetId petId)
        {
            return petId == PetId.Devil
                ? ApartmentKeepsakeOwner.Devil
                : ApartmentKeepsakeOwner.Angel;
        }

        private void WireButtons()
        {
            _detailCloseButton?.onClick.AddListener(CloseDetailPopup);
            _giftCollectionButton?.onClick.AddListener(OpenGiftCollection);
            _giftCollectionCloseButton?.onClick.AddListener(CloseGiftCollection);

            _giftSlotActions = new UnityAction[_giftSlots.Length];
            for (int i = 0; i < _giftSlots.Length; i++)
            {
                int capturedIndex = i;
                UnityAction action = () => OpenGiftSlot(capturedIndex);
                _giftSlotActions[i] = action;
                _giftSlots[i].DetailButton?.onClick.AddListener(action);
            }
        }

        private void UnwireButtons()
        {
            _detailCloseButton?.onClick.RemoveListener(CloseDetailPopup);
            _giftCollectionButton?.onClick.RemoveListener(OpenGiftCollection);
            _giftCollectionCloseButton?.onClick.RemoveListener(CloseGiftCollection);

            int count = Mathf.Min(_giftSlots.Length, _giftSlotActions.Length);
            for (int i = 0; i < count; i++)
            {
                UnityAction action = _giftSlotActions[i];
                if (action != null)
                {
                    _giftSlots[i].DetailButton?.onClick.RemoveListener(action);
                }
            }
        }
    }
}
