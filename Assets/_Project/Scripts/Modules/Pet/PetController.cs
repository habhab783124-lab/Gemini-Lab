#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.FSM;
using GeminiLab.Modules.Furniture;
using GeminiLab.Modules.Navigation;
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
        [SerializeField] private Transform? _sortingAnchor;
        [SerializeField] private int _sortingOrderOffset;
        [SerializeField] private PetId _petId = PetId.Angel;
        [SerializeField] private PetInteractionVisualStrategy _interactionVisualStrategy = new();

        public PetId PetId => _petId;

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
        private Vector2 _lastAnimationPosition;
        private Vector2 _lastMoveDirection = Vector2.down;
        private Vector2 _playerAnimationDirection = Vector2.down;
        private bool _hasPlayerAnimationDirection;
        private string _lastForcedAnimatorStateName = string.Empty;
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

        public string CurrentState => _context?.RuntimeData.CurrentState ?? "None";

        public PetRuntimeData? RuntimeData => _context?.RuntimeData;

        public bool IsPlayerControlEnabled => IsPlayerControlled();

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _capsuleCollider2D = GetComponent<CapsuleCollider2D>();
            _playerInputController = GetComponent<PetPlayerInputController>();
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
        }

        private void Update()
        {
            if (_context is null || _stateMachine is null || _tickService is null)
            {
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
            if (_context is not null)
            {
                ApplyRuntimePosition(_context.RuntimeData.Position);
            }

            if (IsInactivePlayerPet())
            {
                return;
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

        private void UpdateDynamicSortingOrder()
        {
            if (_spriteRenderer == null || _hasStoredInteractionSorting)
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

            context.RuntimeData.Position += movementInput * _playerInputController.MoveSpeed * deltaTime;
            context.RuntimeData.Position = ClampToMovementBounds(context.RuntimeData.Position);
            context.RuntimeData.TargetPosition = context.RuntimeData.Position;
            context.RuntimeData.TargetReached = true;
        }

        private void TickInactivePlayerControlled(PetContext context, float deltaTime)
        {
            CancelPlayerInteraction(context);
            _hasPlayerAnimationDirection = false;
            SetPlayerControlledState(context, IdleState.StateName);
            context.Advance(deltaTime);
            _tickService?.Tick(context, deltaTime);
            ResetPlayerControlledRuntime(context);
            context.RuntimeData.TargetPosition = context.RuntimeData.Position;
            context.RuntimeData.TargetReached = true;
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
                PlayForcedAnimatorState(ResolveInteractionStateName());
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
                PlayForcedAnimatorState(ResolveIdleStateName(_lastMoveDirection));
            }
            else
            {
                _lastForcedAnimatorStateName = string.Empty;
            }

            if (IsPlayerControlled() && _hasPlayerAnimationDirection)
            {
                _lastMoveDirection = _playerAnimationDirection;
            }
            else if (hasDelta)
            {
                _lastMoveDirection = delta.normalized;
            }
            else if (isMoving && _context is not null)
            {
                // When frame-to-frame delta is tiny, keep direction aligned with
                // current movement target so transitions still choose correct clip.
                Vector2 targetDelta = _context.RuntimeData.TargetPosition - currentPosition;
                if (targetDelta.sqrMagnitude > DirectionEpsilonSqr)
                {
                    _lastMoveDirection = targetDelta.normalized;
                }
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

            return _context?.RuntimeData.TargetFurnitureInteractionType switch
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
                runtime.WorkRequested,
                runtime.TargetFurnitureId,
                runtime.TargetFurnitureCategory,
                runtime.TargetFurnitureInteractionType,
                runtime.IsTraveling,
                runtime.LastInteractionFurnitureId,
                runtime.LastInteractionSummary,
                petId: runtime.PetId);

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
                   previous.LastInteractionSummary == current.LastInteractionSummary;
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
                if (legacyBoxCollider != null)
                {
                    Destroy(legacyBoxCollider);
                }

                _capsuleCollider2D = gameObject.AddComponent<CapsuleCollider2D>();
            }

            if (_capsuleCollider2D != null)
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

        private void ApplyRuntimePosition(Vector2 position)
        {
            position = ClampToMovementBounds(position);
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
    }
}
