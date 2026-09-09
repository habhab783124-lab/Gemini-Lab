#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.FSM;
using GeminiLab.Modules.Furniture;
using GeminiLab.Modules.Navigation;
using GeminiLab.Modules.Pet.Behavior;
using GeminiLab.Modules.Pet.Personality;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Runtime host that ticks pet FSM and stat service.
    /// </summary>
    public sealed class PetController : MonoBehaviour
    {
        private static readonly HashSet<string> DynamicOcclusionFurnitureDefinitionIds = new()
        {
            "家具_工作桌_圆桌书写_天使_01",
            "家具_装饰_高书柜_天使_01",
            "家具_竖琴_天使_01",
            "家具_装饰_盆栽_天使_02",
            "家具_装饰_凳子_天使_01",
            "家具_装饰_桌面雕塑左_天使_01",
            "家具_装饰_床头柜镜台_天使_01"
        };

        private static readonly HashSet<string> AlwaysOnTopFurnitureDefinitionIds = new()
        {
            "家具_装饰_床头柜镜台_天使_01"
        };

        private const string MoveFrontStateName = "Move_Front";
        private const string IdleFrontStateName = "Idle_Front";
        private const string IdleBackStateName = "Idle_Back";
        private const string IdleSideStateName = "Idle_Side";
        private const string SleepStateName = "Sleep";
        private const string SleepPoseAnchorName = "SleepPoseAnchor";
        private const string InteractReadStateName = "Interact_Read";
        private const string InteractBesideDoorStateName = "Interact_BesideDoor";
        private const string InteractFlowerStateName = "Interact_Flower";
        private const string InteractPlayingMusicStateName = "Interact_PlayingMusic";
        private const string InteractWriteStateName = "Interact_Write";
        private const string InteractLookAroundStateName = "Interact_LookAround";
        private const string InteractPlayGameStateName = "Interact_PlayGame";
        private const string InteractDrawStateName = "Interact_Draw";
        private const string InteractSleepStateName = "Interact_DevilSleep";
        private const string BaseLayerStatePrefix = "Base Layer.";
        private const string DevilFTracePrefix = "[DEVIL_F_TRACE]";
        private const string WorldMapSceneName = "WorldMap_Main";

        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int MoveDirHash = Animator.StringToHash("MoveDir");
        // Squared-distance threshold for movement direction updates.
        private const float DirectionEpsilonSqr = 0.000001f;
        private const string SortingAnchorName = "SortingAnchor";

        [SerializeField] private PetStateValueSO? _config;
        [SerializeField] private PersonalityMatrixSO? _personality;
        [SerializeField] private RuntimeAnimatorController? _movementController;
        [SerializeField] private bool _sideFramesFaceLeft = true;
        [SerializeField] private BoxCollider2D? _movementBounds;
        [SerializeField] private ApartmentPetMovement? _apartmentMovement;
        [SerializeField] private Transform? _sortingAnchor;
        [SerializeField] private int _sortingOrderOffset;
        [SerializeField] private PetId _petId = PetId.Angel;
        [SerializeField] private bool _ignoreOtherPetCollisions = true;
        [SerializeField] private PetInteractionVisualStrategy _interactionVisualStrategy = new();

        [Header("行为权重系统（数值规则文档 §25）；为空则回退旧随机漫游")]
        [SerializeField] private BehaviorConfigSO? _behaviorConfig;

        public PetId PetId => _petId;

        /// <summary>气泡等反馈跟随实际显示的宠物；睡觉等家具姿态使用独立视觉对象。</summary>
        public Vector3 VisiblePosition => _sleepInteractionVisualSpriteRenderer != null &&
            _sleepInteractionVisualSpriteRenderer.enabled && _sleepInteractionVisualSpriteRenderer.gameObject.activeInHierarchy
                ? _sleepInteractionVisualSpriteRenderer.transform.position : transform.position;

        /// <summary>初始性格矩阵（Inspector 绑定；可为空）。启动时由 PersonalityEvolutionBootstrap 补种读取。</summary>
        public PersonalityMatrixSO? InitialPersonalityMatrix => _personality;

        private PetContext? _context;
        private StateMachine<PetContext>? _stateMachine;
        private StatTickService? _tickService;
        private IPetCommandLinkService? _commandLinkService;
        private PetPlayerInputController? _playerInputController;
        private Animator? _animator;
        private SpriteRenderer? _spriteRenderer;
        private Rigidbody2D? _rigidbody2D;
        private CapsuleCollider2D? _capsuleCollider2D;
        private GeminiLab.Modules.Furniture.Furniture[]? _dynamicOcclusionFurniture;
        private int _defaultSortingOrder;
        private bool _worldMapSortingOverride;
        private float _initialGroundY;
        private Vector2 _lastAnimationPosition;
        private Vector2 _lastMoveDirection = Vector2.down;
        private Vector2 _playerAnimationDirection = Vector2.down;
        private bool _hasPlayerAnimationDirection;
        private PetAnimatorDirectionDebouncer _animationDirectionDebouncer;
        private string _lastForcedAnimatorStateName = string.Empty;

        // 方向更新的最小 delta 阈值：过滤物理振荡（~0.002），低于此值使用目标方向
        private const float MinDirectionDeltaSqr = 0.0001f; // (0.01)²

        // 漫游卡住检测（基于 velocity 驱动 + Rigidbody2D 物理）
        private Vector2 _wanderPrevActualPosition;
        private bool _hasWanderPrevActualPosition;
        private float _wanderStuckTimer;
        private const float WanderStuckTimeout = 2f;
        private const float WanderStuckMoveThreshold = 0.005f;

        // 漫游受阻 → 家具交互：宠物撞上家具后无法接近漫游目标（物理卡住或沿表面滑动），
        // 短暂等待后转入最近的家具交互动画，而不是一直原地走。
        private bool _wanderInteractionActive;
        private float _wanderInteractionRemaining;
        private string _wanderInteractionAnimatorStateName = string.Empty;
        private float _wanderLastTargetDistance;
        private bool _hasWanderLastTargetDistance;
        private const float WanderInteractionRadius = 2.5f;
        // 本帧目标距离未减少该值即视为未接近目标（与 WanderStuckMoveThreshold 同量级，
        // 小于单帧正常位移 ~0.02，避免把正常移动误判为受阻）。
        private const float WanderProgressThreshold = 0.005f;
        // 未受阻帧的衰减系数：受阻帧 +deltaTime，未受阻帧 -deltaTime*系数。
        // 宠物撞上家具后会「受阻/未受阻」交替抖动，若未受阻直接清零则计时器永远到不了 2s 放弃阈值。
        private const float WanderUnblockedDecayFactor = 0.25f;
        // 自动家具交互冷却：撞上家具时触发失败后 1s 重试，一次交互结束后 5s 限频，
        // 期间若再次卡在家具上会走 2s 放弃逻辑换目标，避免宠物被困在角落反复交互。
        private const float WanderInteractionRetryCooldownSeconds = 1f;
        private const float WanderInteractionPostSuccessCooldown = 5f;
        private float _wanderInteractionRetryCooldown;

        // 行为权重系统驱动状态（数值规则文档 §22 BehaviorRuntimeState 的宿主侧）。
        // 配置了 _behaviorConfig 后，非玩家控制路径不再随机漫游，改为
        // "选行为 → 走到绑定交互点 → 执行 → 结算 → 记录 Recent/冷却" 的循环。
        private enum BehaviorPhase
        {
            IdleWait,
            MovingToBehavior
        }

        private BehaviorPhase _behaviorPhase = BehaviorPhase.IdleWait;
        private float _behaviorIdleTimer = 1f;
        private bool _behaviorIdleIsActiveBehavior;
        private BehaviorConfigSO.BehaviorEntry? _activeBehaviorEntry;
        private AutoInteractionCandidate? _pendingBehaviorCandidate;
        private Vector2 _behaviorMoveStartPosition;
        private bool _behaviorHasMoveStart;
        private int _behaviorFailureCount;
        private string _activeBehaviorTargetName = string.Empty;
        private FurnitureCategory _activeBehaviorCategory = FurnitureCategory.Unknown;
        private FurnitureInteractionType _activeBehaviorInteractionType = FurnitureInteractionType.Unknown;

        /// <summary>TickWanderMovement 的单帧移动结果。</summary>
        private enum WanderMoveOutcome
        {
            None,
            Arrived,
            Abandoned
        }
        private PetRuntimeSnapshotChangedEvent? _lastPublishedSnapshot;
        private readonly List<SpriteRenderer> _hiddenInteractionRenderers = new();
        private readonly List<bool> _hiddenInteractionRendererStates = new();
        private bool _hasStoredInteractionPose;
        private Vector3 _storedInteractionPosition;
        private Vector3 _storedInteractionScale;
        private bool _hasInteractionPoseRuntimeOverride;
        private bool _hasInteractionPhysicsOverride;
        private bool _storedInteractionRigidbodySimulated = true;
        private bool _storedInteractionCapsuleColliderEnabled = true;
        private const int DevilPoseTickTraceFrameInterval = 5;
        private int _lastDevilPoseUpdateTraceFrame = -9999;
        private int _lastDevilPoseFixedTraceFrame = -9999;
        private bool _hasStoredInteractionSorting;
        private int _storedInteractionSortingLayerId;
        private int _storedInteractionSortingOrder;
        private GameObject? _sleepInteractionVisualObject;
        private Transform? _sleepInteractionVisualTransform;
        private Animator? _sleepInteractionVisualAnimator;
        private SpriteRenderer? _sleepInteractionVisualSpriteRenderer;
        private bool _hasStoredPetSpriteVisible;
        private bool _storedPetSpriteVisible;
        private bool _hasAppliedWorldMapPetCollisionPolicy;
        private bool _externalMovementLocked;

        // 可步行表面检测
        private WalkableSurface[] _walkableSurfaces = System.Array.Empty<WalkableSurface>();
        private const int WalkableSurfaceRefreshInterval = 60;
        private int _lastWalkableSurfaceRefreshFrame = -WalkableSurfaceRefreshInterval;

        public string CurrentState => _context?.RuntimeData.CurrentState ?? "None";

        public PetRuntimeData? RuntimeData => _context?.RuntimeData;

        public bool IsPlayerControlEnabled => IsPlayerControlled();

        /// <summary>
        /// 供 WorldMap 当前动画联调入口使用的逐宠物移动锁。
        /// 不改变 Apartment 的移动配置，也不影响另一只桌宠。
        /// </summary>
        public bool IsMovementLocked => _externalMovementLocked;

        public void SetExternalMovementLock(bool locked)
        {
            _externalMovementLocked = locked;
            if (locked)
            {
                StopExternalMovement();
            }
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _capsuleCollider2D = GetComponent<CapsuleCollider2D>();
            _playerInputController = GetComponent<PetPlayerInputController>();
            _initialGroundY = transform.position.y;
            TryAutoBindSortingAnchor();
            EnsureAnimatorBinding();
            EnsurePhysicsBinding();
            _lastAnimationPosition = transform.position;
            _defaultSortingOrder = _spriteRenderer != null ? _spriteRenderer.sortingOrder : 0;
            RefreshDynamicOcclusionFurnitureCache();

            PetStateValueSO config = _config ?? ScriptableObject.CreateInstance<PetStateValueSO>();

            PetRuntimeData runtime = new()
            {
                PetId = _petId,
                Mood = config.InitialMood,
                Energy = config.InitialEnergy,
                Satiety = config.InitialSatiety,
                Position = transform.position
            };

            if (ServiceLocator.TryResolve(out IPetRoster? roster) && roster is not null)
            {
                roster.Register(_petId, runtime);
            }

            if (!ServiceLocator.TryResolve(out EventBus? eventBus))
            {
                eventBus = null;
            }
            else
            {
                // 广播 PetController 初始化完成，供外部（如 PersonalityEvolutionService）观察
                eventBus.Publish(new PetControllerInitializedEvent(_petId, _personality));
            }

            if (!ServiceLocator.TryResolve(out INavigationService? navigationService))
            {
                navigationService = null;
            }

            if (!ServiceLocator.TryResolve(out IFurnitureService? furnitureService))
            {
                furnitureService = null;
            }

            if (!ServiceLocator.TryResolve(out _commandLinkService))
            {
                _commandLinkService = new PetCommandLinkService();
                ServiceLocator.Register<IPetCommandLinkService>(_commandLinkService);
            }

            _context = new PetContext(runtime, config, navigationService, furnitureService, eventBus, _commandLinkService)
            {
                ApplyPosition = position =>
                {
                    ApplyRuntimePosition(position);
                }
            };
            _tickService = new StatTickService();
            _stateMachine = PetStateMachineBuilder.Build(_context);
            _stateMachine.StateChanged += PublishStateChanged;

            // 行为权重系统接管非玩家控制路径的目标选择：RandomWander 不再随机选点。
            if (GetComponent<RandomWander>() is { } wanderComponent)
            {
                wanderComponent.ExternalControlEnabled = _behaviorConfig != null;
            }
        }

        private void Update()
        {
            if (_context is null || _stateMachine is null || _tickService is null)
            {
                return;
            }

            if (_externalMovementLocked)
            {
                TickExternalMovementLock(_context);
                PublishSnapshotIfChanged(_context);
                return;
            }

            if (_hasInteractionPoseRuntimeOverride)
            {
                _context.RuntimeData.Position = ClampToMovementBounds(_context.RuntimeData.Position);
                _context.RuntimeData.TargetPosition = _context.RuntimeData.Position;
            }
            else
            {
                _context.RuntimeData.Position = GetCurrentWorldPosition();
            }

            RefreshLateBoundServices(_context);
            if (IsInactivePlayerPet())
            {
                TickInactivePlayerControlled(_context, Time.deltaTime);
                UpdateMovementAnimation();
                PublishSnapshotIfChanged(_context);
                return;
            }

            if (IsPlayerControlled())
            {
                TickPlayerControlled(_context, Time.deltaTime);
                UpdateMovementAnimation();
                PublishSnapshotIfChanged(_context);
                return;
            }

            HandleDebugCommandInput(_context);
            ProcessCommands(_context, _stateMachine);
            _tickService.Tick(_context, Time.deltaTime);
            _stateMachine.Tick(Time.deltaTime);
            UpdateMovementAnimation();
            TraceDevilInteractionTickIfNeeded("Update", ref _lastDevilPoseUpdateTraceFrame);
            PublishSnapshotIfChanged(_context);
        }

        private void FixedUpdate()
        {
            if (_externalMovementLocked)
            {
                StopExternalMovement();
                return;
            }

            if (_apartmentMovement != null && _apartmentMovement.IsReady && _context != null)
            {
                if (!_hasInteractionPhysicsOverride)
                {
                    _apartmentMovement.FixedTick(Time.fixedDeltaTime);
                    _context.RuntimeData.Position = _apartmentMovement.Position;
                }
                return;
            }

            bool isInactivePlayerPet = IsInactivePlayerPet();

            // 所有桌宠统一适配 WalkableSurface 的 Y 高度
            if (_context is not null && _rigidbody2D != null)
            {
                Vector2 pos = _rigidbody2D.position;
                pos.y = ResolveGroundY(pos.x, ResolveGroundFallbackY(pos.y));
                _rigidbody2D.position = pos;
                if (isInactivePlayerPet)
                {
                    _context.RuntimeData.Position = pos;
                }
            }

            if (isInactivePlayerPet)
                return;

            if (_context is not null)
            {
                ApplyRuntimePosition(_context.RuntimeData.Position);
            }

            _stateMachine?.FixedTick(Time.fixedDeltaTime);
            TraceDevilInteractionTickIfNeeded("FixedUpdate", ref _lastDevilPoseFixedTraceFrame);
        }

        private void LateUpdate()
        {
            UpdateDynamicSortingOrder();
        }

        private void OnDestroy()
        {
            RestoreHiddenInteractionVisuals();
            RestoreSleepInteractionVisual();
            RestoreInteractionSorting();
            if (_stateMachine is not null)
            {
                _stateMachine.StateChanged -= PublishStateChanged;
            }

            if (_sleepInteractionVisualObject != null)
            {
                Destroy(_sleepInteractionVisualObject);
            }
            if (ServiceLocator.TryResolve(out IPetRoster? roster) && roster is not null)
            {
                roster.Unregister(_petId);
            }
        }

        private void Start()
        {
            ApplyWorldMapPetCollisionPolicy();
        }

        private void UpdateDynamicSortingOrder()
        {
            if (_spriteRenderer == null || _hasStoredInteractionSorting || _worldMapSortingOverride)
            {
                return;
            }

            int resolvedSortingOrder = _defaultSortingOrder + _sortingOrderOffset;
            if (TryGetDynamicOcclusionFurniture(out GeminiLab.Modules.Furniture.Furniture? furniture))
            {
                int furnitureSortingOrder = furniture.CurrentSortingOrder;
                if (IsAlwaysOnTopFurniture(furniture))
                {
                    resolvedSortingOrder = furnitureSortingOrder + 1;
                }
                else
                {
                    float petAnchorY = ResolveSortingAnchorY();
                    resolvedSortingOrder = petAnchorY <= furniture.SortingAnchorY
                        ? furnitureSortingOrder + 1
                        : furnitureSortingOrder - 1;
                }
            }

            _spriteRenderer.sortingOrder = resolvedSortingOrder;
        }

        public float WorldMapSortingAnchorY => ResolveSortingAnchorY();

        public void ApplyWorldMapSortingOrder(int sortingOrder, string sortingLayerName)
        {
            if (_spriteRenderer == null || !IsWorldMapScene()) return;

            _worldMapSortingOverride = true;
            _spriteRenderer.sortingLayerName = string.IsNullOrWhiteSpace(sortingLayerName)
                ? "Default"
                : sortingLayerName;
            _spriteRenderer.sortingOrder = sortingOrder;
        }

        private float ResolveSortingAnchorY()
        {
            if (_sortingAnchor != null)
            {
                return _sortingAnchor.position.y;
            }

            if (_capsuleCollider2D != null && _capsuleCollider2D.enabled)
            {
                return _capsuleCollider2D.bounds.min.y;
            }

            if (_spriteRenderer != null)
            {
                return _spriteRenderer.bounds.min.y;
            }

            return transform.position.y;
        }

        private static int CalculateDynamicSortingOrder(float y)
        {
            return -(int)(y * 100f);
        }

        private void RefreshDynamicOcclusionFurnitureCache()
        {
            _dynamicOcclusionFurniture = Object.FindObjectsByType<GeminiLab.Modules.Furniture.Furniture>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
        }

        private bool TryGetDynamicOcclusionFurniture(out GeminiLab.Modules.Furniture.Furniture? bestFurniture)
        {
            bestFurniture = null;

            if (_dynamicOcclusionFurniture == null || _dynamicOcclusionFurniture.Length == 0)
            {
                RefreshDynamicOcclusionFurnitureCache();
            }

            if (_dynamicOcclusionFurniture == null || _dynamicOcclusionFurniture.Length == 0)
            {
                return false;
            }

            Bounds petBounds = ResolveSortingBounds();
            Vector2 petCenter = petBounds.center;
            float petAnchorY = ResolveSortingAnchorY();
            float bestScore = float.PositiveInfinity;

            for (int i = 0; i < _dynamicOcclusionFurniture.Length; i++)
            {
                GeminiLab.Modules.Furniture.Furniture furniture = _dynamicOcclusionFurniture[i];
                if (furniture == null || !furniture.isActiveAndEnabled || !ShouldUseDynamicOcclusionFurniture(furniture))
                {
                    continue;
                }

                if (!furniture.TryGetOcclusionBounds(out Bounds furnitureBounds))
                {
                    continue;
                }

                bool isAlwaysOnTopFurniture = IsAlwaysOnTopFurniture(furniture);

                bool overlapsHorizontally =
                    petBounds.max.x >= furnitureBounds.min.x - GetHorizontalCandidatePadding(isAlwaysOnTopFurniture) &&
                    petBounds.min.x <= furnitureBounds.max.x + GetHorizontalCandidatePadding(isAlwaysOnTopFurniture);
                if (!overlapsHorizontally)
                {
                    continue;
                }

                float furnitureAnchorY = furniture.SortingAnchorY;
                float verticalDistance = Mathf.Abs(petAnchorY - furnitureAnchorY);
                float maxVerticalDistance = petBounds.extents.y + furnitureBounds.extents.y + GetVerticalCandidatePadding(isAlwaysOnTopFurniture);
                if (verticalDistance > maxVerticalDistance)
                {
                    continue;
                }

                Bounds scoringBounds = furnitureBounds;
                if (isAlwaysOnTopFurniture)
                {
                    // Keep small seat/table props competitive even when a nearby large bed or instrument also overlaps.
                    scoringBounds.Expand(new Vector3(0.8f, 0.8f, 0f));
                }

                Vector2 closestPoint = scoringBounds.ClosestPoint(petCenter);
                float distanceScore = Vector2.SqrMagnitude(petCenter - closestPoint);

                if (isAlwaysOnTopFurniture)
                {
                    distanceScore *= 0.0001f;
                }

                if (distanceScore < bestScore)
                {
                    bestScore = distanceScore;
                    bestFurniture = furniture;
                }
            }

            return bestFurniture != null;
        }

        private Bounds ResolveSortingBounds()
        {
            if (_capsuleCollider2D != null && _capsuleCollider2D.enabled)
            {
                return _capsuleCollider2D.bounds;
            }

            if (_spriteRenderer != null)
            {
                return _spriteRenderer.bounds;
            }

            return new Bounds(transform.position, Vector3.zero);
        }

        private static bool ShouldUseDynamicOcclusionFurniture(GeminiLab.Modules.Furniture.Furniture furniture)
        {
            if (furniture.UseDynamicSortingRule)
            {
                return true;
            }

            string definitionId = furniture.DefinitionId;
            return !string.IsNullOrWhiteSpace(definitionId) &&
                   DynamicOcclusionFurnitureDefinitionIds.Contains(definitionId);
        }

        private static bool IsAlwaysOnTopFurniture(GeminiLab.Modules.Furniture.Furniture furniture)
        {
            string definitionId = furniture.DefinitionId;
            return !string.IsNullOrWhiteSpace(definitionId) &&
                   AlwaysOnTopFurnitureDefinitionIds.Contains(definitionId);
        }

        private static float GetHorizontalCandidatePadding(bool isAlwaysOnTopFurniture)
        {
            return isAlwaysOnTopFurniture ? 0.9f : 0.2f;
        }

        private static float GetVerticalCandidatePadding(bool isAlwaysOnTopFurniture)
        {
            return isAlwaysOnTopFurniture ? 1.4f : 0.8f;
        }

        private void TryAutoBindSortingAnchor()
        {
            if (_sortingAnchor != null)
            {
                return;
            }

            Transform[] transforms = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate == transform)
                {
                    continue;
                }

                if (!string.Equals(candidate.name, SortingAnchorName, System.StringComparison.Ordinal))
                {
                    continue;
                }

                _sortingAnchor = candidate;
                return;
            }
        }

        public static void PublishStateChanged(string from, string to)
        {
            Debug.Log($"[PetFSM] {from} -> {to}");
            if (ServiceLocator.TryResolve(out EventBus? eventBus) && eventBus is not null)
            {
                eventBus.Publish(new PetStateChangedEvent(from, to));
            }
        }

        private static void ProcessCommands(PetContext context, StateMachine<PetContext> stateMachine)
        {
            if (context.CommandLinkService is null)
            {
                return;
            }

            while (context.CommandLinkService.TryDequeue(out PetCommand command))
            {
                PetCommandRequest request = command.Request;
                context.RuntimeData.LastTraceId = request.TraceId;
                if (context.IsSleeping && request.ForceWake && request.CommandType == PetCommandType.WorkRequest)
                {
                    float moodPenalty = context.Config.ForceWakeMoodPenalty;
                    context.RuntimeData.Mood = Mathf.Clamp(context.RuntimeData.Mood - moodPenalty, 0f, 100f);
                    context.RuntimeData.PreventSleepBeforeTime = context.RuntimeData.RuntimeTimeSeconds + 3f;
                    stateMachine.ForceChangeState<IdleState>();
                    context.EventBus?.Publish(new PetWakePenaltyAppliedEvent(request.TraceId, -moodPenalty));
                }

                switch (request.CommandType)
                {
                    case PetCommandType.WorkRequest:
                        if (!context.IsSleeping || request.ForceWake)
                        {
                            if (request.TargetType == PetWorkTargetType.WorkDesk &&
                                (context.FurnitureService is null ||
                                 !context.FurnitureService.TryGetBestInteractionTarget(context.RuntimeData.Position, FurnitureInteractionQuery.WorkDeskOnly, out FurnitureInteractionTarget _)))
                            {
                                context.EventBus?.Publish(new PetCommandRejectedEvent(request.TraceId, "No available WorkDesk target."));
                                break;
                            }

                            context.RuntimeData.WorkRequested = true;
                            context.RuntimeData.ActiveWorkTraceId = request.TraceId;
                            context.RuntimeData.ActiveWorkMessage = request.Message;
                            context.RuntimeData.RequiredWorkTargetType = request.TargetType;
                            context.RuntimeData.IsAtRequiredWorkTarget = false;
                            context.RuntimeData.TargetFurnitureId = string.Empty;
                            context.RuntimeData.TargetReached = false;
                            context.RuntimeData.ActivePath.Clear();
                            context.EventBus?.Publish(new PetCommandAcceptedEvent(request.TraceId, request.ForceWake, request.CommandType, request.Source));
                        }
                        else
                        {
                            context.EventBus?.Publish(new PetCommandRejectedEvent(request.TraceId, "Pet is sleeping and command is not force-wake."));
                        }

                        break;
                    case PetCommandType.WorkCompleted:
                        if (request.Source == PetCommandSource.Gateway &&
                            string.Equals(context.RuntimeData.ActiveWorkTraceId, request.TraceId, System.StringComparison.Ordinal))
                        {
                            context.RuntimeData.WorkRequested = false;
                            context.EventBus?.Publish(new PetWorkCompletedEvent(request.TraceId, request.Message));
                            ResetWorkRuntime(context);
                        }
                        else
                        {
                            context.EventBus?.Publish(new PetCommandRejectedEvent(request.TraceId, "WorkCompleted ignored: source is not Gateway or traceId mismatch."));
                        }

                        break;
                    case PetCommandType.WorkFailed:
                        if (request.Source == PetCommandSource.Gateway &&
                            string.Equals(context.RuntimeData.ActiveWorkTraceId, request.TraceId, System.StringComparison.Ordinal))
                        {
                            context.RuntimeData.WorkRequested = false;
                            context.EventBus?.Publish(new PetWorkFailedEvent(request.TraceId, request.Message));
                            ResetWorkRuntime(context);
                        }
                        else
                        {
                            context.EventBus?.Publish(new PetCommandRejectedEvent(request.TraceId, "WorkFailed ignored: source is not Gateway or traceId mismatch."));
                        }

                        break;
                }
            }
        }

        private void HandleDebugCommandInput(PetContext context)
        {
            if (_commandLinkService is null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                bool forceWake = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                string traceId = _commandLinkService.RequestWork(forceWake);
                context.RuntimeData.LastTraceId = traceId;
                Debug.Log($"[PetCommand] Debug work requested traceId={traceId}, forceWake={forceWake}");
            }
        }

        private static void RefreshLateBoundServices(PetContext context)
        {
            if (context.NavigationService is null && ServiceLocator.TryResolve(out INavigationService? navigationService))
            {
                context.NavigationService = navigationService;
            }

            if (context.FurnitureService is null && ServiceLocator.TryResolve(out IFurnitureService? furnitureService))
            {
                context.FurnitureService = furnitureService;
            }

            if (context.EventBus is null && ServiceLocator.TryResolve(out EventBus? eventBus))
            {
                context.EventBus = eventBus;
            }

            if (context.CommandLinkService is null && ServiceLocator.TryResolve(out IPetCommandLinkService? commandLinkService))
            {
                context.CommandLinkService = commandLinkService;
            }
        }

        private bool IsPlayerControlled()
        {
            return HasPlayerInputController() && _playerInputController!.InputEnabled;
        }

        private bool IsInactivePlayerPet()
        {
            return HasPlayerInputController() && !_playerInputController!.InputEnabled;
        }

        private bool HasPlayerInputController()
        {
            if (_playerInputController == null)
            {
                _playerInputController = GetComponent<PetPlayerInputController>();
            }

            return _playerInputController != null;
        }

        private void TickPlayerControlled(PetContext context, float deltaTime)
        {
            _apartmentMovement?.SetManualVelocity(Vector2.zero);
            // 玩家接管后终止漫游触发中的家具交互，避免取消选中后残留交互动画。
            // 同时还原自动交互期间应用的覆盖（pose/可视/排序），否则宠物会被钉在交互点。
            if (_wanderInteractionActive)
            {
                _wanderInteractionActive = false;
                _wanderInteractionAnimatorStateName = string.Empty;
                _wanderInteractionRemaining = 0f;
                RestoreHiddenInteractionVisuals();
                RestoreSleepInteractionVisual();
                RestoreInteractionSorting();
                RestoreInteractionPose();
            }

            // 玩家接管同样打断行为驱动循环（未完成的移动/行为按"未完成"处理，不结算不记录）。
            CancelBehaviorDrivenLoop(context);

            if (TickPlayerInteraction(context, deltaTime))
            {
                return;
            }

            Vector2 movementInput = default;
            Vector2 rawInput = default;
            bool canMove = !context.RuntimeData.IsTraveling &&
                           _playerInputController != null &&
                           _playerInputController.TryGetMovementInput(out movementInput, out rawInput);

            _hasPlayerAnimationDirection = canMove;
            if (canMove)
            {
                _playerAnimationDirection = ResolvePlayerAnimationDirection(rawInput, _lastMoveDirection);
            }

            SetPlayerControlledState(context, canMove ? MovingState.StateName : IdleState.StateName);
            context.Advance(deltaTime);
            _tickService?.Tick(context, deltaTime);
            ResetPlayerControlledRuntime(context);

            if (!canMove || _playerInputController == null)
            {
                return;
            }

            if (_apartmentMovement != null && _apartmentMovement.IsReady)
            {
                _apartmentMovement.SetManualVelocity(movementInput * _playerInputController.MoveSpeed);
                return;
            }

            context.RuntimeData.Position += movementInput * _playerInputController.MoveSpeed * deltaTime;
            context.RuntimeData.Position = ClampToMovementBounds(context.RuntimeData.Position);
            context.RuntimeData.TargetPosition = context.RuntimeData.Position;
            context.RuntimeData.TargetReached = true;
        }

        private void TickInactivePlayerControlled(PetContext context, float deltaTime)
        {
            CancelPlayerInteraction(context);
            _hasPlayerAnimationDirection = false;

            // 配置了行为表：非玩家控制路径由行为权重系统驱动（数值规则文档 §25），
            // 不再使用随机漫游 + 碰撞触发交互的旧循环。
            if (_behaviorConfig != null)
            {
                TickBehaviorDriven(context, deltaTime);
                return;
            }

            if (_wanderInteractionActive)
            {
                TickWanderInteraction(context, deltaTime);
                return;
            }

            _wanderInteractionRetryCooldown = Mathf.Max(0f, _wanderInteractionRetryCooldown - deltaTime);

            RandomWander? wander = GetComponent<RandomWander>();
            bool isWandering = false;

            if (wander != null)
            {
                TickWanderMovement(context, wander, deltaTime, allowBumpInteraction: true, out isWandering);
            }
            else
            {
                SetWanderVelocity(Vector2.zero);
                _wanderStuckTimer = 0f;
                _hasWanderPrevActualPosition = false;
                _hasWanderLastTargetDistance = false;
            }

            SetPlayerControlledState(
                context,
                _wanderInteractionActive
                    ? InteractingState.StateName
                    : isWandering ? MovingState.StateName : IdleState.StateName);
            context.Advance(deltaTime);
            _tickService?.Tick(context, deltaTime);
            ResetPlayerControlledRuntime(context);

            if (_wanderInteractionActive)
            {
                // 交互启动帧：扣减剩余时长，结束后恢复正常漫游等待。
                _wanderInteractionRemaining -= deltaTime;
                if (_wanderInteractionRemaining <= 0f)
                {
                    EndWanderInteraction();
                    // 同帧切回 Idle，避免 UpdateMovementAnimation 仍以 Interacting 状态
                    // 播放一次 Move_Front 兜底动画。
                    SetPlayerControlledState(context, IdleState.StateName);
                }
            }
            else if (!isWandering)
            {
                context.RuntimeData.TargetPosition = context.RuntimeData.Position;
                context.RuntimeData.TargetReached = true;
            }
        }

        /// <summary>
        /// 漫游/行为移动的统一位移推进：朝 <see cref="RandomWander.TargetPosition"/> 移动，
        /// 处理到达、物理卡住与超时放弃。旧漫游路径（allowBumpInteraction=true）在物理卡住时
        /// 会立即尝试最近家具交互；行为驱动路径（false）不允许这种"误触"，行为只能由选择器选出。
        /// </summary>
        private WanderMoveOutcome TickWanderMovement(
            PetContext context,
            RandomWander wander,
            float deltaTime,
            bool allowBumpInteraction,
            out bool isWandering)
        {
            isWandering = false;
            if (!wander.IsMoving)
            {
                SetWanderVelocity(Vector2.zero);
                _wanderStuckTimer = 0f;
                _hasWanderPrevActualPosition = false;
                _hasWanderLastTargetDistance = false;
                return WanderMoveOutcome.None;
            }

            isWandering = true;
            if (_apartmentMovement != null && _apartmentMovement.IsReady)
            {
                if (!_apartmentMovement.Follow(wander.TargetPosition, wander.MoveSpeed, wander.ArrivalThreshold, out bool arrived))
                {
                    _apartmentMovement.Stop();
                    wander.AbandonTarget();
                    isWandering = false;
                    return WanderMoveOutcome.Abandoned;
                }
                context.RuntimeData.Position = _apartmentMovement.Position;
                context.RuntimeData.TargetPosition = wander.TargetPosition;
                context.RuntimeData.TargetReached = arrived;
                if (!arrived) return WanderMoveOutcome.None;
                _apartmentMovement.Stop();
                wander.NotifyArrived();
                isWandering = false;
                return WanderMoveOutcome.Arrived;
            }
            Vector2 current = GetCurrentWorldPosition();
            Vector2 toTarget = wander.TargetPosition - current;
            if (wander.HorizontalOnly)
            {
                toTarget.y = 0f;
            }

            if (toTarget.sqrMagnitude <= wander.ArrivalThreshold * wander.ArrivalThreshold)
            {
                SetWanderVelocity(Vector2.zero);
                context.RuntimeData.Position = ResolveWanderArrivedPosition(wander);
                wander.NotifyArrived();
                isWandering = false;
                _wanderStuckTimer = 0f;
                _hasWanderPrevActualPosition = false;
                _hasWanderLastTargetDistance = false;
                return WanderMoveOutcome.Arrived;
            }

            float step = wander.MoveSpeed * deltaTime;

            // 检测是否卡住（本帧实际位置 vs 上帧实际位置）
            bool stuckThisFrame = false;
            if (_hasWanderPrevActualPosition)
            {
                float actualMove = Vector2.Distance(current, _wanderPrevActualPosition);
                stuckThisFrame = step > WanderStuckMoveThreshold && actualMove < WanderStuckMoveThreshold;
            }

            // 受阻 = 撞上家具：要么物理上卡住不动，要么沿表面滑动但无法接近漫游目标。
            // 物理卡住帧用于立即触发家具交互；距离未缩短（含滑行）仅用于累计 2s 放弃计时。
            bool blockedThisFrame = stuckThisFrame;
            if (!blockedThisFrame && _hasWanderLastTargetDistance)
            {
                float targetDistance = toTarget.magnitude;
                blockedThisFrame = step > WanderStuckMoveThreshold &&
                                   targetDistance > _wanderLastTargetDistance - WanderProgressThreshold;
            }
            _wanderLastTargetDistance = toTarget.magnitude;
            _hasWanderLastTargetDistance = true;

            if (blockedThisFrame)
            {
                _wanderStuckTimer += deltaTime;
                // 物理真正卡住（本帧实际无位移）时立即尝试家具交互，不依赖累计计时。
                // 仅旧漫游路径允许这种"撞上什么玩什么"的交互；行为驱动路径下行为由选择器决定。
                if (allowBumpInteraction && stuckThisFrame && _wanderInteractionRetryCooldown <= 0f)
                {
                    if (TryStartWanderInteraction(context))
                    {
                        // 交互已启动：本帧立即进入 Interacting 状态，后续由 TickWanderInteraction 处理。
                        isWandering = false;
                    }
                    else
                    {
                        // 半径内没有可交互家具：短暂冷却避免每帧重试，之后靠 2s 放弃逻辑换目标。
                        _wanderInteractionRetryCooldown = WanderInteractionRetryCooldownSeconds;
                    }
                }

                if (!_wanderInteractionActive && _wanderStuckTimer >= WanderStuckTimeout)
                {
                    // 卡住 2s 且未触发家具交互（没有可用家具 / 交互冷却中）：
                    // 放弃当前目标，避免一直原地走。
                    SetWanderVelocity(Vector2.zero);
                    wander.AbandonTarget();
                    isWandering = false;
                    _wanderStuckTimer = 0f;
                    _hasWanderPrevActualPosition = false;
                    _hasWanderLastTargetDistance = false;
                    return WanderMoveOutcome.Abandoned;
                }
                // 受阻但未放弃：保持 velocity，继续朝向目标播放移动动画
            }
            else
            {
                // 未受阻：衰减而非清零。撞上家具后受阻/未受阻帧交替抖动，
                // 清零会让计时器永远到不了阈值；真正自由移动时衰减回 0 不会误触发。
                _wanderStuckTimer = Mathf.Max(0f, _wanderStuckTimer - deltaTime * WanderUnblockedDecayFactor);
                SetWanderVelocity(toTarget.normalized * wander.MoveSpeed);
            }

            _wanderPrevActualPosition = current;
            _hasWanderPrevActualPosition = true;
            context.RuntimeData.TargetPosition = wander.TargetPosition;
            context.RuntimeData.TargetReached = false;
            return WanderMoveOutcome.None;
        }

        // ------------------------------------------------------------------
        // 行为权重驱动循环（数值规则文档 §25）：
        // 待机/抽取 → 必要时 Moving（§8 移动是中间状态不是行为）→ 执行绑定交互
        // → 结算 Energy/Mood（§12）→ 记录 RecentActions/Cooldown（§9/§10）。
        // ------------------------------------------------------------------

        private void TickBehaviorDriven(PetContext context, float deltaTime)
        {
            RandomWander? wander = GetComponent<RandomWander>();
            if (wander != null && !wander.ExternalControlEnabled)
            {
                wander.ExternalControlEnabled = true;
            }

            if (_wanderInteractionActive)
            {
                TickBehaviorPerforming(context, deltaTime);
                return;
            }

            bool isWandering = false;
            switch (_behaviorPhase)
            {
                case BehaviorPhase.IdleWait:
                    SetWanderVelocity(Vector2.zero);
                    context.RuntimeData.TargetPosition = context.RuntimeData.Position;
                    context.RuntimeData.TargetReached = true;
                    _behaviorIdleTimer -= deltaTime;
                    if (_behaviorIdleTimer <= 0f)
                    {
                        if (_behaviorIdleIsActiveBehavior)
                        {
                            // 原地行为（待机）时长结束 = 行为完成。
                            CompleteActiveBehavior(context, completedNaturally: true);
                        }
                        else
                        {
                            StartNextBehavior(context, wander);
                        }
                    }
                    break;

                case BehaviorPhase.MovingToBehavior:
                    if (wander == null)
                    {
                        AbortActiveBehavior(context, 1f);
                        break;
                    }

                    WanderMoveOutcome outcome = TickWanderMovement(context, wander, deltaTime, allowBumpInteraction: false, out isWandering);
                    if (outcome == WanderMoveOutcome.Arrived)
                    {
                        OnBehaviorMoveArrived(context);
                    }
                    else if (outcome == WanderMoveOutcome.Abandoned)
                    {
                        AbortActiveBehavior(context, 1f);
                    }
                    break;
            }

            SetPlayerControlledState(
                context,
                _wanderInteractionActive
                    ? ResolvePerformingStateName()
                    : isWandering ? MovingState.StateName : IdleState.StateName);
            context.Advance(deltaTime);
            _tickService?.Tick(context, deltaTime);
            ResetPlayerControlledRuntime(context);

            if (!isWandering && _behaviorPhase != BehaviorPhase.MovingToBehavior)
            {
                context.RuntimeData.TargetPosition = context.RuntimeData.Position;
                context.RuntimeData.TargetReached = true;
            }
        }

        /// <summary>行为执行中：沿用漫游交互的可视/pose/动画机制；自然结束时结算并记录。</summary>
        private void TickBehaviorPerforming(PetContext context, float deltaTime)
        {
            SetWanderVelocity(Vector2.zero);
            SetPlayerControlledState(context, ResolvePerformingStateName());
            context.Advance(deltaTime);
            _tickService?.Tick(context, deltaTime);
            ResetPlayerControlledRuntime(context);
            context.RuntimeData.TargetReached = true;

            _wanderInteractionRemaining -= deltaTime;
            if (_wanderInteractionRemaining > 0f)
            {
                return;
            }

            EndWanderInteraction();
            CompleteActiveBehavior(context, completedNaturally: true);
            // 同帧切回 Idle，避免 UpdateMovementAnimation 仍以 Interacting 状态
            // 播放一次 Move_Front 兜底动画。
            SetPlayerControlledState(context, IdleState.StateName);
        }

        /// <summary>睡觉行为执行期间状态名报 Sleeping（§11 睡觉停止精力自然衰减），其余报 Interacting。</summary>
        private string ResolvePerformingStateName()
        {
            return _activeBehaviorEntry != null && _activeBehaviorEntry.BehaviorId == PetBehaviorIds.Sleep
                ? SleepingState.StateName
                : InteractingState.StateName;
        }

        /// <summary>
        /// 抽取下一个行为（§25：候选 → 精力硬规则 → 冷却 → 四倍率 → 加权随机）。
        /// 精力 ≤ 强制阈值时由选择器直接返回睡觉（§4）。
        /// </summary>
        private void StartNextBehavior(PetContext context, RandomWander? wander)
        {
            if (_behaviorConfig == null)
            {
                _behaviorIdleTimer = 1f;
                return;
            }

            PersonalityVector personality = default;
            if (ServiceLocator.TryResolve(out IPersonalityEvolutionService? personalityService) && personalityService is not null)
            {
                personality = personalityService.GetMatrix(context.RuntimeData.PetId);
            }

            BehaviorSelectorInput input = new(
                context.RuntimeData.Energy,
                context.RuntimeData.Mood,
                personality,
                context.RuntimeData.RuntimeTimeSeconds,
                context.Config.ForcedSleepEnergyThreshold,
                context.Config.SleepExcludedAboveEnergy,
                context.Config.ActiveBehaviorMinEnergy);

            BehaviorPickResult? pick = BehaviorSelector.Pick(
                _behaviorConfig,
                input,
                context.RuntimeData.BehaviorState,
                static () => UnityEngine.Random.value);

            if (pick is null)
            {
                _behaviorIdleTimer = 1f;
                return;
            }

            BehaviorConfigSO.BehaviorEntry entry = pick.Value.Entry;

            if (!entry.RequiresMovement)
            {
                // 原地行为（待机）：原地停留一段时长，到时按完成结算。
                _activeBehaviorEntry = entry;
                _pendingBehaviorCandidate = null;
                _behaviorPhase = BehaviorPhase.IdleWait;
                _behaviorIdleIsActiveBehavior = true;
                _behaviorIdleTimer = Mathf.Max(0.1f, _behaviorConfig.RollIdleDurationSeconds());
                context.RuntimeData.CurrentBehaviorId = entry.BehaviorId;
                context.RuntimeData.BehaviorState.CurrentBehaviorId = entry.BehaviorId;
                return;
            }

            _behaviorIdleIsActiveBehavior = false;

            if (wander == null ||
                !TryGetComponent(out PetPlayerFurnitureInteractionController interactionController) ||
                !interactionController.TryGetInteractionCandidateByType(entry.BindingInteractionType, out AutoInteractionCandidate candidate))
            {
                Debug.LogWarning(
                    $"[PetBehavior] {_petId} 行为 '{entry.BehaviorId}' 找不到 InteractionType={entry.BindingInteractionType} " +
                    "的交互绑定，按已尝试记录冷却后重新选择。");
                // 防死循环：绑定缺失的行为按已尝试记录，避免每帧重复选中同一个不可执行行为。
                context.RuntimeData.BehaviorState.RecordCompletion(
                    entry.BehaviorId, context.RuntimeData.RuntimeTimeSeconds, entry.CooldownSeconds);
                _behaviorFailureCount++;
                _behaviorIdleTimer = Mathf.Min(1f * _behaviorFailureCount, 15f);
                return;
            }

            _activeBehaviorEntry = entry;
            _pendingBehaviorCandidate = candidate;
            _behaviorPhase = BehaviorPhase.MovingToBehavior;
            _behaviorMoveStartPosition = GetCurrentWorldPosition();
            _behaviorHasMoveStart = true;
            context.RuntimeData.CurrentBehaviorId = entry.BehaviorId;
            context.RuntimeData.BehaviorState.CurrentBehaviorId = entry.BehaviorId;
            wander.SetExternalTarget(candidate.InteractionPoint);
        }

        /// <summary>到达行为目标点：结算移动能耗（§8），启动该行为绑定的交互。</summary>
        private void OnBehaviorMoveArrived(PetContext context)
        {
            // §8：单次普通移动 Energy -1，仅当移动距离超过策划配置阈值时结算一次（短距离白走）。
            if (_behaviorHasMoveStart && context.Config.MoveEnergyCost > 0f)
            {
                float distance = Vector2.Distance(_behaviorMoveStartPosition, context.RuntimeData.Position);
                if (distance >= context.Config.MoveEnergyCostMinDistance)
                {
                    StatTickService.ApplyEnvironmentalBuff(context.RuntimeData, 0f, -context.Config.MoveEnergyCost);
                }
            }
            _behaviorHasMoveStart = false;

            BehaviorConfigSO.BehaviorEntry? entry = _activeBehaviorEntry;
            AutoInteractionCandidate? candidate = _pendingBehaviorCandidate;
            if (entry == null || candidate == null || !StartBehaviorInteraction(context, entry, candidate.Value))
            {
                AbortActiveBehavior(context, 1f);
            }
        }

        /// <summary>启动选中行为的绑定交互：复用漫游交互的动画/可视/pose 机制。</summary>
        private bool StartBehaviorInteraction(
            PetContext context,
            BehaviorConfigSO.BehaviorEntry entry,
            AutoInteractionCandidate candidate)
        {
            PetPlayerInteractionRequest request = candidate.Request;
            _activeBehaviorTargetName = request.TargetName;
            _activeBehaviorCategory = request.Category;
            _activeBehaviorInteractionType = request.InteractionType;

            FurnitureInteractionTarget target = new(
                request.TargetName,
                request.TargetName,
                request.Category,
                request.InteractionType,
                request.InteractionDurationSeconds,
                candidate.InteractionPoint,
                0f);

            StartWanderInteractionFromRequest(
                context,
                target,
                request,
                hasRequestData: true,
                durationOverrideSeconds: entry.OverrideDurationSeconds,
                alwaysPublishCompletionEvent: true);
            return true;
        }

        /// <summary>
        /// 行为收尾：清理驱动状态；自然完成时按 §12 结算 Energy/Mood、按 §9/§10 记录
        /// 最近行为与冷却（冷却从行为结束时起算），随后进入短暂过渡停顿再抽下一个行为。
        /// </summary>
        private void CompleteActiveBehavior(PetContext context, bool completedNaturally)
        {
            BehaviorConfigSO.BehaviorEntry? entry = _activeBehaviorEntry;
            _activeBehaviorEntry = null;
            _pendingBehaviorCandidate = null;
            _behaviorIdleIsActiveBehavior = false;
            _behaviorHasMoveStart = false;
            _behaviorPhase = BehaviorPhase.IdleWait;
            context.RuntimeData.CurrentBehaviorId = string.Empty;
            context.RuntimeData.BehaviorState.CurrentBehaviorId = string.Empty;

            if (completedNaturally && entry != null)
            {
                // §12：行为完成一次性结算（普通行为 -1~-4 精力、+0~+2 心情；睡觉 +25 精力）。
                StatTickService.ApplyEnvironmentalBuff(context.RuntimeData, entry.MoodDelta, entry.EnergyDelta);
                context.RuntimeData.BehaviorState.RecordCompletion(
                    entry.BehaviorId, context.RuntimeData.RuntimeTimeSeconds, entry.CooldownSeconds);

                if (entry.RequiresMovement)
                {
                    context.RuntimeData.LastInteractionFurnitureId = _activeBehaviorTargetName;
                    context.RuntimeData.LastInteractionSummary =
                        $"{entry.Label} / {_activeBehaviorCategory} (Mood {FormatSigned(entry.MoodDelta)}, Energy {FormatSigned(entry.EnergyDelta)})";
                }

                _behaviorFailureCount = 0;
                _behaviorIdleTimer = _behaviorConfig != null ? _behaviorConfig.TransitionPauseSeconds : 1f;
            }
            else
            {
                _behaviorIdleTimer = 0.5f;
            }

            _activeBehaviorTargetName = string.Empty;
            _activeBehaviorCategory = FurnitureCategory.Unknown;
            _activeBehaviorInteractionType = FurnitureInteractionType.Unknown;
        }

        /// <summary>
        /// 行为未能执行（绑定缺失/移动被放弃/玩家接管）：不结算，但按"已尝试"记录冷却，
        /// 避免反复选中同一个不可达行为；连续失败时退避拉长重试间隔。
        /// </summary>
        private void AbortActiveBehavior(PetContext context, float idleSeconds)
        {
            BehaviorConfigSO.BehaviorEntry? entry = _activeBehaviorEntry;
            CompleteActiveBehavior(context, completedNaturally: false);
            if (entry != null)
            {
                context.RuntimeData.BehaviorState.RecordCompletion(
                    entry.BehaviorId, context.RuntimeData.RuntimeTimeSeconds, entry.CooldownSeconds);
            }

            _behaviorFailureCount++;
            _behaviorIdleTimer = Mathf.Min(idleSeconds * _behaviorFailureCount, 15f);
        }

        /// <summary>玩家接管时打断行为驱动循环：移动中放弃目标，未完成的行为不结算不记录。</summary>
        private void CancelBehaviorDrivenLoop(PetContext context)
        {
            if (_behaviorConfig == null)
            {
                return;
            }

            if (_activeBehaviorEntry == null && _behaviorPhase == BehaviorPhase.IdleWait)
            {
                return;
            }

            if (_behaviorPhase == BehaviorPhase.MovingToBehavior &&
                GetComponent<RandomWander>() is { } wander)
            {
                wander.AbandonTarget();
            }

            CompleteActiveBehavior(context, completedNaturally: false);
        }

        private void TickWanderInteraction(PetContext context, float deltaTime)
        {
            SetWanderVelocity(Vector2.zero);
            SetPlayerControlledState(context, InteractingState.StateName);
            context.Advance(deltaTime);
            _tickService?.Tick(context, deltaTime);
            ResetPlayerControlledRuntime(context);
            context.RuntimeData.TargetReached = true;

            _wanderInteractionRemaining -= deltaTime;
            if (_wanderInteractionRemaining > 0f)
            {
                return;
            }

            EndWanderInteraction();
            // 同帧切回 Idle，避免 UpdateMovementAnimation 仍以 Interacting 状态
            // 播放一次 Move_Front 兜底动画。
            SetPlayerControlledState(context, IdleState.StateName);
        }

        private void EndWanderInteraction()
        {
            _wanderInteractionActive = false;
            _wanderInteractionAnimatorStateName = string.Empty;
            SetWanderVelocity(Vector2.zero);
            _wanderStuckTimer = 0f;
            // 交互结束后限频：期间若再次撞上家具，走 2s 放弃逻辑换目标，
            // 避免宠物被困在角落「交互→等待→再交互」死循环，也避免交互过于频繁。
            _wanderInteractionRetryCooldown = WanderInteractionPostSuccessCooldown;
            _hasWanderPrevActualPosition = false;
            _hasWanderLastTargetDistance = false;
            // 还原自动交互期间应用的可视/排序/pose 覆盖（与手动路径一致），
            // 并把宠物恢复回交互前的游荡位置。
            RestoreHiddenInteractionVisuals();
            RestoreSleepInteractionVisual();
            RestoreInteractionSorting();
            RestoreInteractionPose();
            RandomWander? wander = GetComponent<RandomWander>();
            if (wander != null)
            {
                wander.NotifyArrived();
            }
        }

        private bool TryStartWanderInteraction(PetContext context)
        {
            if (_wanderInteractionActive)
            {
                return false;
            }

            // 优先用宠物自身的交互绑定（与手动 F 键/点击同一套：硬编码交互点 + 显式动画状态名 + pose 数据）。
            // 公寓场景里 FurnitureService._placedFurniture 为空（ApartmentSceneFurnitureBindings 的
            // ResolveTarget 全部解析失败，Editor.log 有 30 条 "Skip binding" 警告），自动交互不能拿它当主依赖；
            // 但仍有场景会注册家具，所以命中失败后兜底再试 _placedFurniture（request 为 default，
            // 状态用 ResolveFurnitureInteractionStateName 按宠物映射）。
            PetPlayerInteractionRequest request = default;
            bool hasRequestData = TryFindNearbyAutoBinding(out FurnitureInteractionTarget target, out request);
            bool found = hasRequestData || TryFindNearbyFurniture(context, out target);

            if (!found)
            {
                return false;
            }

            StartWanderInteractionFromRequest(context, target, request, hasRequestData);
            return true;
        }

        /// <summary>
        /// 启动一次自动交互：动画状态解析、可视/排序/pose 覆盖、时长与 buff/事件广播。
        /// 旧漫游碰撞触发与行为权重驱动共用。durationOverrideSeconds &gt; 0 时覆盖绑定时长
        /// （例如睡觉行为需要比绑定动画更久的持续时间）。alwaysPublishCompletionEvent 用于
        /// 行为驱动路径——性格演化（文档 §2 家具交互规则）要求行为交互总是广播完成事件；
        /// 旧漫游路径保持原样，只在消耗到家具 buff 时广播。
        /// </summary>
        private void StartWanderInteractionFromRequest(
            PetContext context,
            FurnitureInteractionTarget target,
            PetPlayerInteractionRequest request,
            bool hasRequestData,
            float durationOverrideSeconds = 0f,
            bool alwaysPublishCompletionEvent = false)
        {
            _wanderInteractionActive = true;
            float durationSeconds = durationOverrideSeconds > 0f
                ? durationOverrideSeconds
                : target.InteractionDurationSeconds;
            _wanderInteractionRemaining = Mathf.Max(0.1f, durationSeconds);
            if (hasRequestData)
            {
                // 绑定命中：显式动画状态名优先（如 Interact_PlayGame）；缺省时用变体映射兜底（如 "devil sleep" → Interact_DevilSleep）。
                _wanderInteractionAnimatorStateName = !string.IsNullOrWhiteSpace(request.AnimatorStateNameOverride)
                    ? request.AnimatorStateNameOverride
                    : !string.IsNullOrWhiteSpace(request.AnimationVariant)
                        ? ResolvePlayerInteractionStateName(request.AnimationVariant)
                        : ResolveFurnitureInteractionStateName(target.InteractionType);
            }
            else
            {
                // _placedFurniture 命中：request 是 default，按家具类型映射（已按宠物区分天使/恶魔状态）。
                _wanderInteractionAnimatorStateName = ResolveFurnitureInteractionStateName(target.InteractionType);
            }

            SetWanderVelocity(Vector2.zero);

            // 与手动路径一致：应用可视/排序/特殊可视覆盖，并把宠物摆到固定交互点，
            // 而不是在卡住的原地播动画。_placedFurniture 路径 request 为 default，
            // 覆盖/pose 均以 default 请求应用（缩放已在 ApplyAutoInteractionPose 内兜底为当前缩放）。
            ApplyInteractionVisualOverride(request);
            ApplyInteractionSortingOverride(request);
            ApplySpecialInteractionVisualOverride(request);

            // 绑定路径尊重绑定的 pose 意图：门边/书柜等 UsePetPoseOverride=false 的绑定，
            // 手动 F 键/点击不会移动或缩放宠物（保持当前缩放 0.5，只在原地播动画），自动路径
            // 也必须一致；否则自动触发会把宠物强制缩到绑定缩放（门边/书柜为 1.0），出现
            // "自动播放动画的大小 != 手动触发的大小"。_placedFurniture 兜底路径 request 为
            // default（UsePetPoseOverride=false），但它是唯一命中，仍需 pose 到家具交互点
            // （缩放兜底为当前缩放），所以用 !hasRequestData 放行。
            if (!hasRequestData || request.UsePetPoseOverride)
            {
                ApplyAutoInteractionPose(request, target.InteractionPoint);
            }

            Debug.Log(
                $"[PetInteraction] Auto wander interaction started target='{target.FurnitureId}' " +
                $"state='{_wanderInteractionAnimatorStateName}' duration={_wanderInteractionRemaining:F2}");

            // 与 InteractingState.Enter 一致：应用环境加成（仅当通过 _placedFurniture 命中时）。
            EnvironmentalBuff buff = default;
            bool buffConsumed = context.FurnitureService is not null &&
                                context.FurnitureService.TryConsumeInteractionBuff(target.FurnitureId, out buff);
            if (buffConsumed)
            {
                StatTickService.ApplyEnvironmentalBuff(context.RuntimeData, buff.MoodDelta, buff.EnergyDelta);
                context.RuntimeData.LastInteractionFurnitureId = target.FurnitureId;
                context.RuntimeData.LastInteractionSummary =
                    $"{target.InteractionType.ToDisplayLabel()} / {target.Category} (Mood {FormatSigned(buff.MoodDelta)}, Energy {FormatSigned(buff.EnergyDelta)})";
            }

            if (buffConsumed || alwaysPublishCompletionEvent)
            {
                context.EventBus?.Publish(new PetInteractionCompletedEvent(
                    context.RuntimeData.PetId,
                    target.FurnitureId,
                    target.Category,
                    target.InteractionType));
            }
        }

        private bool TryFindNearbyFurniture(PetContext context, out FurnitureInteractionTarget target)
        {
            target = default;
            if (context.FurnitureService is null)
            {
                return false;
            }

            IReadOnlyList<GeminiLab.Modules.Furniture.Furniture> placed = context.FurnitureService.GetPlacedFurniture();
            if (placed is null || placed.Count == 0)
            {
                return false;
            }

            Vector2 origin = GetCurrentWorldPosition();
            GeminiLab.Modules.Furniture.Furniture? best = null;
            float bestDistance = WanderInteractionRadius;
            for (int i = 0; i < placed.Count; i++)
            {
                GeminiLab.Modules.Furniture.Furniture furniture = placed[i];
                if (furniture is null || !furniture.Anchor.IsAvailable)
                {
                    continue;
                }

                float distance = Vector2.Distance(origin, furniture.Anchor.WorldPosition);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = furniture;
                }
            }

            if (best is null)
            {
                return false;
            }

            FurnitureDefinitionSO definition = best.Definition;
            target = new FurnitureInteractionTarget(
                best.InstanceId,
                definition.Id,
                definition.Category,
                definition.InteractionType,
                definition.InteractionDurationSeconds,
                best.Anchor.WorldPosition,
                -bestDistance);
            return true;
        }

        /// <summary>
        /// 从宠物自身的 PetPlayerFurnitureInteractionController 绑定中寻找最近的可交互家具。
        /// 手动 F 键/点击路径用同一套绑定（硬编码交互点 + 显式动画状态名），不依赖 _placedFurniture；
        /// 公寓场景 _placedFurniture 为空，只有这条路径能命中。
        /// </summary>
        private bool TryFindNearbyAutoBinding(out FurnitureInteractionTarget target, out PetPlayerInteractionRequest request)
        {
            target = default;
            request = default;

            if (!TryGetComponent(out PetPlayerFurnitureInteractionController interactionController) ||
                !interactionController.TryGetAutoInteractionCandidate(out AutoInteractionCandidate candidate))
            {
                return false;
            }

            request = candidate.Request;
            target = new FurnitureInteractionTarget(
                request.TargetName,
                request.TargetName,
                request.Category,
                request.InteractionType,
                request.InteractionDurationSeconds,
                candidate.InteractionPoint,
                0f);
            return true;
        }

        /// <summary>
        /// 自动交互的 pose：复用 <see cref="ApplyInteractionPoseOverride"/>，把宠物摆到
        /// 绑定解析出的固定交互点（target.InteractionPoint，即绑定硬编码的 fallbackWorldPoint）。
        /// 手动路径靠玩家把宠物走到点位；自动路径宠物可能卡在家具边缘，必须主动摆到固定点。
        /// 仅当调用方确认该交互需要 pose 时才调用（UsePetPoseOverride=false 的门边/书柜不走这里，
        /// 与手动行为一致，保持当前缩放）。
        /// </summary>
        private void ApplyAutoInteractionPose(PetPlayerInteractionRequest request, Vector2 interactionPoint)
        {
            // 绑定路径的 request 携带绑定缩放（0.39~0.5）；_placedFurniture 兜底路径的 request 是 default，
            // PetInteractionScale 为 (0,0,0)，直接套用会让宠物缩到看不见，这里用当前缩放兜底。
            Vector3 scale = request.PetInteractionScale.sqrMagnitude > 0.0001f
                ? request.PetInteractionScale
                : transform.localScale;

            PetPlayerInteractionRequest poseRequest = new PetPlayerInteractionRequest(
                request.TargetName,
                request.Category,
                request.InteractionType,
                request.AnimationVariant,
                request.AnimatorStateNameOverride,
                request.HideTargetWhileInteracting,
                request.VisualHideTarget,
                visualPoseTarget: null,
                request.AdditionalVisualHideTargets,
                request.UseTargetSortingWhileInteracting,
                request.VisualSortingTarget,
                request.SortingOrderOffsetWhileInteracting,
                usePetPoseOverride: true,
                useTargetPositionForPetPose: false,
                petInteractionLocalOffset: Vector2.zero,
                petInteractionWorldPoint: interactionPoint,
                petInteractionScale: scale,
                request.InteractionDurationSeconds);
            ApplyInteractionPoseOverride(poseRequest);
        }

        private static string FormatSigned(float value)
        {
            return value >= 0f ? $"+{value:0.#}" : value.ToString("0.#");
        }

        private void SetWanderVelocity(Vector2 velocity)
        {
            if (_apartmentMovement != null && _apartmentMovement.IsReady)
            {
                if (velocity == Vector2.zero) _apartmentMovement.Stop();
                else _apartmentMovement.SetManualVelocity(velocity);
                return;
            }
            if (_rigidbody2D != null)
            {
                _rigidbody2D.velocity = velocity;
            }
        }

        private void TickExternalMovementLock(PetContext context)
        {
            StopExternalMovement();
            _hasPlayerAnimationDirection = false;
            SetPlayerControlledState(context, IdleState.StateName);
        }

        private void StopExternalMovement()
        {
            SetWanderVelocity(Vector2.zero);
            if (_rigidbody2D != null)
            {
                _rigidbody2D.angularVelocity = 0f;
            }

            RandomWander? wander = GetComponent<RandomWander>();
            if (wander != null && wander.IsMoving)
            {
                wander.NotifyArrived();
            }

            if (_context?.RuntimeData is not PetRuntimeData runtimeData)
            {
                return;
            }

            Vector2 currentPosition = GetCurrentWorldPosition();
            runtimeData.Position = currentPosition;
            runtimeData.TargetPosition = currentPosition;
            runtimeData.TargetReached = true;
        }

        private Vector2 ResolveWanderArrivedPosition(RandomWander wander)
        {
            if (!wander.HorizontalOnly)
            {
                return wander.TargetPosition;
            }

            Vector2 position = new(wander.TargetPosition.x, wander.HorizontalBaselineY);
            position.y = ResolveGroundY(position.x, position.y);
            return position;
        }

        public bool TryStartPlayerInteraction(PetPlayerInteractionRequest request)
        {
            if (_context is null)
            {
                LogDevilFTraceWarning(
                    $"TryStartPlayerInteraction rejected: context null, target='{request.TargetName}', variant='{request.AnimationVariant}', state='{request.AnimatorStateNameOverride}'");
                Debug.LogWarning("[PetInteraction] TryStartPlayerInteraction failed: _context is null.");
                return false;
            }

            if (!IsPlayerControlled())
            {
                LogDevilFTraceWarning(
                    $"TryStartPlayerInteraction rejected: not player controlled, target='{request.TargetName}', variant='{request.AnimationVariant}', state='{request.AnimatorStateNameOverride}'");
                Debug.LogWarning("[PetInteraction] TryStartPlayerInteraction failed: IsPlayerControlled is false.");
                return false;
            }

            if (_context.RuntimeData.IsPlayerInteractionActive)
            {
                LogDevilFTraceWarning(
                    $"TryStartPlayerInteraction rejected: already interacting, target='{request.TargetName}', variant='{request.AnimationVariant}', state='{request.AnimatorStateNameOverride}'");
                Debug.LogWarning("[PetInteraction] TryStartPlayerInteraction failed: already interacting.");
                return false;
            }

            _context.RuntimeData.IsPlayerInteractionActive = true;
            _context.RuntimeData.PlayerInteractionRemainingSeconds = Mathf.Max(0.1f, request.InteractionDurationSeconds);
            _context.RuntimeData.PlayerInteractionAnimationVariant = request.AnimationVariant;
            _context.RuntimeData.PlayerInteractionAnimatorStateName = request.AnimatorStateNameOverride;
            _context.RuntimeData.PlayerInteractionLabel = request.TargetName;
            _context.RuntimeData.TargetFurnitureId = request.TargetName;
            _context.RuntimeData.TargetFurnitureCategory = request.Category;
            _context.RuntimeData.TargetFurnitureInteractionType = request.InteractionType;
            _context.RuntimeData.TargetInteractionDurationSeconds = Mathf.Max(0.1f, request.InteractionDurationSeconds);
            _context.RuntimeData.TargetReached = true;
            _context.RuntimeData.ActivePath.Clear();
            _context.RuntimeData.PathIndex = 0;
            ApplyInteractionVisualOverride(request);
            ApplyInteractionSortingOverride(request);
            ApplyInteractionPoseOverride(request);
            ApplySpecialInteractionVisualOverride(request);
            LogDevilFTrace(
                $"TryStartPlayerInteraction accepted: target='{request.TargetName}', variant='{request.AnimationVariant}', " +
                $"state='{request.AnimatorStateNameOverride}', duration={_context.RuntimeData.PlayerInteractionRemainingSeconds:F2}");
            Debug.Log($"[PetInteraction] Triggered player interaction target='{request.TargetName}' variant='{request.AnimationVariant}'.");
            return true;
        }

        private bool TickPlayerInteraction(PetContext context, float deltaTime)
        {
            if (!context.RuntimeData.IsPlayerInteractionActive)
            {
                return false;
            }

            SetPlayerControlledState(context, InteractingState.StateName);
            Debug.Log($"[PetInteraction] TickPlayerInteraction: state=Interacting, remainingSeconds={context.RuntimeData.PlayerInteractionRemainingSeconds:F2}, animState='{context.RuntimeData.PlayerInteractionAnimatorStateName}', variant='{context.RuntimeData.PlayerInteractionAnimationVariant}'");
            context.Advance(deltaTime);
            _tickService?.Tick(context, deltaTime);
            context.RuntimeData.WorkRequested = false;
            context.RuntimeData.ActivePath.Clear();
            context.RuntimeData.PathIndex = 0;
            context.RuntimeData.TargetReached = true;
            context.RuntimeData.PlayerInteractionRemainingSeconds -= deltaTime;

            if (context.RuntimeData.PlayerInteractionRemainingSeconds > 0f)
            {
                return true;
            }

            context.RuntimeData.IsPlayerInteractionActive = false;
            context.RuntimeData.PlayerInteractionRemainingSeconds = 0f;
            context.RuntimeData.LastInteractionFurnitureId = context.RuntimeData.TargetFurnitureId;
            context.RuntimeData.LastInteractionSummary =
                $"玩家交互 / {context.RuntimeData.PlayerInteractionLabel} / {context.RuntimeData.PlayerInteractionAnimationVariant}";
            context.RuntimeData.PlayerInteractionAnimatorStateName = string.Empty;
            context.RuntimeData.PlayerInteractionAnimationVariant = string.Empty;
            context.RuntimeData.PlayerInteractionLabel = string.Empty;
            RestoreHiddenInteractionVisuals();
            RestoreSleepInteractionVisual();
            RestoreInteractionSorting();
            RestoreInteractionPose();
            return true;
        }

        private static void ResetWorkRuntime(PetContext context)
        {
            context.RuntimeData.ActiveWorkTraceId = string.Empty;
            context.RuntimeData.ActiveWorkMessage = string.Empty;
            context.RuntimeData.RequiredWorkTargetType = PetWorkTargetType.Any;
            context.RuntimeData.IsAtRequiredWorkTarget = false;
            context.RuntimeData.TargetFurnitureId = string.Empty;
            context.RuntimeData.TargetFurnitureCategory = FurnitureCategory.Unknown;
            context.RuntimeData.TargetFurnitureInteractionType = FurnitureInteractionType.Unknown;
            context.RuntimeData.TargetInteractionDurationSeconds = 1f;
            context.RuntimeData.TargetReached = false;
            context.RuntimeData.ActivePath.Clear();
        }

        private static void ResetPlayerControlledRuntime(PetContext context)
        {
            context.RuntimeData.WorkRequested = false;
            ResetWorkRuntime(context);
            if (!context.RuntimeData.IsPlayerInteractionActive)
            {
                context.RuntimeData.TargetFurnitureId = string.Empty;
                context.RuntimeData.TargetFurnitureCategory = FurnitureCategory.Unknown;
                context.RuntimeData.TargetFurnitureInteractionType = FurnitureInteractionType.Unknown;
                context.RuntimeData.TargetInteractionDurationSeconds = 1f;
                context.RuntimeData.TargetReached = true;
                context.RuntimeData.ActivePath.Clear();
                context.RuntimeData.PathIndex = 0;
            }
        }

        private void CancelPlayerInteraction(PetContext context)
        {
            if (!context.RuntimeData.IsPlayerInteractionActive)
            {
                return;
            }

            context.RuntimeData.IsPlayerInteractionActive = false;
            context.RuntimeData.PlayerInteractionRemainingSeconds = 0f;
            context.RuntimeData.PlayerInteractionAnimatorStateName = string.Empty;
            context.RuntimeData.PlayerInteractionAnimationVariant = string.Empty;
            context.RuntimeData.PlayerInteractionLabel = string.Empty;
            RestoreHiddenInteractionVisuals();
            RestoreSleepInteractionVisual();
            RestoreInteractionSorting();
            RestoreInteractionPose();
        }

        private static void SetPlayerControlledState(PetContext context, string stateName)
        {
            if (string.Equals(context.RuntimeData.CurrentState, stateName, System.StringComparison.Ordinal))
            {
                return;
            }

            context.EnterState(stateName);
        }

        private void ApplyInteractionVisualOverride(PetPlayerInteractionRequest request)
        {
            RestoreHiddenInteractionVisuals();
            if (!request.HideTargetWhileInteracting || request.VisualHideTarget == null)
            {
                return;
            }

            HideInteractionRenderers(request.VisualHideTarget);
            for (int i = 0; i < request.AdditionalVisualHideTargets.Length; i++)
            {
                GameObject extraTarget = request.AdditionalVisualHideTargets[i];
                if (extraTarget == null)
                {
                    continue;
                }

                HideInteractionRenderers(extraTarget);
            }
        }

        private void RestoreHiddenInteractionVisuals()
        {
            for (int i = 0; i < _hiddenInteractionRenderers.Count; i++)
            {
                SpriteRenderer? renderer = _hiddenInteractionRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.enabled = _hiddenInteractionRendererStates[i];
            }

            _hiddenInteractionRenderers.Clear();
            _hiddenInteractionRendererStates.Clear();
        }

        private void HideInteractionRenderers(GameObject target)
        {
            SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                _hiddenInteractionRenderers.Add(renderer);
                _hiddenInteractionRendererStates.Add(renderer.enabled);
                renderer.enabled = false;
            }
        }

        private void ApplySpecialInteractionVisualOverride(PetPlayerInteractionRequest request)
        {
            RestoreSleepInteractionVisual();
            if (!UsesDetachedInteractionVisual(request))
            {
                return;
            }

            GameObject? poseTarget = request.VisualPoseTarget ?? request.VisualHideTarget;
            if (poseTarget == null)
            {
                LogDevilFTrace(
                    $"PoseTrace branch='detached' skipped='poseTarget null' state='{ResolveInteractionAnimatorStateName(request)}' " +
                    $"localOffset={FormatVector2(request.PetInteractionLocalOffset)} worldPoint={FormatVector2(request.PetInteractionWorldPoint)}");
                return;
            }

            GameObject sortingTarget = request.VisualSortingTarget ?? poseTarget;

            EnsureSleepInteractionVisual();
            if (_sleepInteractionVisualTransform == null ||
                _sleepInteractionVisualAnimator == null ||
                _sleepInteractionVisualSpriteRenderer == null)
            {
                return;
            }

            Vector2 posePoint = request.UseTargetPositionForPetPose
                ? ResolveInteractionPosePoint(poseTarget.transform, request.PetInteractionLocalOffset)
                : request.PetInteractionWorldPoint;

            LogDevilFTrace(
                $"PoseTrace branch='detached' state='{ResolveInteractionAnimatorStateName(request)}' " +
                $"poseTarget='{DescribeObject(poseTarget)}' sortingTarget='{DescribeObject(sortingTarget)}' " +
                $"positionSource='{(request.UseTargetPositionForPetPose ? "target+offset" : "worldPoint")}' " +
                $"localOffset={FormatVector2(request.PetInteractionLocalOffset)} worldPoint={FormatVector2(request.PetInteractionWorldPoint)} " +
                $"finalPosePoint={FormatVector2(posePoint)} scale={FormatVector3(request.PetInteractionScale)}");

            _sleepInteractionVisualTransform.position = new Vector3(
                posePoint.x,
                posePoint.y,
                transform.position.z);
            _sleepInteractionVisualTransform.localScale = request.PetInteractionScale;
            ApplySleepInteractionVisualSorting(sortingTarget, request.SortingOrderOffsetWhileInteracting);
            _sleepInteractionVisualSpriteRenderer.enabled = true;
            _sleepInteractionVisualAnimator.Play(
                ResolveInteractionAnimatorStateName(request),
                0,
                0f);

            if (_spriteRenderer != null)
            {
                _storedPetSpriteVisible = _spriteRenderer.enabled;
                _hasStoredPetSpriteVisible = true;
                _spriteRenderer.enabled = false;
            }
        }

        private void RestoreSleepInteractionVisual()
        {
            if (_sleepInteractionVisualSpriteRenderer != null)
            {
                _sleepInteractionVisualSpriteRenderer.enabled = false;
            }

            if (_spriteRenderer != null && _hasStoredPetSpriteVisible)
            {
                _spriteRenderer.enabled = _storedPetSpriteVisible;
            }

            _hasStoredPetSpriteVisible = false;
        }

        private void EnsureSleepInteractionVisual()
        {
            if (_sleepInteractionVisualObject != null &&
                _sleepInteractionVisualTransform != null &&
                _sleepInteractionVisualAnimator != null &&
                _sleepInteractionVisualSpriteRenderer != null)
            {
                return;
            }

            _sleepInteractionVisualObject = new GameObject("SleepInteractionVisual")
            {
                layer = gameObject.layer
            };

            _sleepInteractionVisualTransform = _sleepInteractionVisualObject.transform;
            Transform parent = transform.parent != null ? transform.parent : transform;
            _sleepInteractionVisualTransform.SetParent(parent, false);
            _sleepInteractionVisualTransform.localPosition = Vector3.zero;
            _sleepInteractionVisualTransform.localRotation = Quaternion.identity;
            _sleepInteractionVisualTransform.localScale = Vector3.one;

            _sleepInteractionVisualSpriteRenderer = _sleepInteractionVisualObject.AddComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _sleepInteractionVisualSpriteRenderer.sharedMaterial = _spriteRenderer.sharedMaterial;
                _sleepInteractionVisualSpriteRenderer.color = _spriteRenderer.color;
                _sleepInteractionVisualSpriteRenderer.sortingLayerID = _spriteRenderer.sortingLayerID;
                _sleepInteractionVisualSpriteRenderer.sortingOrder = _spriteRenderer.sortingOrder;
                _sleepInteractionVisualSpriteRenderer.sprite = _spriteRenderer.sprite;
            }

            _sleepInteractionVisualSpriteRenderer.enabled = false;

            _sleepInteractionVisualAnimator = _sleepInteractionVisualObject.AddComponent<Animator>();
            if (_movementController != null)
            {
                _sleepInteractionVisualAnimator.runtimeAnimatorController = _movementController;
            }

            _sleepInteractionVisualAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        private void ApplySleepInteractionVisualSorting(GameObject sortingTarget, int sortingOrderOffset)
        {
            if (_sleepInteractionVisualSpriteRenderer == null)
            {
                return;
            }

            int resolvedOffset = sortingOrderOffset != 0 ? sortingOrderOffset : 1;
            if (TryResolveSortingReference(sortingTarget, out int sortingLayerId, out int sortingOrder))
            {
                _sleepInteractionVisualSpriteRenderer.sortingLayerID = sortingLayerId;
                _sleepInteractionVisualSpriteRenderer.sortingOrder = sortingOrder + resolvedOffset;
            }
        }

        private void ApplyInteractionPoseOverride(PetPlayerInteractionRequest request)
        {
            RestoreInteractionPose();
            if (!request.UsePetPoseOverride)
            {
                LogDevilFTrace(
                    $"PoseTrace branch='main' skipped='UsePetPoseOverride=false' state='{ResolveInteractionAnimatorStateName(request)}'");
                return;
            }

            if (UsesDetachedInteractionVisual(request))
            {
                LogDevilFTrace(
                    $"PoseTrace branch='main' skipped='detached visual branch' state='{ResolveInteractionAnimatorStateName(request)}'");
                return;
            }

            _hasStoredInteractionPose = true;
            _storedInteractionPosition = GetCurrentWorldPosition();
            _storedInteractionScale = transform.localScale;
            Vector2 posePoint = request.PetInteractionWorldPoint;
            GameObject? poseTarget = request.VisualPoseTarget ?? request.VisualHideTarget;
            if (request.UseTargetPositionForPetPose && poseTarget != null)
            {
                posePoint = ResolveInteractionPosePoint(poseTarget.transform, request.PetInteractionLocalOffset);
            }

            posePoint = ClampToMovementBounds(posePoint);
            LogDevilFTrace(
                $"PoseTrace branch='main' state='{ResolveInteractionAnimatorStateName(request)}' " +
                $"poseTarget='{DescribeObject(poseTarget)}' positionSource='{(request.UseTargetPositionForPetPose && poseTarget != null ? "target+offset" : "worldPoint")}' " +
                $"localOffset={FormatVector2(request.PetInteractionLocalOffset)} worldPoint={FormatVector2(request.PetInteractionWorldPoint)} " +
                $"finalPosePoint={FormatVector2(posePoint)} storedPosition={FormatVector2(_storedInteractionPosition)} " +
                $"scale={FormatVector3(request.PetInteractionScale)}");
            ApplyInteractionPhysicsOverride();
            _hasInteractionPoseRuntimeOverride = true;
            if (_context != null)
            {
                _context.RuntimeData.Position = posePoint;
                _context.RuntimeData.TargetPosition = posePoint;
            }

            ApplyRuntimePosition(posePoint);
            transform.localScale = request.PetInteractionScale;
        }

        private static Vector2 ResolveInteractionPosePoint(Transform targetTransform, Vector2 localOffset)
        {
            Transform[] transforms = targetTransform.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (!string.Equals(candidate.name, SleepPoseAnchorName, System.StringComparison.Ordinal))
                {
                    continue;
                }

                return (Vector2)candidate.position + localOffset;
            }

            return (Vector2)targetTransform.position + localOffset;
        }

        private void RestoreInteractionPose()
        {
            if (!_hasStoredInteractionPose)
            {
                _hasInteractionPoseRuntimeOverride = false;
                RestoreInteractionPhysicsOverride();
                return;
            }

            Vector2 restoredPosition = ClampToMovementBounds(_storedInteractionPosition);
            if (_context != null)
            {
                _context.RuntimeData.Position = restoredPosition;
                _context.RuntimeData.TargetPosition = restoredPosition;
            }

            ApplyRuntimePosition(restoredPosition);
            transform.localScale = _storedInteractionScale;
            _hasStoredInteractionPose = false;
            _hasInteractionPoseRuntimeOverride = false;
            RestoreInteractionPhysicsOverride();
        }

        private void ApplyInteractionSortingOverride(PetPlayerInteractionRequest request)
        {
            RestoreInteractionSorting();
            if (_spriteRenderer == null || !request.UseTargetSortingWhileInteracting || request.VisualSortingTarget == null)
            {
                return;
            }

            _hasStoredInteractionSorting = true;
            _storedInteractionSortingLayerId = _spriteRenderer.sortingLayerID;
            _storedInteractionSortingOrder = _spriteRenderer.sortingOrder;

            if (TryResolveSortingReference(
                    request.VisualSortingTarget,
                    out int sortingLayerId,
                    out int sortingOrder))
            {
                _spriteRenderer.sortingLayerID = sortingLayerId;
                _spriteRenderer.sortingOrder = sortingOrder + request.SortingOrderOffsetWhileInteracting;
            }
        }

        private void RestoreInteractionSorting()
        {
            if (!_hasStoredInteractionSorting || _spriteRenderer == null)
            {
                return;
            }

            _spriteRenderer.sortingLayerID = _storedInteractionSortingLayerId;
            _spriteRenderer.sortingOrder = _storedInteractionSortingOrder;
            _hasStoredInteractionSorting = false;
        }

        private void UpdateMovementAnimation()
        {
            if (_animator == null)
            {
                return;
            }

            string? currentState = _context?.RuntimeData.CurrentState;
            if (currentState == SleepingState.StateName)
            {
                PlayForcedAnimatorState(SleepStateName);
                _animator.SetBool(IsMovingHash, false);
                _animator.speed = 1f;
                return;
            }

            if (currentState == InteractingState.StateName || currentState == WorkingState.StateName)
            {
                string resolvedInteractionState = ResolveInteractionStateName();
                PlayForcedAnimatorState(resolvedInteractionState);
                _animator.SetBool(IsMovingHash, false);
                _animator.speed = 1f;
                return;
            }

            Vector2 currentPosition = GetCurrentWorldPosition();
            Vector2 delta = currentPosition - _lastAnimationPosition;
            _lastAnimationPosition = currentPosition;

            bool isMoving = string.Equals(currentState, MovingState.StateName, System.StringComparison.Ordinal);
            bool hasDelta = delta.sqrMagnitude > DirectionEpsilonSqr;

            if (!isMoving)
            {
                _animationDirectionDebouncer.Reset();
                PlayForcedAnimatorState(ResolveIdleStateName(_lastMoveDirection));
            }
            else
            {
                _lastForcedAnimatorStateName = string.Empty;
            }

            if (IsPlayerControlled() && _hasPlayerAnimationDirection)
            {
                // 玩家输入的方向即时生效，不参与去抖。
                _lastMoveDirection = _playerAnimationDirection;
                _animationDirectionDebouncer.Reset();
            }
            else if (isMoving && _context is not null)
            {
                // 移动中优先使用目标方向：目标是稳定点，而逐帧实际位移在撞上家具后
                // 会沿表面滑动/抖动。两种来源都经过去抖，避免 MoveDir 高频翻转
                // 让动画在 Move_Front / Move_Back / Move_Side 之间乱切换。
                Vector2 targetDelta = _context.RuntimeData.TargetPosition - currentPosition;
                // 绕家具时朝向当前路径段，不能一直面向最终家具位置。
                if (_apartmentMovement != null && _apartmentMovement.IsReady)
                    targetDelta = _apartmentMovement.SteeringVelocity;
                if (targetDelta.sqrMagnitude > DirectionEpsilonSqr)
                {
                    _lastMoveDirection = _animationDirectionDebouncer.Step(targetDelta.normalized, _lastMoveDirection);
                }
                else if (hasDelta && delta.sqrMagnitude > MinDirectionDeltaSqr)
                {
                    _lastMoveDirection = _animationDirectionDebouncer.Step(delta.normalized, _lastMoveDirection);
                }
            }
            else if (hasDelta && delta.sqrMagnitude > MinDirectionDeltaSqr)
            {
                _lastMoveDirection = _animationDirectionDebouncer.Step(delta.normalized, _lastMoveDirection);
            }

            _animator.SetBool(IsMovingHash, isMoving);
            _animator.SetFloat(MoveXHash, _lastMoveDirection.x);
            _animator.SetFloat(MoveYHash, _lastMoveDirection.y);
            int moveDir = ResolveMoveDirection(_lastMoveDirection);
            _animator.SetInteger(MoveDirHash, moveDir);
            _animator.speed = 1f;

            UpdateSideMirror(moveDir, _lastMoveDirection);
        }

        private static Vector2 ResolvePlayerAnimationDirection(Vector2 rawInput, Vector2 previousDirection)
        {
            bool hasHorizontal = Mathf.Abs(rawInput.x) > 0.0001f;
            bool hasVertical = Mathf.Abs(rawInput.y) > 0.0001f;

            if (hasHorizontal && hasVertical)
            {
                if (Mathf.Abs(previousDirection.x) > Mathf.Abs(previousDirection.y))
                {
                    return new Vector2(Mathf.Sign(rawInput.x), 0f);
                }

                if (Mathf.Abs(previousDirection.y) > 0.0001f)
                {
                    return new Vector2(0f, Mathf.Sign(rawInput.y));
                }

                return new Vector2(Mathf.Sign(rawInput.x), 0f);
            }

            if (hasHorizontal)
            {
                return new Vector2(Mathf.Sign(rawInput.x), 0f);
            }

            if (hasVertical)
            {
                return new Vector2(0f, Mathf.Sign(rawInput.y));
            }

            return previousDirection;
        }

        private static int ResolveMoveDirection(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                return 2; // Side
            }

            return direction.y >= 0f ? 1 : 0; // Back / Front
        }

        private void UpdateSideMirror(int moveDir, Vector2 direction)
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            // Only side movement uses horizontal mirroring.
            if (moveDir != 2 || Mathf.Abs(direction.x) <= 0.0001f)
            {
                _spriteRenderer.flipX = false;
                return;
            }

            bool movingRight = direction.x > 0f;
            _spriteRenderer.flipX = _sideFramesFaceLeft ? movingRight : !movingRight;
        }

        private string ResolveInteractionStateName()
        {
            if (_wanderInteractionActive && !string.IsNullOrEmpty(_wanderInteractionAnimatorStateName))
            {
                return _wanderInteractionAnimatorStateName;
            }

            if (_context?.RuntimeData.IsPlayerInteractionActive == true)
            {
                if (!string.IsNullOrWhiteSpace(_context.RuntimeData.PlayerInteractionAnimatorStateName))
                {
                    return _context.RuntimeData.PlayerInteractionAnimatorStateName;
                }

                return ResolvePlayerInteractionStateName(_context.RuntimeData.PlayerInteractionAnimationVariant);
            }

            if (_context?.RuntimeData.RequiredWorkTargetType == PetWorkTargetType.WorkDesk ||
                _context?.RuntimeData.TargetFurnitureInteractionType == FurnitureInteractionType.WorkFocus ||
                _context?.RuntimeData.TargetFurnitureCategory == FurnitureCategory.WorkDesk)
            {
                return InteractReadStateName;
            }

            return ResolveFurnitureInteractionStateName(
                _context?.RuntimeData.TargetFurnitureInteractionType ?? FurnitureInteractionType.Unknown);
        }

        private string ResolveFurnitureInteractionStateName(FurnitureInteractionType interactionType)
        {
            if (_petId == PetId.Devil)
            {
                // 恶魔控制器只有 Interact_DevilSleep/Draw/LookAround/PlayGame，
                // 没有天使的 Interact_BesideDoor/Read/PlayingMusic。按宠物映射到恶魔实际拥有的状态，
                // 避免 PlayForcedAnimatorState 因状态缺失报警告并播不出动画。
                return interactionType switch
                {
                    FurnitureInteractionType.PlayHarp => InteractPlayGameStateName,
                    FurnitureInteractionType.PlayGuitar => InteractDrawStateName,
                    FurnitureInteractionType.PaintAtEasel => InteractDrawStateName,
                    FurnitureInteractionType.ViewPhotoBoard => InteractDrawStateName,
                    FurnitureInteractionType.LeisureEngage => InteractDrawStateName,
                    FurnitureInteractionType.InspectBookshelf => InteractLookAroundStateName,
                    FurnitureInteractionType.InspectMirror => InteractLookAroundStateName,
                    FurnitureInteractionType.InspectNightstand => InteractLookAroundStateName,
                    FurnitureInteractionType.ObservePlant => InteractLookAroundStateName,
                    FurnitureInteractionType.ObserveWindow => InteractLookAroundStateName,
                    FurnitureInteractionType.InspectToy => InteractLookAroundStateName,
                    FurnitureInteractionType.ArrangePillow => InteractLookAroundStateName,
                    FurnitureInteractionType.InspectPapers => InteractLookAroundStateName,
                    FurnitureInteractionType.ListenToAudio => InteractLookAroundStateName,
                    FurnitureInteractionType.OrganizeStorage => InteractLookAroundStateName,
                    FurnitureInteractionType.DecorInspect => InteractLookAroundStateName,
                    _ => MoveFrontStateName
                };
            }

            return interactionType switch
            {
                FurnitureInteractionType.PlayHarp => InteractPlayingMusicStateName,
                FurnitureInteractionType.PlayGuitar => InteractReadStateName,
                FurnitureInteractionType.PaintAtEasel => InteractReadStateName,
                FurnitureInteractionType.ViewPhotoBoard => InteractReadStateName,
                FurnitureInteractionType.LeisureEngage => InteractReadStateName,
                FurnitureInteractionType.InspectBookshelf => InteractBesideDoorStateName,
                FurnitureInteractionType.InspectMirror => InteractBesideDoorStateName,
                FurnitureInteractionType.InspectNightstand => InteractBesideDoorStateName,
                FurnitureInteractionType.ObservePlant => InteractBesideDoorStateName,
                FurnitureInteractionType.ObserveWindow => InteractBesideDoorStateName,
                FurnitureInteractionType.InspectToy => InteractBesideDoorStateName,
                FurnitureInteractionType.ArrangePillow => InteractBesideDoorStateName,
                FurnitureInteractionType.InspectPapers => InteractBesideDoorStateName,
                FurnitureInteractionType.ListenToAudio => InteractBesideDoorStateName,
                FurnitureInteractionType.OrganizeStorage => InteractBesideDoorStateName,
                FurnitureInteractionType.DecorInspect => InteractBesideDoorStateName,
                FurnitureInteractionType.RestOnRug => MoveFrontStateName,
                FurnitureInteractionType.SitOnSeat => MoveFrontStateName,
                FurnitureInteractionType.LoungeOnSofa => MoveFrontStateName,
                FurnitureInteractionType.SleepInBed => MoveFrontStateName,
                FurnitureInteractionType.SleepRest => MoveFrontStateName,
                _ => MoveFrontStateName
            };
        }

        private static string ResolvePlayerInteractionStateName(string variant)
        {
            return variant switch
            {
                "beside door" => InteractBesideDoorStateName,
                "flower" => InteractFlowerStateName,
                "playing music" => InteractPlayingMusicStateName,
                "read" => InteractReadStateName,
                "write" => InteractWriteStateName,
                "sleep" => SleepStateName,
                "look around" => InteractLookAroundStateName,
                "play game" => InteractPlayGameStateName,
                "draw" => InteractDrawStateName,
                "devil sleep" => InteractSleepStateName,
                _ => MoveFrontStateName
            };
        }

        private static string ResolveInteractionAnimatorStateName(PetPlayerInteractionRequest request)
        {
            return !string.IsNullOrWhiteSpace(request.AnimatorStateNameOverride)
                ? request.AnimatorStateNameOverride
                : ResolvePlayerInteractionStateName(request.AnimationVariant);
        }

        private bool UsesDetachedInteractionVisual(PetPlayerInteractionRequest request)
        {
            return _interactionVisualStrategy.UsesDetachedVisual(
                ResolveInteractionAnimatorStateName(request),
                _petId);
        }

        private static bool TryResolveSortingReference(GameObject target, out int sortingLayerId, out int sortingOrder)
        {
            if (target.TryGetComponent(out SortingGroup directSortingGroup))
            {
                sortingLayerId = directSortingGroup.sortingLayerID;
                sortingOrder = directSortingGroup.sortingOrder;
                return true;
            }

            if (target.TryGetComponent(out SpriteRenderer directSpriteRenderer))
            {
                sortingLayerId = directSpriteRenderer.sortingLayerID;
                sortingOrder = directSpriteRenderer.sortingOrder;
                return true;
            }

            SortingGroup? childSortingGroup = target.GetComponentInChildren<SortingGroup>(true);
            if (childSortingGroup != null)
            {
                sortingLayerId = childSortingGroup.sortingLayerID;
                sortingOrder = childSortingGroup.sortingOrder;
                return true;
            }

            SpriteRenderer? childSpriteRenderer = target.GetComponentInChildren<SpriteRenderer>(true);
            if (childSpriteRenderer != null)
            {
                sortingLayerId = childSpriteRenderer.sortingLayerID;
                sortingOrder = childSpriteRenderer.sortingOrder;
                return true;
            }

            sortingLayerId = 0;
            sortingOrder = 0;
            return false;
        }

        private static string ResolveIdleStateName(Vector2 direction)
        {
            int moveDir = ResolveMoveDirection(direction);
            return moveDir switch
            {
                1 => IdleBackStateName,
                2 => IdleSideStateName,
                _ => IdleFrontStateName
            };
        }

        private bool TryResolveAnimatorPlaybackStateName(
            string requestedStateName,
            out string resolvedStateName,
            out string fallbackStateName)
        {
            fallbackStateName = requestedStateName.StartsWith(
                BaseLayerStatePrefix,
                System.StringComparison.Ordinal)
                ? requestedStateName
                : $"{BaseLayerStatePrefix}{requestedStateName}";

            resolvedStateName = string.Empty;
            if (_animator == null)
            {
                return false;
            }

            if (_animator.HasState(0, Animator.StringToHash(requestedStateName)))
            {
                resolvedStateName = requestedStateName;
                return true;
            }

            if (!string.Equals(
                    fallbackStateName,
                    requestedStateName,
                    System.StringComparison.Ordinal) &&
                _animator.HasState(0, Animator.StringToHash(fallbackStateName)))
            {
                resolvedStateName = fallbackStateName;
                return true;
            }

            return false;
        }

        private bool ShouldTraceDevilInteraction()
        {
            return _petId == PetId.Devil;
        }

        private bool ShouldTraceDevilAnimatorState(string stateName)
        {
            return ShouldTraceDevilInteraction() &&
                   ((_context?.RuntimeData.IsPlayerInteractionActive ?? false) ||
                    stateName.StartsWith("Interact_", System.StringComparison.Ordinal));
        }

        private void LogDevilFTrace(string message)
        {
            if (!ShouldTraceDevilInteraction())
            {
                return;
            }

            Debug.Log($"{DevilFTracePrefix} {message}");
        }

        private void ApplyInteractionPhysicsOverride()
        {
            if (_hasInteractionPhysicsOverride)
            {
                return;
            }

            _hasInteractionPhysicsOverride = true;
            if (_rigidbody2D != null)
            {
                _storedInteractionRigidbodySimulated = _rigidbody2D.simulated;
                _rigidbody2D.velocity = Vector2.zero;
                _rigidbody2D.angularVelocity = 0f;
                _rigidbody2D.simulated = false;
            }

            if (_capsuleCollider2D != null)
            {
                _storedInteractionCapsuleColliderEnabled = _capsuleCollider2D.enabled;
                _capsuleCollider2D.enabled = false;
            }

            LogDevilFTrace(
                $"InteractionPhysicsOverride enabled: rigidbodySimulated={_storedInteractionRigidbodySimulated} " +
                $"capsuleEnabled={_storedInteractionCapsuleColliderEnabled}");
        }

        private void RestoreInteractionPhysicsOverride()
        {
            if (!_hasInteractionPhysicsOverride)
            {
                return;
            }

            if (_capsuleCollider2D != null)
            {
                _capsuleCollider2D.enabled = _storedInteractionCapsuleColliderEnabled;
            }

            if (_rigidbody2D != null)
            {
                _rigidbody2D.simulated = _storedInteractionRigidbodySimulated;
                _rigidbody2D.velocity = Vector2.zero;
                _rigidbody2D.angularVelocity = 0f;
            }

            _hasInteractionPhysicsOverride = false;
            LogDevilFTrace(
                $"InteractionPhysicsOverride restored: rigidbodySimulated={_storedInteractionRigidbodySimulated} " +
                $"capsuleEnabled={_storedInteractionCapsuleColliderEnabled}");
        }

        private void TraceDevilInteractionTickIfNeeded(string phase, ref int lastTraceFrame)
        {
            if (!ShouldTraceDevilInteraction() ||
                _context == null ||
                !_context.RuntimeData.IsPlayerInteractionActive)
            {
                return;
            }

            int currentFrame = Time.frameCount;
            if (currentFrame - lastTraceFrame < DevilPoseTickTraceFrameInterval)
            {
                return;
            }

            lastTraceFrame = currentFrame;
            TraceDevilInteractionTick(phase);
        }

        private void TraceDevilInteractionTick(string phase)
        {
            if (_context == null || !ShouldTraceDevilInteraction())
            {
                return;
            }

            PetRuntimeData runtimeData = _context.RuntimeData;
            string currentState = runtimeData.CurrentState;
            string interactionState = runtimeData.PlayerInteractionAnimatorStateName;
            string transformPosition = FormatVector3(transform.position);
            string rigidbodyPosition = _rigidbody2D != null
                ? FormatVector2(_rigidbody2D.position)
                : "null";
            string worldPosition = FormatVector2(GetCurrentWorldPosition());
            string runtimePosition = FormatVector2(runtimeData.Position);
            string targetPosition = FormatVector2(runtimeData.TargetPosition);
            bool petSpriteVisible = _spriteRenderer != null && _spriteRenderer.enabled;
            bool detachedVisible = _sleepInteractionVisualSpriteRenderer != null &&
                                   _sleepInteractionVisualSpriteRenderer.enabled;
            string detachedPosition = _sleepInteractionVisualTransform != null
                ? FormatVector3(_sleepInteractionVisualTransform.position)
                : "null";
            bool detachedObjectActive = _sleepInteractionVisualObject != null &&
                                        _sleepInteractionVisualObject.activeInHierarchy;
            bool detachedAnimatorEnabled = _sleepInteractionVisualAnimator != null &&
                                           _sleepInteractionVisualAnimator.enabled;

            LogDevilFTrace(
                $"PoseTick phase='{phase}' frame={Time.frameCount} " +
                $"state='{currentState}' interactionState='{interactionState}' " +
                $"forcedState='{_lastForcedAnimatorStateName}' poseOverride={_hasInteractionPoseRuntimeOverride} " +
                $"physicsOverride={_hasInteractionPhysicsOverride} " +
                $"transform={transformPosition} rigidbody={rigidbodyPosition} world={worldPosition} " +
                $"runtime={runtimePosition} target={targetPosition} " +
                $"petSpriteVisible={petSpriteVisible} detachedVisible={detachedVisible} " +
                $"detachedActive={detachedObjectActive} detachedAnimatorEnabled={detachedAnimatorEnabled} " +
                $"detachedPosition={detachedPosition}");
        }

        private static string DescribeObject(GameObject? target)
        {
            return target != null ? target.name : "null";
        }

        private static string FormatVector2(Vector2 value)
        {
            return $"({value.x:F2}, {value.y:F2})";
        }

        private static string FormatVector3(Vector3 value)
        {
            return $"({value.x:F2}, {value.y:F2}, {value.z:F2})";
        }

        private void LogDevilFTraceWarning(string message)
        {
            if (!ShouldTraceDevilInteraction())
            {
                return;
            }

            Debug.LogWarning($"{DevilFTracePrefix} {message}");
        }

        private void PlayForcedAnimatorState(string stateName)
        {
            if (_animator == null)
            {
                if (ShouldTraceDevilAnimatorState(stateName))
                {
                    LogDevilFTraceWarning($"PlayForcedAnimatorState failed: _animator is null, state='{stateName}'");
                }

                Debug.LogWarning($"[PetAnimation] PlayForcedAnimatorState failed: _animator is null. stateName='{stateName}'");
                return;
            }

            if (string.IsNullOrWhiteSpace(stateName))
            {
                return;
            }

            if (_lastForcedAnimatorStateName == stateName)
            {
                return;
            }

            if (!TryResolveAnimatorPlaybackStateName(
                    stateName,
                    out string resolvedStateName,
                    out string fallbackStateName))
            {
                string controllerName = _animator.runtimeAnimatorController != null
                    ? _animator.runtimeAnimatorController.name
                    : "null";
                if (ShouldTraceDevilAnimatorState(stateName))
                {
                    LogDevilFTraceWarning(
                        $"Animator state missing. requested='{stateName}', fallback='{fallbackStateName}', controller='{controllerName}'");
                }

                Debug.LogWarning(
                    $"[PetAnimation] Animator state missing on '{gameObject.name}'. " +
                    $"requested='{stateName}', fallback='{fallbackStateName}', controller='{controllerName}'");
                _lastForcedAnimatorStateName = stateName;
                return;
            }

            if (ShouldTraceDevilAnimatorState(stateName))
            {
                LogDevilFTrace(
                    $"PlayForcedAnimatorState requested='{stateName}', resolved='{resolvedStateName}', " +
                    $"interactionActive={_context?.RuntimeData.IsPlayerInteractionActive == true}");
            }

            Debug.Log(
                $"[PetAnimation] Playing animator state '{stateName}' on '{gameObject.name}' " +
                $"(resolved='{resolvedStateName}')");
            _animator.Play(resolvedStateName, 0, 0f);
            _lastForcedAnimatorStateName = stateName;
        }

        private void PublishSnapshotIfChanged(PetContext context)
        {
            if (context.EventBus is null)
            {
                return;
            }

            PetRuntimeData runtime = context.RuntimeData;
            PetRuntimeSnapshotChangedEvent snapshot = new(
                runtime.CurrentState,
                runtime.Mood,
                runtime.Energy,
                runtime.Satiety,
                runtime.Relation,
                runtime.WorkRequested,
                runtime.TargetFurnitureId,
                runtime.TargetFurnitureCategory,
                runtime.TargetFurnitureInteractionType,
                runtime.IsTraveling,
                runtime.LastInteractionFurnitureId,
                runtime.LastInteractionSummary,
                petId: runtime.PetId,
                currentBehaviorId: runtime.CurrentBehaviorId);

            if (_lastPublishedSnapshot.HasValue && AreSnapshotsEquivalent(_lastPublishedSnapshot.Value, snapshot))
            {
                return;
            }

            _lastPublishedSnapshot = snapshot;
            context.EventBus.Publish(snapshot);
        }

        private static bool AreSnapshotsEquivalent(PetRuntimeSnapshotChangedEvent previous, PetRuntimeSnapshotChangedEvent current)
        {
            return previous.PetId == current.PetId &&
                   previous.CurrentState == current.CurrentState &&
                   Mathf.Abs(previous.Mood - current.Mood) < 0.01f &&
                   Mathf.Abs(previous.Energy - current.Energy) < 0.01f &&
                   Mathf.Abs(previous.Satiety - current.Satiety) < 0.01f &&
                   previous.WorkRequested == current.WorkRequested &&
                   previous.TargetFurnitureId == current.TargetFurnitureId &&
                   previous.TargetFurnitureCategory == current.TargetFurnitureCategory &&
                   previous.TargetFurnitureInteractionType == current.TargetFurnitureInteractionType &&
                   previous.IsTraveling == current.IsTraveling &&
                   previous.LastInteractionFurnitureId == current.LastInteractionFurnitureId &&
                   previous.LastInteractionSummary == current.LastInteractionSummary &&
                   previous.CurrentBehaviorId == current.CurrentBehaviorId;
        }

        private void EnsureAnimatorBinding()
        {
            if (_animator == null)
            {
                _animator = gameObject.AddComponent<Animator>();
            }

            if (_animator == null)
            {
                Debug.LogWarning("[PetController] Failed to ensure Animator component on pet object.", this);
                return;
            }

            if (_movementController != null && _animator.runtimeAnimatorController != _movementController)
            {
                _animator.runtimeAnimatorController = _movementController;
            }
        }

        private void EnsurePhysicsBinding()
        {
            BoxCollider2D? legacyBoxCollider = gameObject.GetComponent<BoxCollider2D>();
            CapsuleCollider2D? existingCapsule = gameObject.GetComponent<CapsuleCollider2D>();

            if (_rigidbody2D == null)
            {
                _rigidbody2D = gameObject.AddComponent<Rigidbody2D>();
            }

            if (_rigidbody2D != null)
            {
                _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
                _rigidbody2D.gravityScale = 0f;
                _rigidbody2D.freezeRotation = true;
                _rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                _rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            if (_capsuleCollider2D == null)
            {
                _capsuleCollider2D = existingCapsule;
            }

            if (_capsuleCollider2D == null)
            {
                if (legacyBoxCollider != null)
                {
                    Destroy(legacyBoxCollider);
                }

                _capsuleCollider2D = gameObject.AddComponent<CapsuleCollider2D>();
            }

            if (_capsuleCollider2D != null && existingCapsule == null)
            {
                _capsuleCollider2D.direction = CapsuleDirection2D.Vertical;
                if (_spriteRenderer != null && _spriteRenderer.sprite != null)
                {
                    Bounds bounds = _spriteRenderer.sprite.bounds;
                    float colliderWidth = Mathf.Max(0.2f, bounds.size.x * 0.45f);
                    float colliderHeight = Mathf.Max(0.3f, bounds.size.y * 0.7f);
                    _capsuleCollider2D.offset = new Vector2(
                        bounds.center.x,
                        bounds.min.y + colliderHeight * 0.5f);
                    _capsuleCollider2D.size = new Vector2(
                        colliderWidth,
                        colliderHeight);
                }
            }
        }

        private void ApplyWorldMapPetCollisionPolicy()
        {
            if (_hasAppliedWorldMapPetCollisionPolicy ||
                !_ignoreOtherPetCollisions ||
                !IsWorldMapScene())
            {
                return;
            }

            _hasAppliedWorldMapPetCollisionPolicy = true;

            Collider2D[] selfColliders = GetComponents<Collider2D>();
            if (selfColliders.Length == 0)
            {
                return;
            }

            PetController[] otherPets = Object.FindObjectsByType<PetController>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < otherPets.Length; i++)
            {
                PetController other = otherPets[i];
                if (other == null ||
                    ReferenceEquals(other, this) ||
                    !other._ignoreOtherPetCollisions ||
                    !other.IsWorldMapScene())
                {
                    continue;
                }

                Collider2D[] otherColliders = other.GetComponents<Collider2D>();
                for (int j = 0; j < selfColliders.Length; j++)
                {
                    Collider2D selfCollider = selfColliders[j];
                    if (selfCollider == null)
                    {
                        continue;
                    }

                    for (int k = 0; k < otherColliders.Length; k++)
                    {
                        Collider2D otherCollider = otherColliders[k];
                        if (otherCollider == null)
                        {
                            continue;
                        }

                        Physics2D.IgnoreCollision(selfCollider, otherCollider, true);
                    }
                }
            }
        }

        private bool IsWorldMapScene()
        {
            return gameObject.scene.name == WorldMapSceneName;
        }

        private void ApplyRuntimePosition(Vector2 position)
        {
            position = ClampToMovementBounds(position);
            position.y = ResolveGroundY(position.x, ResolveGroundFallbackY(position.y));
            if (_hasInteractionPhysicsOverride)
            {
                ApplyDirectRuntimePosition(position);
                return;
            }

            if (_rigidbody2D != null)
            {
                _rigidbody2D.MovePosition(position);
                return;
            }

            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }

        private void ApplyDirectRuntimePosition(Vector2 position)
        {
            Vector3 worldPosition = new(position.x, position.y, transform.position.z);
            transform.position = worldPosition;
            if (_rigidbody2D != null)
            {
                _rigidbody2D.position = position;
                _rigidbody2D.velocity = Vector2.zero;
                _rigidbody2D.angularVelocity = 0f;
            }
        }

        private Vector2 GetCurrentWorldPosition()
        {
            if (_rigidbody2D != null)
            {
                return _rigidbody2D.position;
            }

            return transform.position;
        }

        private Vector2 ClampToMovementBounds(Vector2 position)
        {
            if (_apartmentMovement != null && _apartmentMovement.IsReady)
                return _apartmentMovement.Clamp(position);
            if (_movementBounds == null)
            {
                return position;
            }

            Bounds bounds = _movementBounds.bounds;
            float halfWidth = 0f;
            float halfHeight = 0f;
            if (_capsuleCollider2D != null)
            {
                halfWidth = _capsuleCollider2D.bounds.extents.x;
                halfHeight = _capsuleCollider2D.bounds.extents.y;
            }

            float minX = bounds.min.x + halfWidth;
            float maxX = bounds.max.x - halfWidth;
            float minY = bounds.min.y + halfHeight;
            float maxY = bounds.max.y - halfHeight;

            if (minX > maxX)
            {
                float centerX = bounds.center.x;
                minX = centerX;
                maxX = centerX;
            }

            if (minY > maxY)
            {
                float centerY = bounds.center.y;
                minY = centerY;
                maxY = centerY;
            }

            return new Vector2(
                Mathf.Clamp(position.x, minX, maxX),
                Mathf.Clamp(position.y, minY, maxY));
        }

        private float ResolveGroundFallbackY(float currentY)
        {
            if (!IsHorizontalOnlyMovement())
            {
                return currentY;
            }

            RandomWander? wander = GetComponent<RandomWander>();
            return wander != null && wander.HorizontalOnly
                ? wander.HorizontalBaselineY
                : _initialGroundY;
        }

        private bool IsHorizontalOnlyMovement()
        {
            if (_playerInputController == null)
            {
                _playerInputController = GetComponent<PetPlayerInputController>();
            }

            if (_playerInputController != null && _playerInputController.HorizontalOnly)
            {
                return true;
            }

            RandomWander? wander = GetComponent<RandomWander>();
            return wander != null && wander.HorizontalOnly;
        }

        private void RefreshWalkableSurfaces()
        {
            if (Time.frameCount - _lastWalkableSurfaceRefreshFrame < WalkableSurfaceRefreshInterval)
                return;
            _lastWalkableSurfaceRefreshFrame = Time.frameCount;
            _walkableSurfaces = Object.FindObjectsByType<WalkableSurface>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        private float ResolveWalkAnchorOffsetY()
        {
            if (_sortingAnchor != null)
            {
                return _sortingAnchor.position.y - transform.position.y;
            }

            if (_capsuleCollider2D != null && _capsuleCollider2D.enabled)
            {
                return _capsuleCollider2D.bounds.min.y - transform.position.y;
            }

            if (_spriteRenderer != null)
            {
                return _spriteRenderer.bounds.min.y - transform.position.y;
            }

            return 0f;
        }

        /// <summary>根据 X 坐标查找下方的可步行表面，返回应站立的 Y。</summary>
        private float ResolveGroundY(float x, float fallbackY)
        {
            RefreshWalkableSurfaces();
            float bestY = fallbackY;
            float walkAnchorOffsetY = ResolveWalkAnchorOffsetY();
            foreach (var surface in _walkableSurfaces)
            {
                if (!surface.TryGetSurfaceY(x, out float surfaceY)) continue;
                // WalkableSurface Y is the walk-anchor height; convert it to transform-space Y.
                float anchoredTransformY = surfaceY - walkAnchorOffsetY;
                if (anchoredTransformY > bestY) bestY = anchoredTransformY;
            }
            return bestY;
        }
    }
}
