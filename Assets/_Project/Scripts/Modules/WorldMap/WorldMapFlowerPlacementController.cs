#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Modules.EmotionGarden;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.Persistence;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// WorldMap 花朵自由摆放入口。
    /// 所有 UI、花卉 Sprite、网格线、预览和落位对象都由 Scene / Inspector 作者化；
    /// 运行时只负责读取数据、切换已有节点、更新文本和移动预置落位槽。
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class WorldMapFlowerPlacementController : MonoBehaviour
    {
        private const string AutoSaveSlot = "autosave";
        private const int SharedSortingBase = 1000;
        private const int SortingOrderStride = 1000;
        private const int BaselineYPrecision = 100;

        public enum PlacementVisualType
        {
            Single,
            Cluster
        }

        [Serializable]
        public sealed class FlowerOption
        {
            [SerializeField] private string _id = string.Empty;
            [SerializeField] private string _displayName = string.Empty;
            [SerializeField] private int _initialSingleCount;
            [SerializeField] private int _initialClusterCount;
            [SerializeField] private Vector2Int _singleFootprint = Vector2Int.one;
            [SerializeField] private Vector2Int _clusterFootprint = new(2, 2);

            [Header("Scene-authored entry view")]
            [SerializeField] private LayoutElement? _entryLayout;
            [SerializeField] private float _collapsedHeight = 73f;
            [SerializeField] private float _expandedHeight = 320f;
            [SerializeField] private Button? _headerButton;
            [SerializeField] private Component? _displayNameText;
            [SerializeField] private GameObject? _expandedRoot;
            [SerializeField] private GameObject? _arrowUp;
            [SerializeField] private GameObject? _arrowDown;
            [SerializeField] private Button? _singleButton;
            [SerializeField] private Button? _clusterButton;
            [SerializeField] private Button? _synthesisButton;
            [SerializeField] private Component? _singleCountText;
            [SerializeField] private Component? _clusterCountText;
            [SerializeField] private GameObject? _singleSelectedMark;
            [SerializeField] private GameObject? _clusterSelectedMark;

            [NonSerialized] private int _singleCount;
            [NonSerialized] private int _clusterCount;

            public string Id => _id;
            public string DisplayName => _displayName;
            public Vector2Int SingleFootprint => _singleFootprint;
            public Vector2Int ClusterFootprint => _clusterFootprint;
            public int SingleCount => _singleCount;
            public int ClusterCount => _clusterCount;
            public Button? HeaderButton => _headerButton;
            public RectTransform? HeaderRectTransform => _headerButton != null
                ? _headerButton.transform as RectTransform
                : null;
            public GameObject? ExpandedRoot => _expandedRoot;
            public GameObject? ArrowUp => _arrowUp;
            public GameObject? ArrowDown => _arrowDown;
            public Button? SingleButton => _singleButton;
            public Button? ClusterButton => _clusterButton;
            public Button? SynthesisButton => _synthesisButton;

            public void InitializeCounts(IEmotionGardenService? gardenService)
            {
                _singleCount = Mathf.Max(0, _initialSingleCount);
                _clusterCount = Mathf.Max(0, _initialClusterCount);
                SyncCounts(gardenService);
            }

            public void SyncCounts(IEmotionGardenService? gardenService)
            {
                if (gardenService == null || !TryGetInventoryKey(out string emotionType, out string owner))
                    return;

                PlacementFlowerInventory inventory = gardenService.GetPlacementInventory(emotionType, owner);
                _singleCount = Mathf.Max(0, inventory.SingleCount);
                _clusterCount = Mathf.Max(0, inventory.ClusterCount);
            }

            public bool TryGetInventoryKey(out string emotionType, out string owner)
            {
                int separatorIndex = _id.IndexOf('|');
                if (separatorIndex <= 0 || separatorIndex >= _id.Length - 1)
                {
                    emotionType = string.Empty;
                    owner = string.Empty;
                    return false;
                }

                owner = EmotionFlowerCatalog.NormalizeOwner(_id.Substring(0, separatorIndex));
                emotionType = EmotionFlowerCatalog.NormalizeEmotionType(_id.Substring(separatorIndex + 1));
                return true;
            }

            public void RefreshView(int selectedIndex, int index)
            {
                bool expanded = selectedIndex == index;
                if (_entryLayout != null)
                {
                    float height = expanded ? _expandedHeight : _collapsedHeight;
                    _entryLayout.minHeight = height;
                    _entryLayout.preferredHeight = height;
                    _entryLayout.flexibleHeight = 0f;
                }
                if (_expandedRoot != null) _expandedRoot.SetActive(expanded);
                if (_arrowUp != null) _arrowUp.SetActive(expanded);
                if (_arrowDown != null) _arrowDown.SetActive(!expanded);
                SetText(_displayNameText, _displayName);
                SetText(_singleCountText, "×" + _singleCount);
                SetText(_clusterCountText, "×" + _clusterCount);
            }

            public void SetSelection(PlacementVisualType? selectedType)
            {
                if (_singleSelectedMark != null)
                    _singleSelectedMark.SetActive(selectedType == PlacementVisualType.Single);
                if (_clusterSelectedMark != null)
                    _clusterSelectedMark.SetActive(selectedType == PlacementVisualType.Cluster);
            }
        }

        [Header("Scene / Inspector 引用")]
        [SerializeField] private Button? _openButton;
        [SerializeField] private Button? _closeButton;
        [SerializeField] private GameObject? _sidebarPanel;
        [SerializeField] private ScrollRect? _sidebarScrollRect;
        [SerializeField] private RectTransform? _flowerListLayoutRoot;
        [SerializeField] private GameObject? _placementGrid;
        [SerializeField] private GameObject? _previewRoot;
        [SerializeField] private Transform? _placementRoot;
        [SerializeField] private Collider2D? _placementSurface;
        [SerializeField] private List<WorldMapFlowerPlacementRegion> _placementRegions = new();
        [SerializeField] private Component? _statusText;
        [SerializeField] private GameObject? _hintBubble;
        [SerializeField] private List<WorldMapPlacementSlot> _placementSlots = new();

        [Header("摆放参数")]
        [SerializeField] private Vector2 _cellSize = Vector2.one;
        [SerializeField] private Vector2 _clusterCellSize = new(3.99f, 2.22f);
        [SerializeField] private float _baselineSnapTolerance = 0.05f;
        [SerializeField] private List<PlacementLayer> _placementLayers = new();
        [SerializeField] private int _flowerSortingOrderOffset;
        [SerializeField] private string _flowerSortingLayerName = "Default";
        [SerializeField] private bool _useSurfaceBoundsAsGridOrigin = true;
        [SerializeField] private Vector2 _gridOrigin;
        [SerializeField] private Color _validPreviewColor = Color.white;
        [SerializeField] private Color _invalidPreviewColor = new(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private float _previewAlpha = 0.5f;
        [SerializeField, Min(0.01f)] private float _previewSmoothTime = 0.1f;
        [SerializeField, Min(0.01f)] private float _previewColorSmoothTime = 0.08f;
        [SerializeField, Min(0f)] private float _layerSwitchHysteresis = 0.12f;
        [SerializeField] private List<PlacementVisualBinding> _previewVisuals = new();
        [SerializeField] private List<FlowerOption> _options = new();

        private Camera? _camera;
        private IEmotionGardenService? _gardenService;
        private IDisposable? _inventoryChangedSubscription;
        private IDisposable? _gardenClearedSubscription;
        private IDisposable? _placementsChangedSubscription;
        private FlowerOption? _selectedOption;
        private PlacementVisualType? _selectedVisualType;
        private int _expandedIndex = -1;
        private Vector2Int _selectedFootprint = Vector2Int.one;
        private bool _isSelecting;
        private bool _saveInProgress;
        private bool _saveQueued;
        private bool _commitInProgress;
        private Vector2 _previewVelocity;
        private bool _previewPositionInitialized;
        private Color _previewTint;
        private bool _previewTintInitialized;
        private PlacementLayer? _previewLayer;

        [Serializable]
        public sealed class PlacementLayer
        {
            [SerializeField] private string _id = string.Empty;
            [SerializeField] private float _baselineY;
            [SerializeField] private int _sortingOrder;
            [SerializeField] private float _xMin;
            [SerializeField] private float _xMax;
            [SerializeField] private float _xOffset;

            public string Id => _id;
            public float BaselineY => _baselineY;
            public int SortingOrder => _sortingOrder;
            public float XMin => _xMin;
            public float XMax => _xMax;
            public float XOffset => _xOffset;
        }

        public Vector2 CellSize => _cellSize;
        public bool IsSelecting => _isSelecting;
        public bool UsesAuthoredPlacementRegions => HasPlacementRegionBindings;

        private void Awake()
        {
            _camera = Camera.main;
            ResolveSharedInventoryDependencies();
            ResolvePlacementRegionsFallback();
            ResolvePlacementSurfaceFallback();
            ValidateSceneBindings();
            ValidatePlacementRegionBindings();

            _cellSize = new Vector2(Mathf.Max(0.05f, _cellSize.x), Mathf.Max(0.05f, _cellSize.y));
            for (int i = 0; i < _options.Count; i++)
            {
                FlowerOption option = _options[i];
                option.InitializeCounts(_gardenService);
                BindOption(option, i);
                option.RefreshView(_expandedIndex, i);
                option.SetSelection(null);
            }

            BindButtonsOnce();
            RestorePlacedFlowersFromSharedService();
            SetHintVisible(false);
            SetPlacementVisuals(false, null);
            SetPlacementMode(false);
            SetSidebarVisible(false);
        }

        private void Start()
        {
            // RuntimeInitializeOnLoadMethod 与场景组件 Awake 的先后顺序可能因启动入口不同而变化。
            // Start 再解析一次，确保自动读档前已经订阅摆放变更事件并能恢复槽位。
            ResolveSharedInventoryDependencies();
            RestorePlacedFlowersFromSharedService();
        }

        private void LateUpdate()
        {
            ApplyWorldMapPetSorting();
        }

        private void ApplyWorldMapPetSorting()
        {
            if (_placementLayers.Count == 0) return;

            PetController[] pets = FindObjectsByType<PetController>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < pets.Length; i++)
            {
                PetController pet = pets[i];
                if (pet == null || pet.gameObject.scene.name != "WorldMap_Main") continue;

                BaselineItem? baseline = pet.GetComponent<BaselineItem>();
                if (baseline == null) continue;

                // Read the pet's authored BaselineItem directly. Same exact line: keep the pet slightly in front.
                pet.ApplyWorldMapSortingOrder(
                    ResolvePetSortingOrder(baseline.SortingOrder, baseline.EffectiveBaselineY),
                    _flowerSortingLayerName);
            }
        }

        private void ValidatePlacementRegionBindings()
        {
            if (!HasPlacementRegionBindings)
            {
                Debug.LogWarning("[WorldMapFlowerPlacement] Legacy placement fallback active: no authored placement regions are configured; FlowerPlacementBounds is used.");
                return;
            }

            int validRegionCount = 0;
            for (int i = 0; i < _placementRegions.Count; i++)
            {
                WorldMapFlowerPlacementRegion? region = _placementRegions[i];
                if (region != null && region.BoundsCollider != null)
                    validRegionCount++;
            }

            bool angelReady = ResolvePlacementRegion(EmotionFlowerCatalog.OwnerAngel) != null;
            bool demonReady = ResolvePlacementRegion(EmotionFlowerCatalog.OwnerDemon) != null;
            if (validRegionCount != _placementRegions.Count || !angelReady || !demonReady)
            {
                Debug.LogError($"[WorldMapFlowerPlacement] Placement region configuration invalid: {validRegionCount}/{_placementRegions.Count} references are valid, angel={angelReady}, demon={demonReady}. FlowerPlacementBounds will not be used as a fallback.");
                return;
            }

            Debug.Log($"[WorldMapFlowerPlacement] Authored placement regions active: {_placementRegions.Count} regions. FlowerPlacementBounds is fallback-only.");
        }

        private void ValidateSceneBindings()
        {
            // PlacementSlot 引用已经由 Scene 作者化并序列化到控制器上。
            // 运行时再根据层级扫描重建列表会在场景尚未完成子节点枚举时覆盖有效引用，
            // 进而让合法点击永远找不到可用槽位，因此始终保留序列化列表作为唯一来源。
            // Runtime binding diagnostics intentionally remain in this path for Play verification.
            int boundSlotCount = 0;
            int boundVisualCount = 0;
            for (int i = 0; i < _placementSlots.Count; i++)
            {
                WorldMapPlacementSlot? slot = _placementSlots[i];
                if (slot == null)
                {
                    if (i == 0)
                        Debug.LogWarning("[WorldMapFlowerPlacement] Slot diagnostic: serialized slot 0 is null after scene deserialization.");
                    continue;
                }
                slot.EnsureRuntimeBindings(GetFlowerIds());
                if (i == 0)
                    Debug.Log($"[WorldMapFlowerPlacement] Slot diagnostic: name={slot.name}, visualBindings={slot.VisualBindingCount}, hasBindings={slot.HasVisualBindings}, active={slot.gameObject.activeSelf}.");
                if (!slot.HasVisualBindings) continue;
                boundSlotCount++;
                boundVisualCount += slot.VisualBindingCount;
            }

            if (boundSlotCount != _placementSlots.Count || boundVisualCount == 0)
            {
                Debug.LogError($"[WorldMapFlowerPlacement] Scene placement bindings incomplete: {boundSlotCount}/{_placementSlots.Count} slots, {boundVisualCount} visual bindings. Run Author Flower Placement.");
            }
            else
            {
                Debug.Log($"[WorldMapFlowerPlacement] Scene placement bindings ready: {boundSlotCount}/{_placementSlots.Count} slots, {boundVisualCount} visual bindings.");
            }
        }

        private void Update()
        {
            if (_isSelecting && Input.GetKeyDown(KeyCode.Escape))
            {
                CancelSelection();
                return;
            }

            if (!_isSelecting || _camera == null || _previewRoot == null || _selectedOption == null ||
                !_selectedVisualType.HasValue)
            {
                return;
            }

            Vector2 worldPoint = _camera.ScreenToWorldPoint(Input.mousePosition);
            PlacementLayer previewLayer = ResolvePreviewPlacementLayer(worldPoint.y);
            Vector2 snapped = SnapToGrid(worldPoint, _selectedFootprint, previewLayer);
            if (!_previewPositionInitialized)
            {
                _previewRoot.transform.position = new Vector3(snapped.x, snapped.y, _previewRoot.transform.position.z);
                _previewVelocity = Vector2.zero;
                _previewPositionInitialized = true;
            }
            else
            {
                Vector2 current = _previewRoot.transform.position;
                Vector2 smoothed = Vector2.SmoothDamp(current, snapped, ref _previewVelocity,
                    Mathf.Max(0.01f, _previewSmoothTime), Mathf.Infinity, Time.unscaledDeltaTime);
                _previewRoot.transform.position = new Vector3(smoothed.x, smoothed.y, _previewRoot.transform.position.z);
            }
            SetPreviewSortingOrder(previewLayer.SortingOrder, previewLayer.BaselineY);

            bool valid = IsValidPlacement(snapped, _selectedFootprint);
            SmoothPreviewTint(valid ? _validPreviewColor : _invalidPreviewColor);
            if (Input.GetMouseButtonDown(0))
            {
                bool overPlacementUi = IsPointerOverPlacementUi();
                Debug.Log($"[WorldMapFlowerPlacement] 点击吸附点：world={snapped}，layer={ResolvePlacementLayer(snapped.y).Id}，valid={valid}，overPlacementUi={overPlacementUi}");
                if (valid && !overPlacementUi)
                    CommitPlacement(snapped);
            }
        }

        private void OnDestroy()
        {
            _openButton?.onClick.RemoveListener(OpenSidebar);
            _closeButton?.onClick.RemoveListener(CloseSidebar);
            _inventoryChangedSubscription?.Dispose();
            _gardenClearedSubscription?.Dispose();
            _placementsChangedSubscription?.Dispose();
            _inventoryChangedSubscription = null;
            _gardenClearedSubscription = null;
            _placementsChangedSubscription = null;
        }

        private void BindButtonsOnce()
        {
            _openButton?.onClick.AddListener(OpenSidebar);
            _closeButton?.onClick.AddListener(CloseSidebar);
        }

        private void BindOption(FlowerOption option, int index)
        {
            option.HeaderButton?.onClick.AddListener(() => ToggleFlower(index));
            option.SingleButton?.onClick.AddListener(() => SelectVisualType(index, PlacementVisualType.Single));
            option.ClusterButton?.onClick.AddListener(() => SelectVisualType(index, PlacementVisualType.Cluster));
            option.SynthesisButton?.onClick.AddListener(() => Synthesize(index));
        }

        private void OpenSidebar()
        {
            ResolveSharedInventoryDependencies();
            RefreshInventoryFromSharedService();
            SetSidebarVisible(true);
            SetHintVisible(false);
            RefreshOptionViews();
        }

        private void CloseSidebar()
        {
            CancelSelection();
            SetSidebarVisible(false);
        }

        private void ToggleFlower(int index)
        {
            if (index < 0 || index >= _options.Count) return;
            _expandedIndex = _expandedIndex == index ? -1 : index;
            RefreshOptionViews(_options[index].HeaderRectTransform);
        }

        private void SelectVisualType(int index, PlacementVisualType visualType)
        {
            if (index < 0 || index >= _options.Count) return;

            ResolveSharedInventoryDependencies();
            FlowerOption option = _options[index];
            option.SyncCounts(_gardenService);
            int count = visualType == PlacementVisualType.Single ? option.SingleCount : option.ClusterCount;
            if (count <= 0)
            {
                SetHintVisible(true);
                SetStatus("需要先获得对应的花卉库存。");
                return;
            }

            _selectedOption = option;
            _selectedVisualType = visualType;
            _selectedFootprint = visualType == PlacementVisualType.Single
                ? option.SingleFootprint
                : Vector2Int.one;
            _previewLayer = null;
            _previewPositionInitialized = false;
            _previewTintInitialized = false;
            _previewVelocity = Vector2.zero;
            _expandedIndex = index;
            RefreshOptionViews();
            SetHintVisible(false);
            SetPlacementMode(true);
            SetPlacementVisuals(true, option.Id + "|" + visualType);
            // 需求流程要求选定单花/花丛后底部库存栏自动收起，
            // 但当前选择和预览必须保留，玩家才能直接点击场景落位。
            SetSidebarVisible(false);
            SetStatus("已选择 " + option.DisplayName + "·" +
                      (visualType == PlacementVisualType.Single ? "单花" : "花丛") +
                      "，点击草地区域摆放，按 Esc 退出");
        }

        private void Synthesize(int index)
        {
            if (index < 0 || index >= _options.Count) return;

            ResolveSharedInventoryDependencies();
            FlowerOption option = _options[index];
            option.SyncCounts(_gardenService);
            if (option.SingleCount < 3)
            {
                SetHintVisible(true);
                SetStatus("3 朵同种单花可以合成一个花丛哦！");
                return;
            }

            if (_gardenService == null ||
                !option.TryGetInventoryKey(out string emotionType, out string owner))
            {
                SetHintVisible(true);
                SetStatus("花卉库存尚未加载，请稍后重试。");
                return;
            }

            if (!_gardenService.TrySynthesizePlacementCluster(emotionType, owner))
            {
                RefreshInventoryFromSharedService();
                SetHintVisible(true);
                SetStatus("3 朵同种单花可以合成一个花丛哦！");
                return;
            }

            option.SyncCounts(_gardenService);
            _expandedIndex = index;
            RefreshOptionViews();
            SetHintVisible(false);
            SetStatus(option.DisplayName + " 已合成 1 个花丛");
        }

        private void CommitPlacement(Vector2 position)
        {
            if (_selectedOption == null || !_selectedVisualType.HasValue) return;

            if (!IsValidPlacement(position, _selectedFootprint))
            {
                SetHintVisible(true);
                SetStatus("当前位置不属于该培育者的花园区域。 ");
                return;
            }

            ResolveSharedInventoryDependencies();

            WorldMapPlacementSlot? slot = FindFreeSlot();
            if (slot == null)
            {
                int occupiedCount = 0;
                for (int i = 0; i < _placementSlots.Count; i++)
                {
                    if (_placementSlots[i] != null && _placementSlots[i].IsOccupied)
                        occupiedCount++;
                }
                Debug.LogWarning($"[WorldMapFlowerPlacement] No free placement slot: serializedSlots={_placementSlots.Count}, occupied={occupiedCount}.");
                SetStatus("摆放槽位已满，请先调整场景中的花卉。");
                return;
            }

            PlacementVisualType visualType = _selectedVisualType.Value;
            int remaining = visualType == PlacementVisualType.Single
                ? _selectedOption.SingleCount
                : _selectedOption.ClusterCount;
            if (remaining <= 0)
            {
                CancelSelection();
                return;
            }

            FlowerOption placedOption = _selectedOption;
            if (_gardenService == null ||
                !placedOption.TryGetInventoryKey(out string emotionType, out string owner))
            {
                SetHintVisible(true);
                SetStatus("花卉库存尚未加载，请稍后重试。");
                return;
            }

            int slotIndex = _placementSlots.IndexOf(slot);
            slot.EnsureRuntimeBindings(GetFlowerIds());
            Debug.Log($"[WorldMapFlowerPlacement] 提交落位：slot={slotIndex}，slotName={slot.name}，bindings={slot.VisualBindingCount}，key={placedOption.Id}|{visualType}");
            bool placed = false;
            _commitInProgress = true;
            try
            {
                placed = slotIndex >= 0 && _gardenService.TryPlaceFlower(
                        emotionType,
                        owner,
                        visualType == PlacementVisualType.Cluster,
                        slotIndex,
                        position.x,
                        position.y);
            }
            finally
            {
                _commitInProgress = false;
            }
            if (!placed)
            {
                Debug.LogWarning($"[WorldMapFlowerPlacement] 落位失败：slot={slotIndex}，type={visualType}，inventory={emotionType}|{owner}");
                RefreshInventoryFromSharedService();
                CancelSelection();
                SetHintVisible(true);
                SetStatus("对应花卉库存不足。");
                return;
            }

            PlacementLayer placementLayer = ResolvePlacementLayer(position.y);
            slot.Place(placedOption.Id, visualType, position, _selectedFootprint,
                ResolveCellSize(visualType), placementLayer.Id,
                ResolveFlowerSortingOrder(placementLayer.SortingOrder, placementLayer.BaselineY), _flowerSortingLayerName);
            Debug.Log($"[WorldMapFlowerPlacement] 落位成功：slot={slotIndex}，position={position}，occupied={slot.IsOccupied}");
            placedOption.SyncCounts(_gardenService);

            RefreshOptionViews();
            SetHintVisible(false);
            QueueAutoSave();
            SetStatus("已摆放 " + placedOption.DisplayName + "·" +
                      (visualType == PlacementVisualType.Single ? "单花" : "花丛") +
                      "，可以继续摆放");
        }

        private void ResolveSharedInventoryDependencies()
        {
            if (_gardenService == null)
                ServiceLocator.TryResolve(out _gardenService);

            if (_inventoryChangedSubscription != null && _gardenClearedSubscription != null &&
                _placementsChangedSubscription != null) return;
            if (!ServiceLocator.TryResolve(out EventBus? eventBus) || eventBus == null) return;

            _inventoryChangedSubscription ??= eventBus.Subscribe<EmotionFlowerPlacementInventoryChangedEvent>(
                _ => HandleSharedInventoryChanged());
            _gardenClearedSubscription ??= eventBus.Subscribe<EmotionGardenClearedEvent>(
                _ => HandleSharedInventoryChanged());
            _placementsChangedSubscription ??= eventBus.Subscribe<EmotionFlowerPlacementsChangedEvent>(
                _ => HandleSharedPlacementsChanged());
        }

        private void HandleSharedPlacementsChanged()
        {
            // TryPlaceFlower publishes synchronously.  Restoring from that event while
            // CommitPlacement is still deciding the visual state clears the same slot
            // that is about to be shown, which made a successful click look like a no-op.
            if (_commitInProgress) return;
            RestorePlacedFlowersFromSharedService();
        }

        private void HandleSharedInventoryChanged()
        {
            RefreshInventoryFromSharedService();
            RefreshOptionViews();
        }

        private void RefreshInventoryFromSharedService()
        {
            if (_gardenService == null) return;
            for (int i = 0; i < _options.Count; i++)
                _options[i].SyncCounts(_gardenService);
        }

        private void RestorePlacedFlowersFromSharedService()
        {
            if (_gardenService == null)
                ServiceLocator.TryResolve(out _gardenService);
            if (_gardenService == null) return;

            for (int i = 0; i < _placementSlots.Count; i++)
            {
                if (_placementSlots[i] != null)
                    _placementSlots[i].ClearVisualState();
            }

            IReadOnlyList<PlacedEmotionFlower> placements = _gardenService.GetPlacedFlowers();
            Debug.Log($"[WorldMapFlowerPlacement] 恢复已持久化摆放：{placements.Count} 条记录，场景槽位 {_placementSlots.Count} 个。");
            for (int i = 0; i < placements.Count; i++)
            {
                PlacedEmotionFlower placed = placements[i];
                if (placed.SlotIndex < 0 || placed.SlotIndex >= _placementSlots.Count) continue;

                WorldMapPlacementSlot slot = _placementSlots[placed.SlotIndex];
                if (slot == null) continue;

                string flowerId = EmotionFlowerCatalog.NormalizeOwner(placed.Owner) + "|" +
                                  EmotionFlowerCatalog.NormalizeEmotionType(placed.EmotionType);
                PlacementVisualType visualType = placed.IsCluster
                    ? PlacementVisualType.Cluster
                    : PlacementVisualType.Single;
                Vector2Int footprint = ResolveFootprint(flowerId, visualType);
                PlacementLayer placementLayer = ResolvePlacementLayer(placed.WorldY);
                slot.Place(
                    flowerId,
                    visualType,
                    new Vector2(placed.WorldX, placed.WorldY),
                    footprint,
                    ResolveCellSize(visualType),
                    placementLayer.Id,
                    ResolveFlowerSortingOrder(placementLayer.SortingOrder, placementLayer.BaselineY),
                    _flowerSortingLayerName);
            }
        }

        private Vector2Int ResolveFootprint(string flowerId, PlacementVisualType visualType)
        {
            for (int i = 0; i < _options.Count; i++)
            {
                FlowerOption option = _options[i];
                if (!string.Equals(option.Id, flowerId, StringComparison.Ordinal)) continue;
                return visualType == PlacementVisualType.Single
                    ? option.SingleFootprint
                    : option.ClusterFootprint;
            }

            return Vector2Int.one;
        }

        private Vector2 ResolveCellSize(PlacementVisualType visualType)
        {
            return visualType == PlacementVisualType.Cluster ? _clusterCellSize : _cellSize;
        }

        private void QueueAutoSave()
        {
            _saveQueued = true;
            if (!_saveInProgress)
                _ = FlushAutoSaveQueueAsync();
        }

        private async System.Threading.Tasks.Task FlushAutoSaveQueueAsync()
        {
            _saveInProgress = true;
            try
            {
                while (_saveQueued)
                {
                    _saveQueued = false;
                    if (!ServiceLocator.TryResolve(out ISaveCoordinator? coordinator) || coordinator == null)
                    {
                        Debug.LogWarning("[WorldMapFlowerPlacement] ISaveCoordinator 未注册，本次摆放将在退出游戏时保存。");
                        return;
                    }

                    try
                    {
                        await coordinator.SaveAsync(AutoSaveSlot);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning($"[WorldMapFlowerPlacement] 摆放自动存档失败：{exception.Message}");
                    }
                }
            }
            finally
            {
                _saveInProgress = false;
                if (_saveQueued)
                    _ = FlushAutoSaveQueueAsync();
            }
        }

        private void CancelSelection()
        {
            _isSelecting = false;
            _selectedOption = null;
            _selectedVisualType = null;
            SetPlacementVisuals(false, null);
            SetPlacementMode(false);
            SetHintVisible(false);
            RefreshOptionViews();
            SetStatus(string.Empty);
        }

        private void SetPlacementMode(bool enabled)
        {
            _isSelecting = enabled;
            // 网格只参与编辑器作者化和运行时吸附计算，不再显示，避免把内部层级规则误导成可见格子。
            if (_placementGrid != null) _placementGrid.SetActive(false);
            if (_previewRoot != null) _previewRoot.SetActive(enabled);
        }

        private void SetSidebarVisible(bool visible)
        {
            if (_sidebarPanel != null) _sidebarPanel.SetActive(visible);
            if (_openButton != null) _openButton.gameObject.SetActive(!visible);
            if (_closeButton != null) _closeButton.gameObject.SetActive(visible);
        }

        private void RefreshOptionViews(RectTransform? stableHeader = null)
        {
            Vector2? previousScrollPosition = _sidebarScrollRect?.content != null
                ? _sidebarScrollRect.content.anchoredPosition
                : null;
            Canvas.ForceUpdateCanvases();
            float? stableHeaderViewportY = GetViewportLocalY(stableHeader);

            for (int i = 0; i < _options.Count; i++)
            {
                FlowerOption option = _options[i];
                option.RefreshView(_expandedIndex, i);
                PlacementVisualType? selected = ReferenceEquals(option, _selectedOption)
                    ? _selectedVisualType
                    : null;
                option.SetSelection(selected);
            }

            // 每个条目只通过自身预置的 LayoutElement 高度展开或收起。
            // FlowerList 是唯一纵向排版根，强制在同一帧重排可避免嵌套自动布局的跳动。
            Canvas.ForceUpdateCanvases();
            if (_flowerListLayoutRoot != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_flowerListLayoutRoot);

            if (_sidebarScrollRect?.content == null) return;

            // 展开详情页只应推开下方条目。若从另一条目切换详情，保持本次点击的标题栏
            // 在视窗中的位置，避免前一个详情收起时把当前点击目标一起带动。
            float? rebuiltHeaderViewportY = GetViewportLocalY(stableHeader);
            if (stableHeaderViewportY.HasValue && rebuiltHeaderViewportY.HasValue)
            {
                Vector2 position = _sidebarScrollRect.content.anchoredPosition;
                position.y += stableHeaderViewportY.Value - rebuiltHeaderViewportY.Value;
                _sidebarScrollRect.content.anchoredPosition = position;
            }
            else if (previousScrollPosition.HasValue)
            {
                _sidebarScrollRect.content.anchoredPosition = previousScrollPosition.Value;
            }
        }

        private float? GetViewportLocalY(RectTransform? target)
        {
            if (target == null || _sidebarScrollRect?.viewport == null) return null;
            Vector3 targetWorldPosition = target.TransformPoint(target.rect.center);
            return _sidebarScrollRect.viewport.InverseTransformPoint(targetWorldPosition).y;
        }

        private void SetHintVisible(bool visible)
        {
            if (_hintBubble != null) _hintBubble.SetActive(visible);
        }

        private void SetStatus(string value)
        {
            SetText(_statusText, value);
        }

        private static void SetText(Component? component, string value)
        {
            if (component == null) return;
            var property = component.GetType().GetProperty("text");
            if (property != null && property.CanWrite)
                property.SetValue(component, value, null);
        }

        private WorldMapPlacementSlot? FindFreeSlot()
        {
            for (int i = 0; i < _placementSlots.Count; i++)
            {
                if (_placementSlots[i] != null && !_placementSlots[i].IsOccupied)
                    return _placementSlots[i];
            }

            return null;
        }

        private void SetPlacementVisuals(bool visible, string? key)
        {
            if (_previewRoot != null) _previewRoot.SetActive(visible);
            for (int i = 0; i < _previewVisuals.Count; i++)
            {
                PlacementVisualBinding binding = _previewVisuals[i];
                if (binding.Visual != null)
                    binding.Visual.SetActive(visible &&
                                             string.Equals(binding.Key, key, StringComparison.Ordinal));
            }
        }

        private void SetPreviewTint(Color color, float alpha)
        {
            if (_previewRoot == null) return;
            Color tint = color;
            tint.a = Mathf.Clamp01(alpha);
            foreach (SpriteRenderer renderer in _previewRoot.GetComponentsInChildren<SpriteRenderer>(true))
                renderer.color = tint;
        }

        private void SetPreviewSortingOrder(int sortingOrder, float baselineY)
        {
            if (_previewRoot == null) return;
            int resolvedSortingOrder = ResolveFlowerSortingOrder(sortingOrder, baselineY);
            foreach (SpriteRenderer renderer in _previewRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.sortingLayerName = _flowerSortingLayerName;
                renderer.sortingOrder = resolvedSortingOrder;
            }
        }

        private int ResolveFlowerSortingOrder(int baselineSortingOrder, float baselineY)
        {
            return ResolveSharedSortingOrder(baselineSortingOrder, baselineY, false);
        }

        private int ResolvePetSortingOrder(int baselineSortingOrder, float baselineY)
        {
            return ResolveSharedSortingOrder(baselineSortingOrder, baselineY, true);
        }

        private int ResolveSharedSortingOrder(int baselineSortingOrder, float baselineY, bool petTieBreak)
        {
            // SortingOrder 是主层级；同一主层级内，基线 Y 越低代表越靠近镜头，应该越靠前。
            int baselineYKey = Mathf.Clamp(Mathf.RoundToInt(-baselineY * BaselineYPrecision), 0, SortingOrderStride - 2);
            return SharedSortingBase + _flowerSortingOrderOffset + baselineSortingOrder * SortingOrderStride +
                   baselineYKey + (petTieBreak ? 1 : 0);
        }

        private Vector2 SnapToGrid(Vector2 worldPoint, Vector2Int footprint)
        {
            return SnapToGrid(worldPoint, footprint, null);
        }

        private Vector2 SnapToGrid(Vector2 worldPoint, Vector2Int footprint, PlacementLayer? layerOverride)
        {
            Vector2 origin = ResolveGridOrigin();
            PlacementLayer layer = layerOverride ?? ResolvePlacementLayer(worldPoint.y);
            Vector2 cell = ResolveCellSize(_selectedVisualType ?? PlacementVisualType.Single);
            float offset = layer.XOffset;
            float x = origin.x + offset + Mathf.Floor((worldPoint.x - origin.x - offset) / cell.x) * cell.x + cell.x * 0.5f;
            float y = layer.BaselineY;
            return new Vector2(x, y);
        }

        private PlacementLayer ResolvePreviewPlacementLayer(float worldY)
        {
            PlacementLayer candidate = ResolvePlacementLayer(worldY);
            if (_previewLayer == null)
            {
                _previewLayer = candidate;
                return candidate;
            }

            float currentDistance = Mathf.Abs(worldY - _previewLayer.BaselineY);
            float candidateDistance = Mathf.Abs(worldY - candidate.BaselineY);
            float hysteresis = Mathf.Max(_baselineSnapTolerance, 0f) + Mathf.Max(_layerSwitchHysteresis, 0f);
            if (candidateDistance + hysteresis < currentDistance)
                _previewLayer = candidate;
            return _previewLayer;
        }

        private void SmoothPreviewTint(Color targetColor)
        {
            Color target = targetColor;
            target.a = Mathf.Clamp01(_previewAlpha);
            if (!_previewTintInitialized)
            {
                _previewTint = target;
                _previewTintInitialized = true;
            }
            else
            {
                float t = 1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(0.01f, _previewColorSmoothTime));
                _previewTint = Color.Lerp(_previewTint, target, t);
            }
            SetPreviewTint(_previewTint, 1f);
        }

        private void ResolvePlacementSlotsFallback()
        {
            if (_placementRoot == null) return;
            WorldMapPlacementSlot[] discovered = _placementRoot.GetComponentsInChildren<WorldMapPlacementSlot>(true);
            // 某些场景加载/脚本重载时，子节点组件暂时还不可枚举；绝不能用空结果覆盖
            // Scene 中已经序列化好的槽位引用，否则 FindFreeSlot 会永久返回 null。
            if (discovered.Length == 0)
                return;

            bool needsDiscovery = _placementSlots.Count == 0 || _placementSlots.Count != discovered.Length;
            if (!needsDiscovery)
            {
                for (int i = 0; i < _placementSlots.Count; i++)
                {
                    WorldMapPlacementSlot? slot = _placementSlots[i];
                    if (slot == null || !slot.HasVisualBindings)
                    {
                        needsDiscovery = true;
                        break;
                    }
                }
            }
            if (!needsDiscovery) return;

            Array.Sort(discovered, (left, right) => string.CompareOrdinal(left.name, right.name));
            _placementSlots.Clear();
            _placementSlots.AddRange(discovered);
        }

        private string[] GetFlowerIds()
        {
            string[] ids = new string[_options.Count];
            for (int i = 0; i < _options.Count; i++)
                ids[i] = _options[i].Id;
            return ids;
        }

        private PlacementLayer ResolvePlacementLayer(float worldY)
        {
            if (_placementLayers.Count == 0) return new PlacementLayer();
            PlacementLayer best = _placementLayers[0];
            float bestDistance = Mathf.Abs(worldY - best.BaselineY);
            for (int i = 1; i < _placementLayers.Count; i++)
            {
                PlacementLayer candidate = _placementLayers[i];
                float distance = Mathf.Abs(worldY - candidate.BaselineY);
                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private bool IsValidPlacement(Vector2 center, Vector2Int footprint)
        {
            if (_placementLayers.Count == 0) return false;

            Vector2 size = Vector2.Scale((Vector2)footprint,
                ResolveCellSize(_selectedVisualType ?? PlacementVisualType.Single));
            Rect placement = new(new Vector2(center.x - size.x * 0.5f, center.y), size);
            PlacementLayer layer = ResolvePlacementLayer(center.y);

            if (HasPlacementRegionBindings)
            {
                WorldMapFlowerPlacementRegion? selectedRegion = ResolveSelectedPlacementRegion();
                if (selectedRegion == null || !selectedRegion.Contains(placement)) return false;
            }
            else
            {
                if (_placementSurface == null) return false;

                Bounds bounds = GetPlacementBounds();
                Rect surface = new(bounds.min, bounds.size);
                if (placement.xMin < surface.xMin || placement.xMax > surface.xMax ||
                    center.y < surface.yMin || center.y > surface.yMax ||
                    center.x < layer.XMin || center.x > layer.XMax) return false;
            }

            for (int i = 0; i < _placementSlots.Count; i++)
            {
                WorldMapPlacementSlot slot = _placementSlots[i];
                if (slot == null || !slot.IsOccupied) continue;
                if (!slot.IsOnSamePlacementLayer(layer.Id)) continue;
                if (placement.Overlaps(slot.GetOccupiedRect())) return false;
            }

            return true;
        }

        private Vector2 ResolveGridOrigin()
        {
            if (HasPlacementRegionBindings)
            {
                WorldMapFlowerPlacementRegion? selectedRegion = ResolveSelectedPlacementRegion();
                return selectedRegion != null ? selectedRegion.WorldBounds.min : _gridOrigin;
            }

            if (_useSurfaceBoundsAsGridOrigin && _placementSurface != null)
                return GetPlacementBounds().min;
            return _gridOrigin;
        }

        private void ResolvePlacementSurfaceFallback()
        {
            if (_placementSurface != null) return;

            GameObject? boundsObject = GameObject.Find("FlowerPlacementBounds");
            if (boundsObject != null)
                _placementSurface = boundsObject.GetComponent<Collider2D>();
        }

        private void ResolvePlacementRegionsFallback()
        {
            if (_placementRoot == null) return;

            WorldMapFlowerPlacementRegion[] discovered =
                _placementRoot.GetComponentsInChildren<WorldMapFlowerPlacementRegion>(true);
            if (discovered.Length == 0) return;

            if (_placementRegions.Count == discovered.Length)
            {
                bool complete = true;
                for (int i = 0; i < _placementRegions.Count; i++)
                {
                    if (_placementRegions[i] == null)
                    {
                        complete = false;
                        break;
                    }
                }

                if (complete) return;
            }

            Array.Sort(discovered, (left, right) => string.CompareOrdinal(left.name, right.name));
            _placementRegions.Clear();
            _placementRegions.AddRange(discovered);
        }

        private bool HasPlacementRegionBindings
        {
            get
            {
                return _placementRegions != null && _placementRegions.Count > 0;
            }
        }

        private WorldMapFlowerPlacementRegion? ResolveSelectedPlacementRegion()
        {
            if (_selectedOption == null ||
                !_selectedOption.TryGetInventoryKey(out _, out string owner))
                return null;

            return ResolvePlacementRegion(owner);
        }

        private WorldMapFlowerPlacementRegion? ResolvePlacementRegion(string owner)
        {
            for (int i = 0; i < _placementRegions.Count; i++)
            {
                WorldMapFlowerPlacementRegion? region = _placementRegions[i];
                if (region != null && region.MatchesOwner(owner))
                    return region;
            }

            return null;
        }

        private Bounds GetPlacementBounds()
        {
            return GetPlacementBounds(_placementSurface);
        }

        private static Bounds GetPlacementBounds(Collider2D? surface)
        {
            if (surface is BoxCollider2D box)
            {
                Vector3 scale = box.transform.lossyScale;
                Vector2 absoluteScale = new(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                Vector2 size = Vector2.Scale(box.size, absoluteScale);
                Vector2 offset = Vector2.Scale(box.offset, absoluteScale);
                Vector2 center = (Vector2)box.transform.position + offset;
                return new Bounds(center, size);
            }

            return surface != null ? surface.bounds : new Bounds();
        }

        private bool IsPointerOverPlacementUi()
        {
            if (EventSystem.current == null) return false;
            PointerEventData pointer = new(EventSystem.current) { position = Input.mousePosition };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);
            for (int i = 0; i < results.Count; i++)
            {
                Transform target = results[i].gameObject.transform;
                if (IsWithin(target, _sidebarPanel?.transform) ||
                    IsWithin(target, _openButton?.transform) ||
                    IsWithin(target, _closeButton?.transform) ||
                    IsWithin(target, _hintBubble?.transform))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsWithin(Transform target, Transform? root)
        {
            return root != null && (target == root || target.IsChildOf(root));
        }

        [Serializable]
        public sealed class PlacementVisualBinding
        {
            [SerializeField] private string _key = string.Empty;
            [SerializeField] private GameObject? _visual;

            public string Key => _key;
            public GameObject? Visual => _visual;
        }
    }

}
