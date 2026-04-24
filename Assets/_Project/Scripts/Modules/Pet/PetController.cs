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
        [SerializeField] private PetStateValueSO? _config;
        [SerializeField] private PersonalityMatrixSO? _personality;

        private PetContext? _context;
        private StateMachine<PetContext>? _stateMachine;
        private StatTickService? _tickService;
        private IPetCommandLinkService? _commandLinkService;

        public string CurrentState => _context?.RuntimeData.CurrentState ?? "None";

        public PetRuntimeData? RuntimeData => _context?.RuntimeData;

        private void Awake()
        {
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
                                context.RuntimeData.WorkRequested = false;
                                context.EventBus?.Publish(new PetWorkFailedEvent(request.TraceId, "No available WorkDesk target."));
                                ResetWorkRuntime(context);
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
                        if (string.Equals(context.RuntimeData.ActiveWorkTraceId, request.TraceId, System.StringComparison.Ordinal))
                        {
                            context.RuntimeData.WorkRequested = false;
                            context.EventBus?.Publish(new PetWorkCompletedEvent(request.TraceId, request.Message));
                            ResetWorkRuntime(context);
                        }

                        break;
                    case PetCommandType.WorkFailed:
                        if (string.Equals(context.RuntimeData.ActiveWorkTraceId, request.TraceId, System.StringComparison.Ordinal))
                        {
                            context.RuntimeData.WorkRequested = false;
                            context.EventBus?.Publish(new PetWorkFailedEvent(request.TraceId, request.Message));
                            ResetWorkRuntime(context);
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

            if (Input.GetKeyDown(KeyCode.W))
            {
                bool forceWake = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                string traceId = _commandLinkService.RequestWork(forceWake);
                context.RuntimeData.LastTraceId = traceId;
                Debug.Log($"[PetCommand] Work requested traceId={traceId}, forceWake={forceWake}");
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
            context.RuntimeData.TargetReached = false;
            context.RuntimeData.ActivePath.Clear();
        }
    }
}
