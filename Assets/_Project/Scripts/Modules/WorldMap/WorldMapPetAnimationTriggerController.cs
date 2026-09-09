#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Modules.EmotionGarden;
using GeminiLab.Modules.Pet;
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// WorldMap 室外双宠动画唯一裁决入口。
    ///
    /// PetController 继续负责移动、玩家操纵、漫游和 WalkableSurface 过桥；
    /// 本组件只负责 WorldMap 室外 Animator 的最终状态、特殊动作请求和恢复。
    /// 室内桌宠不引用本组件。
    /// </summary>
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class WorldMapPetAnimationTriggerController : MonoBehaviour
    {
        private enum PetAction
        {
            AngelSit,
            AngelPray,
            AngelWater,
            AngelHappy,
            DevilSleep,
            DevilCast,
            DevilProud
        }

        private enum TriggerSource
        {
            Roaming,
            PlayerInput,
            Debug,
            PlacementSuccess
        }

        [Serializable]
        public sealed class DebugAnimationBinding
        {
            [SerializeField, Range(1, 9)] private int _keyNumber = 1;
            [SerializeField] private PetId _petId;
            [SerializeField] private string _label = string.Empty;
            [SerializeField] private string _animationStateName = string.Empty;
            [SerializeField, Min(0.1f)] private float _durationSeconds = 2f;

            public DebugAnimationBinding() { }

            public DebugAnimationBinding(
                int keyNumber,
                PetId petId,
                string label,
                string animationStateName,
                float durationSeconds)
            {
                _keyNumber = Mathf.Clamp(keyNumber, 1, 9);
                _petId = petId;
                _label = label;
                _animationStateName = animationStateName;
                _durationSeconds = Mathf.Max(0.1f, durationSeconds);
            }

            public int KeyNumber => Mathf.Clamp(_keyNumber, 1, 9);
            public PetId PetId => _petId;
            public string Label => _label;
            public string AnimationStateName => _animationStateName;
            public float DurationSeconds => Mathf.Max(0.1f, _durationSeconds);
        }

        private sealed class ActiveAnimation
        {
            public ActiveAnimation(
                PetController pet,
                Animator animator,
                PetAction action,
                string stateName,
                float playbackSeconds,
                SpriteRenderer? renderer,
                bool originalFlipX,
                bool holdTargetFacing,
                bool targetFlipX)
            {
                Pet = pet;
                Animator = animator;
                Action = action;
                StateName = stateName;
                PlaybackSeconds = Mathf.Max(0.01f, playbackSeconds);
                Renderer = renderer;
                OriginalFlipX = originalFlipX;
                HoldTargetFacing = holdTargetFacing;
                TargetFlipX = targetFlipX;
            }

            public PetController Pet { get; }
            public Animator Animator { get; }
            public PetAction Action { get; }
            public string StateName { get; }
            public float PlaybackSeconds { get; }
            public SpriteRenderer? Renderer { get; }
            public bool OriginalFlipX { get; }
            public bool HoldTargetFacing { get; }
            public bool TargetFlipX { get; }
            public float ElapsedSeconds { get; set; }
        }

        private readonly struct AnimationRequest
        {
            public AnimationRequest(
                PetController pet,
                PetAction action,
                TriggerSource source,
                Transform? target,
                Vector3? targetPosition,
                float fallbackDuration)
            {
                Pet = pet;
                Action = action;
                Source = source;
                Target = target;
                TargetPosition = targetPosition;
                FallbackDuration = fallbackDuration;
            }

            public PetController Pet { get; }
            public PetAction Action { get; }
            public TriggerSource Source { get; }
            public Transform? Target { get; }
            public Vector3? TargetPosition { get; }
            public float FallbackDuration { get; }
        }

        private readonly struct FlowerCandidate
        {
            public FlowerCandidate(Transform target, Vector2 closestPoint, float distanceSquared)
            {
                Target = target;
                ClosestPoint = closestPoint;
                DistanceSquared = distanceSquared;
            }

            public Transform Target { get; }
            public Vector2 ClosestPoint { get; }
            public float DistanceSquared { get; }
        }

        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int MoveDirHash = Animator.StringToHash("MoveDir");

        private static readonly KeyCode[] NumberKeys =
        {
            KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
            KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9
        };

        private static readonly KeyCode[] KeypadNumberKeys =
        {
            KeyCode.Keypad0, KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4,
            KeyCode.Keypad5, KeyCode.Keypad6, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9
        };

        [Header("Pets")]
        [SerializeField] private PetController? _angelPet;
        [SerializeField] private PetController? _devilPet;

        [Header("Explicit scene targets")]
        [Tooltip("Only these serialized WorldMap targets are used. Missing Collider2D disables that proximity trigger.")]
        [SerializeField] private Transform[] _appleTreeTargets = Array.Empty<Transform>();
        [SerializeField] private Transform? _wishTreeTarget;
        [SerializeField] private Transform? _angelSignTarget;
        [SerializeField] private Transform? _devilSignTarget;

        [Header("Trigger tuning")]
        [SerializeField, Min(0.1f)] private float _proximityRadius = 1.5f;
        [SerializeField, Min(0.1f)] private float _playerInteractionRadius = 1.5f;
        [SerializeField, Range(0f, 1f)] private float _roamingTriggerChance = 0.35f;
        [SerializeField, Min(0.1f)] private float _roamingCooldownSeconds = 5f;
        [SerializeField, Min(0.1f)] private float _sitDurationSeconds = 2.5f;
        [SerializeField, Min(0.1f)] private float _prayDurationSeconds = 2.5f;
        [SerializeField, Min(0.1f)] private float _waterDurationSeconds = 2f;
        [SerializeField, Min(0.1f)] private float _happyDurationSeconds = 2f;
        [SerializeField, Min(0.1f)] private float _sleepDurationSeconds = 2.5f;
        [SerializeField, Min(0.1f)] private float _castDurationSeconds = 2f;
        [SerializeField, Min(0.1f)] private float _proudDurationSeconds = 2f;
        [SerializeField] private DebugAnimationBinding[] _bindings = Array.Empty<DebugAnimationBinding>();

        private readonly Dictionary<PetId, ActiveAnimation> _activeAnimations = new();
        private readonly Dictionary<PetId, AnimationRequest> _pendingRequests = new();
        private readonly Dictionary<string, int> _rangeLatches = new(StringComparer.Ordinal);
        private readonly Dictionary<PetId, float> _nextRoamingTriggerTime = new();
        private readonly Dictionary<PetId, float> _lastFixedHorizontalPositions = new();
        private readonly Dictionary<PetId, float> _fixedHorizontalDeltas = new();
        private readonly Dictionary<PetId, int> _stableHorizontalDirections = new();
        private readonly Dictionary<PetId, int> _candidateHorizontalDirections = new();
        private readonly Dictionary<PetId, int> _candidateHorizontalDirectionSteps = new();
        private readonly Dictionary<PetId, string> _lastNormalAnimatorStates = new();
        private readonly HashSet<string> _knownPlacementKeys = new(StringComparer.Ordinal);
        private readonly Dictionary<PetPlayerFurnitureInteractionController, bool>
            _suspendedOutdoorFurnitureInteractions = new();

        private IEmotionGardenService? _gardenService;
        private EventBus? _eventBus;
        private IDisposable? _placementsChangedSubscription;
        private IDisposable? _gardenClearedSubscription;
        private bool _placementSnapshotInitialized;
        private bool _sceneTargetsValidated;
        private bool _isShuttingDown;
        private const float ActualHorizontalMotionEpsilon = 0.005f;
        private const int OppositeHorizontalDirectionConfirmationSteps = 2;

        public IReadOnlyList<DebugAnimationBinding> Bindings => _bindings;

        private void OnEnable()
        {
            _isShuttingDown = false;
            _sceneTargetsValidated = false;
            EnsureDependencies();
            SetOutdoorAnimationControllerActive(true);
            SuspendLegacyOutdoorFurnitureInteractions();
        }

        private void OnDisable()
        {
            _isShuttingDown = true;
            ReleaseAllAnimations(false);
            DisposeEventSubscriptions();
            _rangeLatches.Clear();
            ClearHorizontalMotionSamples();
            _lastNormalAnimatorStates.Clear();
            SetOutdoorAnimationControllerActive(false);
            RestoreLegacyOutdoorFurnitureInteractions();
        }

        private void OnDestroy()
        {
            _isShuttingDown = true;
            ReleaseAllAnimations(false);
            DisposeEventSubscriptions();
            ClearHorizontalMotionSamples();
            _lastNormalAnimatorStates.Clear();
            SetOutdoorAnimationControllerActive(false);
            RestoreLegacyOutdoorFurnitureInteractions();
        }

        private void Update()
        {
            EnsureDependencies();
            TickActiveAnimations();

            if (TryReadNumberKey(out int keyNumber))
            {
                TryTriggerForKey(keyNumber);
            }

            HandlePlayerFInput();
            EvaluateProximityTriggers();
        }

        private void FixedUpdate()
        {
            EnsureDependencies();
            SampleActualHorizontalMotion(_angelPet);
            SampleActualHorizontalMotion(_devilPet);
            ApplyOutdoorNormalAnimations();
        }

        /// <summary>Called by the editor authoring pass to bind the two pets and debug mappings.</summary>
        public void ConfigureForAuthoring(PetController? angelPet, PetController? devilPet)
        {
            _angelPet = angelPet;
            _devilPet = devilPet;
            SuspendLegacyOutdoorFurnitureInteractions();
            _bindings = new[]
            {
                new DebugAnimationBinding(1, PetId.Angel, "天使 - 坐地", "Outdoor_Sit", _sitDurationSeconds),
                new DebugAnimationBinding(2, PetId.Angel, "天使 - 祈祷", "Outdoor_Pray", _prayDurationSeconds),
                new DebugAnimationBinding(3, PetId.Angel, "天使 - 开心", "Outdoor_Happy", _happyDurationSeconds),
                new DebugAnimationBinding(4, PetId.Angel, "天使 - 浇水", "Outdoor_Water", _waterDurationSeconds),
                new DebugAnimationBinding(5, PetId.Devil, "恶魔 - 睡觉", "Outdoor_Sleep", _sleepDurationSeconds),
                new DebugAnimationBinding(6, PetId.Devil, "恶魔 - 施法", "Outdoor_Cast", _castDurationSeconds),
                new DebugAnimationBinding(7, PetId.Devil, "恶魔 - 得意", "Outdoor_Proud", _proudDurationSeconds)
            };
        }

        /// <summary>
        /// WorldMap owns the outdoor pet animation interaction whitelist. The
        /// legacy furniture adapter has scene bindings and fallback world points
        /// intended for indoor furniture, so it must not remain an alternate
        /// trigger source on either explicitly bound outdoor pet.
        /// </summary>
        private void SuspendLegacyOutdoorFurnitureInteractions()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            SuspendLegacyOutdoorFurnitureInteraction(_angelPet);
            SuspendLegacyOutdoorFurnitureInteraction(_devilPet);
        }

        private void SuspendLegacyOutdoorFurnitureInteraction(PetController? pet)
        {
            if (pet == null)
            {
                return;
            }

            PetPlayerFurnitureInteractionController? interactionController =
                pet.GetComponent<PetPlayerFurnitureInteractionController>();
            if (interactionController == null ||
                _suspendedOutdoorFurnitureInteractions.ContainsKey(interactionController))
            {
                return;
            }

            _suspendedOutdoorFurnitureInteractions.Add(interactionController, interactionController.enabled);
            interactionController.enabled = false;
            Debug.Log(
                $"[WorldMapPetAnimation] Disabled legacy furniture interaction on outdoor pet '{pet.name}'. " +
                "Only the explicit WorldMap pet whitelist may trigger outdoor pet animation.",
                pet);
        }

        private void RestoreLegacyOutdoorFurnitureInteractions()
        {
            foreach (KeyValuePair<PetPlayerFurnitureInteractionController, bool> pair in
                     _suspendedOutdoorFurnitureInteractions)
            {
                if (pair.Key != null)
                {
                    pair.Key.enabled = pair.Value;
                }
            }

            _suspendedOutdoorFurnitureInteractions.Clear();
        }

        /// <summary>
        /// Binds authored scene targets. Runtime proximity never searches by object name.
        /// </summary>
        public void ConfigureSceneTargets(
            Transform[]? appleTrees,
            Transform? wishTree,
            Transform? angelSign,
            Transform? devilSign)
        {
            if (appleTrees != null && appleTrees.Length > 0)
            {
                _appleTreeTargets = appleTrees;
            }

            if (wishTree != null) _wishTreeTarget = wishTree;
            if (angelSign != null) _angelSignTarget = angelSign;
            if (devilSign != null) _devilSignTarget = devilSign;
            _sceneTargetsValidated = false;
        }

        /// <summary>Retains the existing number-key debug entry point for animation QA.</summary>
        public bool TryTriggerForKey(int keyNumber)
        {
            DebugAnimationBinding? binding = FindBinding(keyNumber);
            if (binding == null)
            {
                Debug.LogWarning($"[WorldMapPetAnimation] No debug binding for key {keyNumber}.", this);
                return false;
            }

            PetController? pet = binding.PetId == PetId.Angel ? _angelPet : _devilPet;
            if (pet == null)
            {
                Debug.LogWarning(
                    $"[WorldMapPetAnimation] Missing {binding.PetId} pet for debug key {binding.KeyNumber}.",
                    this);
                return false;
            }

            if (!TryResolveAction(binding.AnimationStateName, out PetAction action))
            {
                Debug.LogWarning(
                    $"[WorldMapPetAnimation] Unsupported debug state '{binding.AnimationStateName}'.",
                    this);
                return false;
            }

            return RequestAction(
                pet,
                action,
                TriggerSource.Debug,
                null,
                null,
                binding.DurationSeconds);
        }

        private void EnsureDependencies()
        {
            ValidateSceneTargets();
            SetOutdoorAnimationControllerActive(true);

            if (_gardenService == null)
            {
                ServiceLocator.TryResolve(out _gardenService);
            }

            if (_eventBus == null && ServiceLocator.TryResolve(out EventBus? eventBus) && eventBus != null)
            {
                _eventBus = eventBus;
                _placementsChangedSubscription = _eventBus.Subscribe<EmotionFlowerPlacementsChangedEvent>(
                    _ => HandlePlacementsChanged());
                _gardenClearedSubscription = _eventBus.Subscribe<EmotionGardenClearedEvent>(
                    _ => HandleGardenCleared());
            }

            if (!_placementSnapshotInitialized && _gardenService != null)
            {
                RefreshPlacementSnapshot(false);
            }
        }

        private void SetOutdoorAnimationControllerActive(bool active)
        {
            if (_angelPet != null)
            {
                _angelPet.SetExternalAnimationControllerActive(active);
            }

            if (_devilPet != null)
            {
                _devilPet.SetExternalAnimationControllerActive(active);
            }
        }

        private void ValidateSceneTargets()
        {
            if (_sceneTargetsValidated) return;
            _sceneTargetsValidated = true;

            if (_appleTreeTargets == null || _appleTreeTargets.Length == 0)
            {
                Debug.LogWarning(
                    "[WorldMapPetAnimation] No serialized apple-tree targets; Sit/Sleep proximity triggers are disabled.",
                    this);
            }
            else
            {
                for (int i = 0; i < _appleTreeTargets.Length; i++)
                {
                    LogMissingColliderIfNeeded(_appleTreeTargets[i], "apple-tree");
                }
            }

            LogMissingColliderIfNeeded(_wishTreeTarget, "wishing-tree");
            LogMissingColliderIfNeeded(_angelSignTarget, "angel sign");
            LogMissingColliderIfNeeded(_devilSignTarget, "devil sign");
        }

        private static void LogMissingColliderIfNeeded(Transform? target, string label)
        {
            if (target == null) return;
            if (TryGetClosestPoint(target, target.position, out _)) return;
            Debug.LogWarning(
                $"[WorldMapPetAnimation] Serialized {label} target '{target.name}' has no enabled Collider2D; its proximity trigger is disabled.",
                target);
        }

        private void HandlePlayerFInput()
        {
            if (!Input.GetKeyDown(KeyCode.F)) return;

            PetController? controlledPet = null;
            if (_angelPet != null && _angelPet.IsPlayerControlEnabled)
            {
                controlledPet = _angelPet;
            }
            else if (_devilPet != null && _devilPet.IsPlayerControlEnabled)
            {
                controlledPet = _devilPet;
            }

            if (controlledPet == null || _activeAnimations.ContainsKey(controlledPet.PetId)) return;

            if (controlledPet.PetId == PetId.Angel)
            {
                if (TryGetNearestColliderTarget(
                        controlledPet,
                        _appleTreeTargets,
                        _playerInteractionRadius,
                        out Transform? appleTree,
                        out Vector3 applePoint))
                {
                    RequestAction(
                        controlledPet,
                        PetAction.AngelSit,
                        TriggerSource.PlayerInput,
                        appleTree,
                        applePoint,
                        _sitDurationSeconds);
                }
                else if (TryGetNearestAngelFlower(
                             controlledPet,
                             _playerInteractionRadius,
                             out Transform? flowerTarget,
                             out Vector3 flowerPoint))
                {
                    RequestAction(
                        controlledPet,
                        PetAction.AngelWater,
                        TriggerSource.PlayerInput,
                        flowerTarget,
                        flowerPoint,
                        _waterDurationSeconds);
                }
                else if (TryGetTargetClosestPoint(
                             controlledPet,
                             _wishTreeTarget,
                             _playerInteractionRadius,
                             out Vector3 wishPoint))
                {
                    RequestAction(
                        controlledPet,
                        PetAction.AngelPray,
                        TriggerSource.PlayerInput,
                        _wishTreeTarget,
                        wishPoint,
                        _prayDurationSeconds);
                }
            }
            else if (TryGetNearestColliderTarget(
                         controlledPet,
                         _appleTreeTargets,
                         _playerInteractionRadius,
                         out Transform? appleTree,
                         out Vector3 applePoint))
            {
                RequestAction(
                    controlledPet,
                    PetAction.DevilSleep,
                    TriggerSource.PlayerInput,
                    appleTree,
                    applePoint,
                    _sleepDurationSeconds);
            }
            else if (TryGetTargetClosestPoint(
                         controlledPet,
                         _devilSignTarget,
                         _playerInteractionRadius,
                         out Vector3 devilSignPoint))
            {
                RequestAction(
                    controlledPet,
                    PetAction.DevilCast,
                    TriggerSource.PlayerInput,
                    _devilSignTarget,
                    devilSignPoint,
                    _castDurationSeconds);
            }
        }

        private void EvaluateProximityTriggers()
        {
            EvaluateAngelProximity(_angelPet);
            EvaluateDevilProximity(_devilPet);
        }

        private void EvaluateAngelProximity(PetController? pet)
        {
            if (pet == null) return;

            if (pet.IsPlayerControlEnabled)
            {
                UpdateRangeLatch(pet.PetId, PetAction.AngelWater, null, false);
                UpdateRangeLatch(pet.PetId, PetAction.AngelSit, null, false);
                UpdateRangeLatch(pet.PetId, PetAction.AngelPray, null, false);
                return;
            }

            bool hasFlower = TryGetNearestAngelFlower(
                pet,
                _proximityRadius,
                out Transform? flowerTarget,
                out Vector3 flowerPoint);
            bool enteredWaterRange = UpdateRangeLatch(
                pet.PetId,
                PetAction.AngelWater,
                flowerTarget,
                hasFlower);
            if (enteredWaterRange && !_activeAnimations.ContainsKey(pet.PetId))
            {
                RequestAction(
                    pet,
                    PetAction.AngelWater,
                    TriggerSource.Roaming,
                    flowerTarget,
                    flowerPoint,
                    _waterDurationSeconds);
                return;
            }

            bool nearAppleTree = TryGetNearestColliderTarget(
                pet,
                _appleTreeTargets,
                _proximityRadius,
                out Transform? appleTree,
                out Vector3 applePoint);
            if (TryRoamingTrigger(
                    pet,
                    PetAction.AngelSit,
                    nearAppleTree,
                    appleTree,
                    applePoint,
                    _sitDurationSeconds))
            {
                return;
            }

            bool nearWishTree = TryGetTargetClosestPoint(
                pet,
                _wishTreeTarget,
                _proximityRadius,
                out Vector3 wishPoint);
            TryRoamingTrigger(
                pet,
                PetAction.AngelPray,
                nearWishTree,
                _wishTreeTarget,
                wishPoint,
                _prayDurationSeconds);
        }

        private void EvaluateDevilProximity(PetController? pet)
        {
            if (pet == null) return;

            if (pet.IsPlayerControlEnabled)
            {
                UpdateRangeLatch(pet.PetId, PetAction.DevilSleep, null, false);
                UpdateRangeLatch(pet.PetId, PetAction.DevilCast, null, false);
                return;
            }

            bool nearAppleTree = TryGetNearestColliderTarget(
                pet,
                _appleTreeTargets,
                _proximityRadius,
                out Transform? appleTree,
                out Vector3 applePoint);
            if (TryRoamingTrigger(
                    pet,
                    PetAction.DevilSleep,
                    nearAppleTree,
                    appleTree,
                    applePoint,
                    _sleepDurationSeconds))
            {
                return;
            }

            bool nearDevilSign = TryGetTargetClosestPoint(
                pet,
                _devilSignTarget,
                _proximityRadius,
                out Vector3 devilSignPoint);
            TryRoamingTrigger(
                pet,
                PetAction.DevilCast,
                nearDevilSign,
                _devilSignTarget,
                devilSignPoint,
                _castDurationSeconds);
        }

        private bool TryRoamingTrigger(
            PetController pet,
            PetAction action,
            bool inRange,
            Transform? target,
            Vector3 targetPoint,
            float fallbackDuration)
        {
            if (pet.IsPlayerControlEnabled)
            {
                UpdateRangeLatch(pet.PetId, action, null, false);
                return false;
            }

            bool entered = UpdateRangeLatch(pet.PetId, action, target, inRange);
            if (!entered || _activeAnimations.ContainsKey(pet.PetId))
            {
                return false;
            }

            if (!CanRollRoamingTrigger(pet.PetId)) return false;
            if (UnityEngine.Random.value > Mathf.Clamp01(_roamingTriggerChance)) return false;

            bool started = RequestAction(
                pet,
                action,
                TriggerSource.Roaming,
                target,
                inRange ? targetPoint : null,
                fallbackDuration);
            if (started)
            {
                _nextRoamingTriggerTime[pet.PetId] =
                    Time.time + Mathf.Max(0.1f, _roamingCooldownSeconds);
            }

            return started;
        }

        private bool RequestAction(
            PetController pet,
            PetAction action,
            TriggerSource source,
            Transform? target,
            Vector3? targetPosition,
            float fallbackDuration)
        {
            if (!IsActionForPet(action, pet.PetId)) return false;
            if (source == TriggerSource.Roaming && pet.IsPlayerControlEnabled)
            {
                return false;
            }

            var request = new AnimationRequest(
                pet,
                action,
                source,
                target,
                targetPosition,
                fallbackDuration);

            if (_activeAnimations.ContainsKey(pet.PetId))
            {
                // Roaming is an edge-triggered opportunity, not a delayed command.
                // Only a deliberate input or a successful placement may wait for the
                // current special action to finish.
                return source != TriggerSource.Roaming && EnqueuePendingRequest(request);
            }

            return StartAction(request);
        }

        private bool EnqueuePendingRequest(AnimationRequest request)
        {
            if (!_pendingRequests.TryGetValue(request.Pet.PetId, out AnimationRequest current) ||
                PriorityFor(request.Source) > PriorityFor(current.Source))
            {
                _pendingRequests[request.Pet.PetId] = request;
                return true;
            }

            return false;
        }

        private bool StartAction(AnimationRequest request)
        {
            PetController pet = request.Pet;
            if (pet == null || pet.IsMovementLocked)
            {
                return false;
            }

            Animator? animator = pet.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                Debug.LogWarning($"[WorldMapPetAnimation] '{pet.name}' has no Animator.", pet);
                return false;
            }

            RuntimeAnimatorController? controller = animator.runtimeAnimatorController;
            if (controller == null)
            {
                Debug.LogWarning($"[WorldMapPetAnimation] '{pet.name}' Animator has no controller.", pet);
                return false;
            }

            string requestedState = StateNameFor(request.Action);
            if (!TryResolveStateName(animator, requestedState, out string resolvedStateName))
            {
                Debug.LogWarning(
                    $"[WorldMapPetAnimation] State '{requestedState}' is missing from '{controller.name}'.",
                    pet);
                return false;
            }

            SpriteRenderer? renderer = pet.GetComponentInChildren<SpriteRenderer>(true);
            bool originalFlipX = renderer != null && renderer.flipX;
            bool holdTargetFacing = false;
            bool targetFlipX = originalFlipX;
            if (renderer != null && request.TargetPosition is Vector3 targetPosition)
            {
                holdTargetFacing = true;
                float horizontalDirection = targetPosition.x - pet.transform.position.x;
                if (Mathf.Abs(horizontalDirection) > 0.00001f)
                {
                    targetFlipX = ResolveFacingFlip(horizontalDirection);
                }

                renderer.flipX = targetFlipX;
            }

            // Lock movement before entering the special state. This keeps the
            // PetController from applying a normal movement/pose update in the
            // same frame as this animation request.
            pet.SetExternalMovementLock(true);

            // The WorldMap controller has movement AnyState transitions. Clear the
            // movement condition before entering a special state so it cannot win
            // over this explicit Play call on the next Animator evaluation.
            animator.SetBool(IsMovingHash, false);
            animator.speed = 1f;
            animator.Play(resolvedStateName, 0, 0f);
            animator.Update(0f);

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float clipLength = stateInfo.length;
            float playbackSeconds = clipLength > 0.0001f
                ? clipLength
                : Mathf.Max(0.1f, request.FallbackDuration);

            _activeAnimations[pet.PetId] = new ActiveAnimation(
                pet,
                animator,
                request.Action,
                resolvedStateName,
                playbackSeconds,
                renderer,
                originalFlipX,
                holdTargetFacing,
                targetFlipX);

            Debug.Log(
                $"[WorldMapPetAnimation] {pet.PetId} started {request.Action} ({resolvedStateName}) " +
                $"source={request.Source}, oneShotSeconds={playbackSeconds:0.###}.",
                pet);
            return true;
        }

        private void TickActiveAnimations()
        {
            if (_activeAnimations.Count == 0) return;

            var activePetIds = new List<PetId>(_activeAnimations.Keys);
            foreach (PetId petId in activePetIds)
            {
                if (!_activeAnimations.TryGetValue(petId, out ActiveAnimation? active)) continue;
                if (active.Pet == null || active.Animator == null)
                {
                    ReleaseAnimation(petId, true);
                    continue;
                }

                active.Animator.speed = 1f;
                active.Animator.SetBool(IsMovingHash, false);
                if (active.HoldTargetFacing && active.Renderer != null)
                {
                    active.Renderer.flipX = active.TargetFlipX;
                }

                AnimatorStateInfo stateInfo = active.Animator.GetCurrentAnimatorStateInfo(0);
                if (!stateInfo.IsName(active.StateName))
                {
                    float normalizedTime = active.PlaybackSeconds <= 0.0001f
                        ? 0f
                        : Mathf.Clamp01(active.ElapsedSeconds / active.PlaybackSeconds);
                    active.Animator.Play(active.StateName, 0, normalizedTime);
                    active.Animator.Update(0f);
                }

                active.ElapsedSeconds += Time.deltaTime;
                if (active.ElapsedSeconds + 0.0001f >= active.PlaybackSeconds)
                {
                    ReleaseAnimation(petId, true);
                }
            }
        }

        private void ReleaseAnimation(PetId petId, bool processPending)
        {
            if (!_activeAnimations.TryGetValue(petId, out ActiveAnimation? active)) return;

            active.Animator.speed = 1f;
            if (active.Renderer != null)
            {
                // PetController remains the normal movement mirror owner. Restore
                // the pre-action facing here; the next movement tick will apply its
                // existing serialized side-frame convention if movement resumes.
                active.Renderer.flipX = active.OriginalFlipX;
            }

            if (active.Pet != null) active.Pet.SetExternalMovementLock(false);
            _activeAnimations.Remove(petId);

            bool startedPending = false;
            if (processPending && !_isShuttingDown &&
                _pendingRequests.TryGetValue(petId, out AnimationRequest pending))
            {
                _pendingRequests.Remove(petId);
                startedPending = StartAction(pending);
            }

            if (!startedPending)
            {
                RestoreNormalAnimatorState(petId, active.Animator);
            }
        }

        private void RestoreNormalAnimatorState(PetId petId, Animator animator)
        {
            // The WorldMap controller owns normal outdoor animation playback.
            // Restore one authored Idle state at the special-action boundary;
            // the next normal sampling pass will switch to Move only if the
            // pet has actually moved since that boundary.
            if (!TryResolveStateName(animator, "Idle_Side", out string idleStateName)) return;

            animator.SetBool(IsMovingHash, false);
            animator.SetFloat(MoveXHash, 0f);
            animator.SetFloat(MoveYHash, 0f);
            animator.SetInteger(MoveDirHash, 2);
            animator.speed = 1f;
            animator.Play(idleStateName, 0, 0f);
            animator.Update(0f);
            _lastNormalAnimatorStates[petId] = "Idle_Side";
        }

        private void ReleaseAllAnimations(bool processPending)
        {
            var petIds = new List<PetId>(_activeAnimations.Keys);
            foreach (PetId petId in petIds)
            {
                ReleaseAnimation(petId, processPending);
            }

            _activeAnimations.Clear();
            _pendingRequests.Clear();
        }

        private void HandlePlacementsChanged()
        {
            if (_gardenService == null) EnsureDependencies();
            RefreshPlacementSnapshot(true);
        }

        private void HandleGardenCleared()
        {
            _knownPlacementKeys.Clear();
            _placementSnapshotInitialized = true;
        }

        private void RefreshPlacementSnapshot(bool triggerNewPlacements)
        {
            if (_gardenService == null) return;

            IReadOnlyList<PlacedEmotionFlower> placements = _gardenService.GetPlacedFlowers();
            var currentKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < placements.Count; i++)
            {
                PlacedEmotionFlower placement = placements[i];
                string key = PlacementKey(placement);
                currentKeys.Add(key);

                if (!triggerNewPlacements || !_placementSnapshotInitialized || _knownPlacementKeys.Contains(key))
                {
                    continue;
                }

                PetId petId = EmotionFlowerCatalog.NormalizeOwner(placement.Owner) ==
                              EmotionFlowerCatalog.OwnerDemon
                    ? PetId.Devil
                    : PetId.Angel;
                PetController? pet = petId == PetId.Angel ? _angelPet : _devilPet;
                if (pet == null) continue;

                PetAction action = petId == PetId.Angel
                    ? PetAction.AngelHappy
                    : PetAction.DevilProud;
                // This event is published only after TryPlaceFlower has accepted
                // the record. The visual slot may be activated later in the same
                // frame, so Happy/Proud does not depend on a preview or slot lookup.
                RequestAction(
                    pet,
                    action,
                    TriggerSource.PlacementSuccess,
                    null,
                    null,
                    DurationFor(action));
            }

            _knownPlacementKeys.Clear();
            foreach (string key in currentKeys) _knownPlacementKeys.Add(key);
            _placementSnapshotInitialized = true;
        }

        private bool TryGetNearestAngelFlower(
            PetController pet,
            float radius,
            out Transform? target,
            out Vector3 closestPoint)
        {
            target = null;
            closestPoint = default;
            if (_gardenService == null) return false;

            IReadOnlyList<PlacedEmotionFlower> placements = _gardenService.GetPlacedFlowers();
            WorldMapPlacementSlot[] slots = UnityEngine.Object.FindObjectsByType<WorldMapPlacementSlot>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var usedSlots = new HashSet<WorldMapPlacementSlot>();
            var flowerCandidates = new List<FlowerCandidate>();
            float maxDistanceSquared = radius * radius;

            for (int placementIndex = 0; placementIndex < placements.Count; placementIndex++)
            {
                PlacedEmotionFlower placement = placements[placementIndex];
                if (EmotionFlowerCatalog.NormalizeOwner(placement.Owner) !=
                    EmotionFlowerCatalog.OwnerAngel)
                {
                    continue;
                }

                string flowerId = EmotionFlowerCatalog.NormalizeOwner(placement.Owner) + "|" +
                                  EmotionFlowerCatalog.NormalizeEmotionType(placement.EmotionType);
                WorldMapFlowerPlacementController.PlacementVisualType visualType = placement.IsCluster
                    ? WorldMapFlowerPlacementController.PlacementVisualType.Cluster
                    : WorldMapFlowerPlacementController.PlacementVisualType.Single;
                WorldMapPlacementSlot? matchedSlot = null;
                Rect matchedRect = default;
                float bestRecordDistance = float.PositiveInfinity;

                for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
                {
                    WorldMapPlacementSlot slot = slots[slotIndex];
                    if (slot == null || usedSlots.Contains(slot) || !slot.IsOccupied ||
                        !slot.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    Collider2D? occupancyCollider = slot.GetComponent<Collider2D>();
                    if (occupancyCollider == null || !occupancyCollider.enabled ||
                        !occupancyCollider.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    WorldMapPlacedFlower? metadata = slot.GetComponent<WorldMapPlacedFlower>();
                    if (metadata == null || metadata.FlowerId != flowerId ||
                        metadata.VisualType != visualType)
                    {
                        continue;
                    }

                    Rect occupiedRect = slot.GetOccupiedRect();
                    if (occupiedRect.width <= 0.0001f || occupiedRect.height <= 0.0001f)
                    {
                        continue;
                    }

                    Vector2 savedPosition = new(placement.WorldX, placement.WorldY);
                    float recordDistance = ((Vector2)occupiedRect.center - savedPosition).sqrMagnitude;
                    if (recordDistance >= bestRecordDistance) continue;
                    bestRecordDistance = recordDistance;
                    matchedSlot = slot;
                    matchedRect = occupiedRect;
                }

                if (matchedSlot == null) continue;
                usedSlots.Add(matchedSlot);

                Vector2 petPosition = pet.transform.position;
                Vector2 point = new(
                    Mathf.Clamp(petPosition.x, matchedRect.xMin, matchedRect.xMax),
                    Mathf.Clamp(petPosition.y, matchedRect.yMin, matchedRect.yMax));
                float distanceSquared = (point - petPosition).sqrMagnitude;
                if (distanceSquared > maxDistanceSquared) continue;

                flowerCandidates.Add(new FlowerCandidate(matchedSlot.transform, point, distanceSquared));
            }

            FlowerCandidate? nearest = null;
            for (int i = 0; i < flowerCandidates.Count; i++)
            {
                FlowerCandidate candidate = flowerCandidates[i];
                if (!nearest.HasValue || candidate.DistanceSquared < nearest.Value.DistanceSquared)
                {
                    nearest = candidate;
                }
            }

            if (!nearest.HasValue) return false;
            target = nearest.Value.Target;
            closestPoint = nearest.Value.ClosestPoint;
            return true;
        }

        private static bool TryGetNearestColliderTarget(
            PetController pet,
            Transform[]? targets,
            float radius,
            out Transform? nearest,
            out Vector3 closestPoint)
        {
            nearest = null;
            closestPoint = default;
            if (targets == null || targets.Length == 0) return false;

            Vector2 source = pet.transform.position;
            float bestDistanceSquared = radius * radius;
            for (int i = 0; i < targets.Length; i++)
            {
                Transform? target = targets[i];
                if (!TryGetClosestPoint(target, source, out Vector2 candidatePoint)) continue;

                float distanceSquared = (candidatePoint - source).sqrMagnitude;
                if (distanceSquared > bestDistanceSquared) continue;
                bestDistanceSquared = distanceSquared;
                nearest = target;
                closestPoint = candidatePoint;
            }

            return nearest != null;
        }

        private static bool TryGetTargetClosestPoint(
            PetController pet,
            Transform? target,
            float radius,
            out Vector3 closestPoint)
        {
            closestPoint = default;
            if (!TryGetClosestPoint(target, pet.transform.position, out Vector2 candidatePoint))
            {
                return false;
            }

            if (((Vector2)pet.transform.position - candidatePoint).sqrMagnitude > radius * radius)
            {
                return false;
            }

            closestPoint = candidatePoint;
            return true;
        }

        private static bool TryGetClosestPoint(
            Transform? target,
            Vector2 source,
            out Vector2 closestPoint)
        {
            closestPoint = default;
            if (target == null) return false;

            Collider2D[] colliders = target.GetComponentsInChildren<Collider2D>(true);
            float bestDistanceSquared = float.PositiveInfinity;
            bool found = false;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D collider = colliders[i];
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Vector2 candidatePoint = collider.ClosestPoint(source);
                float distanceSquared = (candidatePoint - source).sqrMagnitude;
                if (distanceSquared >= bestDistanceSquared) continue;
                bestDistanceSquared = distanceSquared;
                closestPoint = candidatePoint;
                found = true;
            }

            return found;
        }

        private bool UpdateRangeLatch(
            PetId petId,
            PetAction action,
            Transform? target,
            bool inRange)
        {
            string key = $"{petId}:{action}";
            int previousTargetId = _rangeLatches.TryGetValue(key, out int previous) ? previous : 0;
            int currentTargetId = inRange && target != null ? target.GetInstanceID() : 0;
            _rangeLatches[key] = currentTargetId;
            return currentTargetId != 0 && currentTargetId != previousTargetId;
        }

        private void SampleActualHorizontalMotion(PetController? pet)
        {
            if (pet == null)
            {
                return;
            }

            PetId petId = pet.PetId;
            float currentX = ResolveActualPetPosition(pet).x;
            if (!_lastFixedHorizontalPositions.TryGetValue(petId, out float previousX))
            {
                _lastFixedHorizontalPositions[petId] = currentX;
                _fixedHorizontalDeltas[petId] = 0f;
                return;
            }

            _fixedHorizontalDeltas[petId] = currentX - previousX;
            _lastFixedHorizontalPositions[petId] = currentX;
        }

        private void ClearHorizontalMotionSamples()
        {
            _lastFixedHorizontalPositions.Clear();
            _fixedHorizontalDeltas.Clear();
            _stableHorizontalDirections.Clear();
            _candidateHorizontalDirections.Clear();
            _candidateHorizontalDirectionSteps.Clear();
        }

        private void ApplyOutdoorNormalAnimations()
        {
            ApplyOutdoorNormalAnimation(_angelPet);
            ApplyOutdoorNormalAnimation(_devilPet);
        }

        private void ApplyOutdoorNormalAnimation(PetController? pet)
        {
            if (pet == null)
            {
                return;
            }

            if (!_fixedHorizontalDeltas.TryGetValue(pet.PetId, out float horizontalDelta))
            {
                return;
            }

            // Sampling continues while a special action is active so the
            // release boundary cannot turn a stale position difference into a
            // false Move state.
            if (_activeAnimations.ContainsKey(pet.PetId))
            {
                return;
            }

            bool movedHorizontally = Mathf.Abs(horizontalDelta) > ActualHorizontalMotionEpsilon;
            PlayOutdoorNormalState(pet, movedHorizontally, horizontalDelta);
        }

        private void PlayOutdoorNormalState(
            PetController pet,
            bool moving,
            float horizontalDelta)
        {
            Animator? animator = pet.GetComponent<Animator>();
            if (animator == null)
            {
                return;
            }

            string requestedState = moving ? "Move_Side" : "Idle_Side";
            if (!TryResolveStateName(animator, requestedState, out string resolvedStateName))
            {
                Debug.LogWarning(
                    $"[WorldMapPetAnimation] Normal state '{requestedState}' is missing from the pet Animator.",
                    pet);
                return;
            }

            // The existing controllers use Any State transitions for Move and
            // allow self transitions. Keep the condition false and enter the
            // authored state explicitly, otherwise a true IsMoving value can
            // restart the same Move Clip repeatedly.
            animator.SetBool(IsMovingHash, false);
            int stableDirection = ResolveStableHorizontalDirection(pet.PetId, horizontalDelta, moving);
            animator.SetFloat(MoveXHash, moving ? stableDirection : 0f);
            animator.SetFloat(MoveYHash, 0f);
            animator.SetInteger(MoveDirHash, 2);
            animator.speed = 1f;

            if (stableDirection != 0)
            {
                pet.ApplyExternalAnimationFacing(stableDirection);
            }

            bool stateChanged = !_lastNormalAnimatorStates.TryGetValue(pet.PetId, out string? lastState) ||
                                !string.Equals(lastState, requestedState, StringComparison.Ordinal);
            bool transitionStillActive = animator.IsInTransition(0);
            if (stateChanged || transitionStillActive)
            {
                animator.Play(resolvedStateName, 0, 0f);
                animator.Update(0f);
            }

            _lastNormalAnimatorStates[pet.PetId] = requestedState;
        }

        private int ResolveStableHorizontalDirection(PetId petId, float horizontalDelta, bool moving)
        {
            if (!moving)
            {
                _candidateHorizontalDirections.Remove(petId);
                _candidateHorizontalDirectionSteps.Remove(petId);
                return _stableHorizontalDirections.TryGetValue(petId, out int stableDirection)
                    ? stableDirection
                    : 0;
            }

            int observedDirection = horizontalDelta > 0f ? 1 : -1;
            if (!_stableHorizontalDirections.TryGetValue(petId, out int currentDirection) ||
                currentDirection == 0)
            {
                _stableHorizontalDirections[petId] = observedDirection;
                _candidateHorizontalDirections.Remove(petId);
                _candidateHorizontalDirectionSteps.Remove(petId);
                return observedDirection;
            }

            if (observedDirection == currentDirection)
            {
                _candidateHorizontalDirections.Remove(petId);
                _candidateHorizontalDirectionSteps.Remove(petId);
                return currentDirection;
            }

            if (!_candidateHorizontalDirections.TryGetValue(petId, out int candidateDirection) ||
                candidateDirection != observedDirection)
            {
                _candidateHorizontalDirections[petId] = observedDirection;
                _candidateHorizontalDirectionSteps[petId] = 1;
                return currentDirection;
            }

            int candidateSteps = _candidateHorizontalDirectionSteps.TryGetValue(petId, out int steps)
                ? steps + 1
                : 1;
            _candidateHorizontalDirectionSteps[petId] = candidateSteps;
            if (candidateSteps < OppositeHorizontalDirectionConfirmationSteps)
            {
                return currentDirection;
            }

            _stableHorizontalDirections[petId] = observedDirection;
            _candidateHorizontalDirections.Remove(petId);
            _candidateHorizontalDirectionSteps.Remove(petId);
            return observedDirection;
        }

        private static Vector2 ResolveActualPetPosition(PetController pet)
        {
            Rigidbody2D? rigidbody = pet.GetComponent<Rigidbody2D>();
            return rigidbody != null ? rigidbody.position : pet.transform.position;
        }

        private static bool ResolveFacingFlip(float horizontalDirection)
        {
            if (Mathf.Abs(horizontalDirection) <= 0.00001f) return false;

            // Both outdoor pets currently have the serialized PetController
            // side-frame convention "face left": moving right flips the sprite.
            // Keep target-facing consistent with that existing authoring value.
            return horizontalDirection > 0f;
        }

        private static int PriorityFor(TriggerSource source)
        {
            return source switch
            {
                TriggerSource.PlacementSuccess => 300,
                TriggerSource.PlayerInput => 200,
                TriggerSource.Debug => 150,
                TriggerSource.Roaming => 100,
                _ => 0
            };
        }

        private bool CanRollRoamingTrigger(PetId petId)
        {
            return !_nextRoamingTriggerTime.TryGetValue(petId, out float nextTime) ||
                   Time.time >= nextTime;
        }

        private void DisposeEventSubscriptions()
        {
            _placementsChangedSubscription?.Dispose();
            _gardenClearedSubscription?.Dispose();
            _placementsChangedSubscription = null;
            _gardenClearedSubscription = null;
            _eventBus = null;
        }

        private static bool TryReadNumberKey(out int keyNumber)
        {
            for (int number = 1; number <= 9; number++)
            {
                if (Input.GetKeyDown(NumberKeys[number]) || Input.GetKeyDown(KeypadNumberKeys[number]))
                {
                    keyNumber = number;
                    return true;
                }
            }

            keyNumber = 0;
            return false;
        }

        private DebugAnimationBinding? FindBinding(int keyNumber)
        {
            for (int i = 0; i < _bindings.Length; i++)
            {
                DebugAnimationBinding? binding = _bindings[i];
                if (binding != null && binding.KeyNumber == keyNumber) return binding;
            }

            return null;
        }

        private static bool TryResolveStateName(
            Animator animator,
            string requestedStateName,
            out string resolvedStateName)
        {
            resolvedStateName = requestedStateName;
            if (string.IsNullOrWhiteSpace(requestedStateName)) return false;

            if (animator.HasState(0, Animator.StringToHash(requestedStateName))) return true;

            string baseLayerStateName = $"Base Layer.{requestedStateName}";
            if (animator.HasState(0, Animator.StringToHash(baseLayerStateName)))
            {
                resolvedStateName = baseLayerStateName;
                return true;
            }

            return false;
        }

        private static bool IsActionForPet(PetAction action, PetId petId)
        {
            bool angelAction = action is PetAction.AngelSit or PetAction.AngelPray or
                PetAction.AngelWater or PetAction.AngelHappy;
            return angelAction ? petId == PetId.Angel : petId == PetId.Devil;
        }

        private static string StateNameFor(PetAction action)
        {
            return action switch
            {
                PetAction.AngelSit => "Outdoor_Sit",
                PetAction.AngelPray => "Outdoor_Pray",
                PetAction.AngelWater => "Outdoor_Water",
                PetAction.AngelHappy => "Outdoor_Happy",
                PetAction.DevilSleep => "Outdoor_Sleep",
                PetAction.DevilCast => "Outdoor_Cast",
                PetAction.DevilProud => "Outdoor_Proud",
                _ => string.Empty
            };
        }

        private float DurationFor(PetAction action)
        {
            return action switch
            {
                PetAction.AngelSit => _sitDurationSeconds,
                PetAction.AngelPray => _prayDurationSeconds,
                PetAction.AngelWater => _waterDurationSeconds,
                PetAction.AngelHappy => _happyDurationSeconds,
                PetAction.DevilSleep => _sleepDurationSeconds,
                PetAction.DevilCast => _castDurationSeconds,
                PetAction.DevilProud => _proudDurationSeconds,
                _ => 2f
            };
        }

        private static bool TryResolveAction(string stateName, out PetAction action)
        {
            action = stateName switch
            {
                "Outdoor_Sit" => PetAction.AngelSit,
                "Outdoor_Pray" => PetAction.AngelPray,
                "Outdoor_Water" => PetAction.AngelWater,
                "Outdoor_Happy" => PetAction.AngelHappy,
                "Outdoor_Sleep" => PetAction.DevilSleep,
                "Outdoor_Cast" => PetAction.DevilCast,
                "Outdoor_Proud" => PetAction.DevilProud,
                _ => default
            };

            return stateName is "Outdoor_Sit" or "Outdoor_Pray" or "Outdoor_Water" or "Outdoor_Happy"
                or "Outdoor_Sleep" or "Outdoor_Cast" or "Outdoor_Proud";
        }

        private static string PlacementKey(PlacedEmotionFlower placement)
        {
            return $"{placement.SlotIndex}|" +
                   $"{EmotionFlowerCatalog.NormalizeEmotionType(placement.EmotionType)}|" +
                   $"{EmotionFlowerCatalog.NormalizeOwner(placement.Owner)}|" +
                   placement.IsCluster;
        }
    }
}
