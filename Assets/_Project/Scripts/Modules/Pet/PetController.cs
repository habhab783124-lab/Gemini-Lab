#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.FSM;
using GeminiLab.Modules.Furniture;
using GeminiLab.Modules.Navigation;
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Runtime host that ticks pet FSM and stat service.
    /// </summary>
    public sealed class PetController : MonoBehaviour
    {
        private const string MoveFrontStateName = "Move_Front";
        private const string InteractReadStateName = "Interact_Read";
        private const string InteractBesideDoorStateName = "Interact_BesideDoor";

        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int MoveDirHash = Animator.StringToHash("MoveDir");
        // Squared-distance threshold for movement direction updates.
        private const float DirectionEpsilonSqr = 0.000001f;

        [SerializeField] private PetStateValueSO? _config;
        [SerializeField] private PersonalityMatrixSO? _personality;
        [SerializeField] private RuntimeAnimatorController? _movementController;
        [SerializeField] private bool _sideFramesFaceLeft = true;

        private PetContext? _context;
        private StateMachine<PetContext>? _stateMachine;
        private StatTickService? _tickService;
        private IPetCommandLinkService? _commandLinkService;
        private Animator? _animator;
        private SpriteRenderer? _spriteRenderer;
        private Vector2 _lastAnimationPosition;
        private Vector2 _lastMoveDirection = Vector2.down;
        private string _lastForcedAnimatorStateName = string.Empty;
        private PetRuntimeSnapshotChangedEvent? _lastPublishedSnapshot;

        public string CurrentState => _context?.RuntimeData.CurrentState ?? "None";

        public PetRuntimeData? RuntimeData => _context?.RuntimeData;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            EnsureAnimatorBinding();
            _lastAnimationPosition = transform.position;

            PetStateValueSO config = _config ?? ScriptableObject.CreateInstance<PetStateValueSO>();
            _ = _personality; // Reserved for Phase 3 prompt adaptation.

            PetRuntimeData runtime = new()
            {
                Mood = config.InitialMood,
                Energy = config.InitialEnergy,
                Satiety = config.InitialSatiety,
                Position = transform.position
            };

            if (!ServiceLocator.TryResolve(out EventBus? eventBus))
            {
                eventBus = null;
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
                    transform.position = new Vector3(position.x, position.y, transform.position.z);
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

            _context.RuntimeData.Position = transform.position;
            RefreshLateBoundServices(_context);
            HandleDebugCommandInput(_context);
            ProcessCommands(_context, _stateMachine);
            _tickService.Tick(_context, Time.deltaTime);
            _stateMachine.Tick(Time.deltaTime);
            _context.ApplyPosition?.Invoke(_context.RuntimeData.Position);
            UpdateMovementAnimation();
            PublishSnapshotIfChanged(_context);
        }

        private void FixedUpdate()
        {
            _stateMachine?.FixedTick(Time.fixedDeltaTime);
        }

        private void OnDestroy()
        {
            if (_stateMachine is not null)
            {
                _stateMachine.StateChanged -= PublishStateChanged;
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

        private void UpdateMovementAnimation()
        {
            if (_animator is null)
            {
                return;
            }

            string? currentState = _context?.RuntimeData.CurrentState;
            if (currentState == InteractingState.StateName || currentState == WorkingState.StateName)
            {
                PlayForcedAnimatorState(ResolveInteractionStateName());
                _animator.SetBool(IsMovingHash, false);
                _animator.speed = 1f;
                return;
            }

            Vector2 currentPosition = transform.position;
            Vector2 delta = currentPosition - _lastAnimationPosition;
            _lastAnimationPosition = currentPosition;

            bool isMoving = string.Equals(currentState, MovingState.StateName, System.StringComparison.Ordinal);
            bool hasDelta = delta.sqrMagnitude > DirectionEpsilonSqr;

            if (!isMoving)
            {
                PlayForcedAnimatorState(MoveFrontStateName);
            }
            else
            {
                _lastForcedAnimatorStateName = string.Empty;
            }

            if (hasDelta)
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
            _animator.speed = isMoving ? 1f : 0f;

            UpdateSideMirror(moveDir, _lastMoveDirection);
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
            if (_spriteRenderer is null)
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
            if (_context?.RuntimeData.RequiredWorkTargetType == PetWorkTargetType.WorkDesk ||
                _context?.RuntimeData.TargetFurnitureInteractionType == FurnitureInteractionType.WorkFocus ||
                _context?.RuntimeData.TargetFurnitureCategory == FurnitureCategory.WorkDesk)
            {
                return InteractReadStateName;
            }

            return _context?.RuntimeData.TargetFurnitureInteractionType switch
            {
                FurnitureInteractionType.PlayHarp => InteractReadStateName,
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

        private void PlayForcedAnimatorState(string stateName)
        {
            if (_animator is null || string.IsNullOrWhiteSpace(stateName))
            {
                return;
            }

            if (_lastForcedAnimatorStateName == stateName)
            {
                return;
            }

            _animator.Play(stateName, 0, 0f);
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
                runtime.LastInteractionSummary);

            if (_lastPublishedSnapshot.HasValue && AreSnapshotsEquivalent(_lastPublishedSnapshot.Value, snapshot))
            {
                return;
            }

            _lastPublishedSnapshot = snapshot;
            context.EventBus.Publish(snapshot);
        }

        private static bool AreSnapshotsEquivalent(PetRuntimeSnapshotChangedEvent previous, PetRuntimeSnapshotChangedEvent current)
        {
            return previous.CurrentState == current.CurrentState &&
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
            if (_animator is null)
            {
                _animator = gameObject.AddComponent<Animator>();
            }

            if (_movementController is not null && _animator.runtimeAnimatorController != _movementController)
            {
                _animator.runtimeAnimatorController = _movementController;
            }
        }
    }
}
