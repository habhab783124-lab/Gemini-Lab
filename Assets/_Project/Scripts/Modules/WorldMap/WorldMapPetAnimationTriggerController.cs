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
    /// Drives the outdoor pet special-animation state machine.
    ///
    /// PetController remains the owner of the normal Idle/Move states.  This
    /// component only owns a temporary special action, locks that pet while the
    /// action is playing, then releases it back to PetController.
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

        [Serializable]
        public sealed class DebugAnimationBinding
        {
            [SerializeField, Range(1, 9)] private int _keyNumber = 1;
            [SerializeField] private PetId _petId;
            [SerializeField] private string _label = string.Empty;
            [SerializeField] private string _animationStateName = string.Empty;
            [SerializeField, Min(0.1f)] private float _durationSeconds = 2f;

            public DebugAnimationBinding() { }

            public DebugAnimationBinding(int keyNumber, PetId petId, string label, string animationStateName, float durationSeconds)
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
            public ActiveAnimation(PetController pet, Animator animator, PetAction action, string stateName,
                float durationSeconds, float clipLengthSeconds, SpriteRenderer? renderer, bool originalFlipX)
            {
                Pet = pet;
                Animator = animator;
                Action = action;
                StateName = stateName;
                DurationSeconds = Mathf.Max(0.1f, durationSeconds);
                ClipLengthSeconds = Mathf.Max(0.01f, clipLengthSeconds);
                Renderer = renderer;
                OriginalFlipX = originalFlipX;
            }

            public PetController Pet { get; }
            public Animator Animator { get; }
            public PetAction Action { get; }
            public string StateName { get; }
            public float DurationSeconds { get; }
            public float ClipLengthSeconds { get; }
            public SpriteRenderer? Renderer { get; }
            public bool OriginalFlipX { get; }
            public float ElapsedSeconds { get; set; }
        }

        private readonly struct PendingAnimation
        {
            public PendingAnimation(PetAction action, Transform? target)
            {
                Action = action;
                Target = target;
            }

            public PetAction Action { get; }
            public Transform? Target { get; }
        }

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

        private static readonly string[] AppleTreeNames =
        {
            "大树 2", "大树 3", "大树 4", "大树 5", "苹果树", "苹果树 2", "苹果树 3", "苹果树 4", "苹果树 5"
        };

        private static readonly string[] AngelSignNames =
        {
            "天使标牌", "天使区域标牌", "天使区域的标牌", "标牌_天使", "AngelSign", "AngelSignboard"
        };

        private static readonly string[] DevilSignNames =
        {
            "恶魔标牌", "恶魔区域标牌", "恶魔区域的标牌", "标牌_恶魔", "DevilSign", "DevilSignboard"
        };

        [Header("Pets")]
        [SerializeField] private PetController? _angelPet;
        [SerializeField] private PetController? _devilPet;

        [Header("Explicit scene targets")]
        [Tooltip("Apple trees that can trigger Sit/Sleep. 大树 1 (wishing tree) is intentionally excluded.")]
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
        private readonly Dictionary<PetId, PendingAnimation> _pendingAnimations = new();
        private readonly Dictionary<string, bool> _proximityLatches = new(StringComparer.Ordinal);
        private readonly Dictionary<PetId, float> _nextRoamingTriggerTime = new();
        private readonly HashSet<string> _knownPlacementKeys = new(StringComparer.Ordinal);

        private IEmotionGardenService? _gardenService;
        private EventBus? _eventBus;
        private IDisposable? _placementsChangedSubscription;
        private IDisposable? _gardenClearedSubscription;
        private bool _placementSnapshotInitialized;
        private bool _sceneTargetsResolved;
        private bool _isShuttingDown;

        public IReadOnlyList<DebugAnimationBinding> Bindings => _bindings;

        private void OnEnable()
        {
            _isShuttingDown = false;
            _sceneTargetsResolved = false;
            EnsureDependencies();
        }

        private void OnDisable()
        {
            _isShuttingDown = true;
            ReleaseAllAnimations(false);
            DisposeEventSubscriptions();
            _proximityLatches.Clear();
        }

        private void OnDestroy()
        {
            _isShuttingDown = true;
            ReleaseAllAnimations(false);
            DisposeEventSubscriptions();
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

        /// <summary>Called by the editor authoring pass to bind the two pets and debug mappings.</summary>
        public void ConfigureForAuthoring(PetController? angelPet, PetController? devilPet)
        {
            _angelPet = angelPet;
            _devilPet = devilPet;
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
        /// Binds authored scene targets. Empty optional target arguments preserve an
        /// existing Inspector assignment so an unrecognized sign is still editable.
        /// </summary>
        public void ConfigureSceneTargets(Transform[]? appleTrees, Transform? wishTree,
            Transform? angelSign, Transform? devilSign)
        {
            if (appleTrees != null && appleTrees.Length > 0)
            {
                _appleTreeTargets = appleTrees;
            }

            if (wishTree != null) _wishTreeTarget = wishTree;
            if (angelSign != null) _angelSignTarget = angelSign;
            if (devilSign != null) _devilSignTarget = devilSign;
            _sceneTargetsResolved = false;
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
                Debug.LogWarning($"[WorldMapPetAnimation] Missing {binding.PetId} pet for debug key {binding.KeyNumber}.", this);
                return false;
            }

            if (!TryResolveAction(binding.AnimationStateName, out PetAction action))
            {
                Debug.LogWarning($"[WorldMapPetAnimation] Unsupported debug state '{binding.AnimationStateName}'.", this);
                return false;
            }

            return TryStartAction(pet, action, null, binding.DurationSeconds, false);
        }

        private void EnsureDependencies()
        {
            ResolveSceneTargetsFallbacks();

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

        private void ResolveSceneTargetsFallbacks()
        {
            if (_sceneTargetsResolved) return;

            if (_appleTreeTargets == null || _appleTreeTargets.Length == 0)
            {
                _appleTreeTargets = FindTransforms(AppleTreeNames);
            }

            _wishTreeTarget ??= FindFirstTransform(new[] { "许愿树", "WishingTree" });
            _angelSignTarget ??= FindFirstTransform(AngelSignNames);
            _devilSignTarget ??= FindFirstTransform(DevilSignNames);
            _sceneTargetsResolved = true;

            if (_appleTreeTargets.Length == 0)
            {
                Debug.LogWarning("[WorldMapPetAnimation] No apple-tree targets found; Sit/Sleep proximity triggers are disabled.", this);
            }

            if (_wishTreeTarget == null)
            {
                Debug.LogWarning("[WorldMapPetAnimation] No wishing-tree target found; Pray proximity trigger is disabled.", this);
            }

            if (_angelSignTarget == null || _devilSignTarget == null)
            {
                Debug.Log("[WorldMapPetAnimation] Sign targets are optional; assign missing angel/devil sign references in Inspector.", this);
            }
        }

        private void HandlePlayerFInput()
        {
            if (!Input.GetKeyDown(KeyCode.F)) return;

            PetController? controlledPet = null;
            if (_angelPet != null && _angelPet.IsPlayerControlEnabled) controlledPet = _angelPet;
            else if (_devilPet != null && _devilPet.IsPlayerControlEnabled) controlledPet = _devilPet;
            if (controlledPet == null || _activeAnimations.ContainsKey(controlledPet.PetId)) return;

            if (controlledPet.PetId == PetId.Angel)
            {
                if (TryGetNearestTarget(controlledPet, _appleTreeTargets, _playerInteractionRadius, out Transform? appleTree))
                {
                    TryStartAction(controlledPet, PetAction.AngelSit, appleTree, _sitDurationSeconds, true);
                }
                else if (IsWithinRadius(controlledPet.transform.position, _wishTreeTarget, _playerInteractionRadius))
                {
                    TryStartAction(controlledPet, PetAction.AngelPray, _wishTreeTarget, _prayDurationSeconds, true);
                }
            }
            else
            {
                if (TryGetNearestTarget(controlledPet, _appleTreeTargets, _playerInteractionRadius, out Transform? appleTree))
                {
                    TryStartAction(controlledPet, PetAction.DevilSleep, appleTree, _sleepDurationSeconds, true);
                }
                else if (IsWithinRadius(controlledPet.transform.position, _devilSignTarget, _playerInteractionRadius))
                {
                    TryStartAction(controlledPet, PetAction.DevilCast, _devilSignTarget, _castDurationSeconds, true);
                }
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

            bool hasFlower = TryGetNearestAngelFlower(pet, out Vector3 flowerPosition);
            bool enteredWaterRange = UpdateProximityLatch(pet.PetId, PetAction.AngelWater, hasFlower);
            if (enteredWaterRange && !_activeAnimations.ContainsKey(pet.PetId))
            {
                TryStartAction(pet, PetAction.AngelWater, null, _waterDurationSeconds, true, flowerPosition);
                return;
            }

            bool nearAppleTree = TryGetNearestTarget(pet, _appleTreeTargets, _proximityRadius, out Transform? appleTree);
            if (TryRoamingTrigger(pet, PetAction.AngelSit, nearAppleTree, appleTree, _sitDurationSeconds)) return;

            bool nearWishTree = IsWithinRadius(pet.transform.position, _wishTreeTarget, _proximityRadius);
            if (TryRoamingTrigger(pet, PetAction.AngelPray, nearWishTree, _wishTreeTarget, _prayDurationSeconds)) return;
        }

        private void EvaluateDevilProximity(PetController? pet)
        {
            if (pet == null) return;

            bool nearAppleTree = TryGetNearestTarget(pet, _appleTreeTargets, _proximityRadius, out Transform? appleTree);
            if (TryRoamingTrigger(pet, PetAction.DevilSleep, nearAppleTree, appleTree, _sleepDurationSeconds)) return;

            bool nearDevilSign = IsWithinRadius(pet.transform.position, _devilSignTarget, _proximityRadius);
            TryRoamingTrigger(pet, PetAction.DevilCast, nearDevilSign, _devilSignTarget, _castDurationSeconds);
        }

        private bool TryRoamingTrigger(PetController pet, PetAction action, bool inRange, Transform? target, float duration)
        {
            bool entered = UpdateProximityLatch(pet.PetId, action, inRange);
            if (!entered || pet.IsPlayerControlEnabled || _activeAnimations.ContainsKey(pet.PetId)) return false;

            if (!CanRollRoamingTrigger(pet.PetId)) return false;
            if (UnityEngine.Random.value > Mathf.Clamp01(_roamingTriggerChance)) return false;

            _nextRoamingTriggerTime[pet.PetId] = Time.time + Mathf.Max(0.1f, _roamingCooldownSeconds);
            return TryStartAction(pet, action, target, duration, true);
        }

        private bool CanRollRoamingTrigger(PetId petId)
        {
            return !_nextRoamingTriggerTime.TryGetValue(petId, out float nextTime) || Time.time >= nextTime;
        }

        private bool UpdateProximityLatch(PetId petId, PetAction action, bool inRange)
        {
            string key = $"{petId}:{action}";
            bool wasInRange = _proximityLatches.TryGetValue(key, out bool previous) && previous;
            _proximityLatches[key] = inRange;
            return inRange && !wasInRange;
        }

        private bool TryStartAction(PetController pet, PetAction action, Transform? target, float duration,
            bool queueIfBusy, Vector3? explicitTargetPosition = null)
        {
            if (!IsActionForPet(action, pet.PetId)) return false;

            if (_activeAnimations.ContainsKey(pet.PetId))
            {
                if (queueIfBusy) _pendingAnimations[pet.PetId] = new PendingAnimation(action, target);
                return false;
            }

            Animator? animator = pet.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                Debug.LogWarning($"[WorldMapPetAnimation] '{pet.name}' has no Animator.", pet);
                return false;
            }

            var animatorController = animator.runtimeAnimatorController;
            if (animatorController == null)
            {
                Debug.LogWarning($"[WorldMapPetAnimation] '{pet.name}' Animator has no controller.", pet);
                return false;
            }

            string requestedState = StateNameFor(action);
            if (!TryResolveStateName(animator, requestedState, out string resolvedStateName))
            {
                Debug.LogWarning($"[WorldMapPetAnimation] State '{requestedState}' is missing from '{animatorController.name}'.", pet);
                return false;
            }

            SpriteRenderer? renderer = pet.GetComponentInChildren<SpriteRenderer>(true);
            bool originalFlipX = renderer != null && renderer.flipX;
            Vector3? targetPosition = explicitTargetPosition;
            if (!targetPosition.HasValue && target != null) targetPosition = target.position;
            if (renderer != null && targetPosition.HasValue)
            {
                renderer.flipX = targetPosition.Value.x < pet.transform.position.x;
            }

            animator.speed = 1f;
            animator.Play(resolvedStateName, 0, 0f);
            animator.Update(0f);
            float clipLength = animator.GetCurrentAnimatorStateInfo(0).length;
            pet.SetExternalMovementLock(true);
            _activeAnimations[pet.PetId] = new ActiveAnimation(
                pet, animator, action, resolvedStateName, duration, clipLength, renderer, originalFlipX);

            Debug.Log($"[WorldMapPetAnimation] {pet.PetId} started {action} ({resolvedStateName}).", pet);
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

                active.ElapsedSeconds += Time.deltaTime;
                if (active.ElapsedSeconds >= active.DurationSeconds)
                {
                    ReleaseAnimation(petId, true);
                }
            }
        }

        private void ReleaseAnimation(PetId petId, bool processPending)
        {
            if (!_activeAnimations.TryGetValue(petId, out ActiveAnimation? active)) return;

            active.Animator.speed = 1f;
            if (active.Renderer != null) active.Renderer.flipX = active.OriginalFlipX;
            if (active.Pet != null) active.Pet.SetExternalMovementLock(false);
            _activeAnimations.Remove(petId);

            if (processPending && !_isShuttingDown && _pendingAnimations.TryGetValue(petId, out PendingAnimation pending))
            {
                _pendingAnimations.Remove(petId);
                PetController? pet = petId == PetId.Angel ? _angelPet : _devilPet;
                if (pet != null) TryStartAction(pet, pending.Action, pending.Target, DurationFor(pending.Action), true);
            }
        }

        private void ReleaseAllAnimations(bool processPending)
        {
            var petIds = new List<PetId>(_activeAnimations.Keys);
            foreach (PetId petId in petIds) ReleaseAnimation(petId, processPending);
            _activeAnimations.Clear();
            _pendingAnimations.Clear();
        }

        private void HandlePlacementsChanged()
        {
            if (_gardenService == null)
            {
                EnsureDependencies();
            }

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

                if (triggerNewPlacements && _placementSnapshotInitialized && !_knownPlacementKeys.Contains(key))
                {
                    PetId petId = EmotionFlowerCatalog.NormalizeOwner(placement.Owner) == EmotionFlowerCatalog.OwnerDemon
                        ? PetId.Devil
                        : PetId.Angel;
                    PetController? pet = petId == PetId.Angel ? _angelPet : _devilPet;
                    if (pet != null)
                    {
                        PetAction action = petId == PetId.Angel ? PetAction.AngelHappy : PetAction.DevilProud;
                        TryStartAction(pet, action, null, DurationFor(action), true);
                    }
                }
            }

            _knownPlacementKeys.Clear();
            foreach (string key in currentKeys) _knownPlacementKeys.Add(key);
            _placementSnapshotInitialized = true;
        }

        private bool TryGetNearestAngelFlower(PetController pet, out Vector3 position)
        {
            position = default;
            if (_gardenService == null) return false;

            IReadOnlyList<PlacedEmotionFlower> placements = _gardenService.GetPlacedFlowers();
            float bestDistance = float.PositiveInfinity;
            bool found = false;
            for (int i = 0; i < placements.Count; i++)
            {
                PlacedEmotionFlower placement = placements[i];
                if (EmotionFlowerCatalog.NormalizeOwner(placement.Owner) != EmotionFlowerCatalog.OwnerAngel) continue;

                Vector3 candidate = new(placement.WorldX, placement.WorldY, pet.transform.position.z);
                float distance = (candidate - pet.transform.position).sqrMagnitude;
                if (distance > _proximityRadius * _proximityRadius || distance >= bestDistance) continue;
                bestDistance = distance;
                position = candidate;
                found = true;
            }

            return found;
        }

        private static bool TryGetNearestTarget(PetController pet, Transform[]? targets, float radius, out Transform? nearest)
        {
            nearest = null;
            if (targets == null || targets.Length == 0) return false;

            float bestDistance = radius * radius;
            for (int i = 0; i < targets.Length; i++)
            {
                Transform? target = targets[i];
                if (target == null) continue;
                float distance = (target.position - pet.transform.position).sqrMagnitude;
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    nearest = target;
                }
            }

            return nearest != null;
        }

        private static bool IsWithinRadius(Vector3 source, Transform? target, float radius)
        {
            return target != null && (target.position - source).sqrMagnitude <= radius * radius;
        }

        private static Transform[] FindTransforms(string[] names)
        {
            var found = new List<Transform>();
            for (int i = 0; i < names.Length; i++)
            {
                Transform? target = FindFirstTransform(new[] { names[i] });
                if (target != null && !found.Contains(target)) found.Add(target);
            }

            return found.ToArray();
        }

        private static Transform? FindFirstTransform(string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                GameObject? target = GameObject.Find(names[i]);
                if (target != null) return target.transform;
            }

            return null;
        }

        private static string PlacementKey(PlacedEmotionFlower placement)
        {
            return $"{placement.SlotIndex}|{placement.EmotionType}|{placement.Owner}|{placement.IsCluster}";
        }

        private static bool IsActionForPet(PetAction action, PetId petId)
        {
            bool angelAction = action is PetAction.AngelSit or PetAction.AngelPray or PetAction.AngelWater or PetAction.AngelHappy;
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

        private DebugAnimationBinding? FindBinding(int keyNumber)
        {
            for (int i = 0; i < _bindings.Length; i++)
            {
                DebugAnimationBinding? binding = _bindings[i];
                if (binding != null && binding.KeyNumber == keyNumber) return binding;
            }

            return null;
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

        private static bool TryResolveStateName(Animator animator, string requestedStateName, out string resolvedStateName)
        {
            resolvedStateName = requestedStateName;
            if (string.IsNullOrWhiteSpace(requestedStateName)) return false;

            int requestedHash = Animator.StringToHash(requestedStateName);
            if (animator.HasState(0, requestedHash)) return true;

            string baseLayerStateName = $"Base Layer.{requestedStateName}";
            if (animator.HasState(0, Animator.StringToHash(baseLayerStateName)))
            {
                resolvedStateName = baseLayerStateName;
                return true;
            }

            return false;
        }
    }
}
