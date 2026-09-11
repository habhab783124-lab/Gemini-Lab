#nullable enable
using System;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Furniture;
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Allows player-triggered furniture interactions when the pet is close enough
    /// to a configured scene object and the player presses F.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PetPlayerFurnitureInteractionController : MonoBehaviour
    {
        private const string DevilFTracePrefix = "[DEVIL_F_TRACE]";

        /// <summary>
        /// 漫游自动交互的搜索半径增量。手动 F 键/点击用绑定自身的激活距离即可；
        /// 自动路径在宠物“卡住”时触发，宠物可能撞在大型家具边缘而硬编码交互点在家具中心，
        /// 因此额外放宽该距离，确保边缘碰撞也能命中对应绑定。
        /// </summary>
        private const float AutoInteractionRadiusPadding = 1.25f;

        [Serializable]
        public sealed class InteractionAnimationOption
        {
            [SerializeField] private string _label = string.Empty;
            [SerializeField] private string _animatorStateName = string.Empty;
            [SerializeField] private string _variantKey = string.Empty;

            public string Label => _label;
            public string AnimatorStateName => _animatorStateName;
            public string VariantKey => _variantKey;
        }

        [Serializable]
        private sealed class InteractionBinding
        {
            [SerializeField] private string _label = string.Empty;
            [SerializeField] private GameObject? _target;
            [SerializeField] private GameObject? _poseTarget;
            [Tooltip("宠物走到家具旁的站立点；与交互动画的姿势点分开。")]
            [SerializeField] private Transform? _approachPoint;
            [SerializeField] private string _fallbackTargetName = string.Empty;
            [SerializeField] private string _fallbackPoseTargetName = string.Empty;
            [SerializeField] private bool _useFallbackWorldPoint;
            [SerializeField] private Vector2 _fallbackWorldPoint;
            [SerializeField] private float _activationDistance = 1.5f;
            [SerializeField] private bool _hideTargetWhileInteracting;
            [SerializeField] private GameObject[] _additionalHideTargets = Array.Empty<GameObject>();
            [SerializeField] private GameObject? _sortingTarget;
            [SerializeField] private string _fallbackSortingTargetName = string.Empty;
            [SerializeField] private bool _useTargetSortingWhileInteracting;
            [SerializeField] private int _sortingOrderOffsetWhileInteracting;
            [SerializeField] private bool _usePetPoseOverride;
            [SerializeField] private bool _useTargetPositionForPetPose;
            [SerializeField] private Vector2 _petInteractionLocalOffset;
            [SerializeField] private Vector2 _petInteractionWorldPoint;
            [SerializeField] private Vector3 _petInteractionScale = Vector3.one;
            [SerializeField] private FurnitureCategory _category = FurnitureCategory.Decoration;
            [SerializeField] private FurnitureInteractionType _interactionType = FurnitureInteractionType.DecorInspect;
            [SerializeField] private PetSelfInteractionVariant _animationVariant = PetSelfInteractionVariant.Read;
            [SerializeField] private float _interactionDurationSeconds = 1.5f;
            [SerializeField] private InteractionAnimationOption[] _animationOptions = Array.Empty<InteractionAnimationOption>();

            public string Label => _label;
            public GameObject? Target => _target;
            public GameObject? PoseTarget => _poseTarget;
            public Transform? ApproachPoint => _approachPoint;
            public string FallbackTargetName => _fallbackTargetName;
            public string FallbackPoseTargetName => _fallbackPoseTargetName;
            public bool UseFallbackWorldPoint => _useFallbackWorldPoint;
            public Vector2 FallbackWorldPoint => _fallbackWorldPoint;
            public float ActivationDistance => Mathf.Max(0.1f, _activationDistance);
            public bool HideTargetWhileInteracting => _hideTargetWhileInteracting;
            public GameObject[] AdditionalHideTargets => _additionalHideTargets;
            public GameObject? SortingTarget => _sortingTarget;
            public string FallbackSortingTargetName => _fallbackSortingTargetName;
            public bool UseTargetSortingWhileInteracting => _useTargetSortingWhileInteracting;
            public int SortingOrderOffsetWhileInteracting => _sortingOrderOffsetWhileInteracting;
            public bool UsePetPoseOverride => _usePetPoseOverride;
            public bool UseTargetPositionForPetPose => _useTargetPositionForPetPose;
            public Vector2 PetInteractionLocalOffset => _petInteractionLocalOffset;
            public Vector2 PetInteractionWorldPoint => _petInteractionWorldPoint;
            public Vector3 PetInteractionScale => _petInteractionScale;
            public FurnitureCategory Category => _category;
            public FurnitureInteractionType InteractionType => _interactionType;
            public PetSelfInteractionVariant AnimationVariant => _animationVariant;
            public float InteractionDurationSeconds => Mathf.Max(0.1f, _interactionDurationSeconds);
            public InteractionAnimationOption[] AnimationOptions => _animationOptions;

            public void SetResolvedTarget(GameObject target)
            {
                _target = target;
            }

            public void SetResolvedPoseTarget(GameObject poseTarget)
            {
                _poseTarget = poseTarget;
            }

            public void SetResolvedSortingTarget(GameObject sortingTarget)
            {
                _sortingTarget = sortingTarget;
            }

            public bool TryGetPreferredAnimation(out string animatorStateName, out string variantKey)
            {
                for (int i = 0; i < _animationOptions.Length; i++)
                {
                    InteractionAnimationOption option = _animationOptions[i];
                    if (string.IsNullOrWhiteSpace(option.AnimatorStateName) &&
                        string.IsNullOrWhiteSpace(option.VariantKey))
                    {
                        continue;
                    }

                    animatorStateName = option.AnimatorStateName;
                    variantKey = option.VariantKey;
                    return true;
                }

                animatorStateName = string.Empty;
                variantKey = string.Empty;
                return false;
            }
        }

        [SerializeField] private bool _enableInteraction = true;
        [SerializeField] private KeyCode _interactKey = KeyCode.F;
        [SerializeField] private InteractionBinding[] _bindings = Array.Empty<InteractionBinding>();

        private static int s_lastProcessedInteractFrame = -1;
        // 场景交互入口优先消费 F，避免同一按键同时结算交流和启动家具动画。
        public static event Func<bool>? PriorityInteractRequested;

        public static bool TryHandlePriorityInteraction()
        {
            if (PriorityInteractRequested == null) return false;
            foreach (Func<bool> handler in PriorityInteractRequested.GetInvocationList())
                if (handler()) return true;
            return false;
        }
        private PetController? _petController;

        private void Awake()
        {
            _petController = GetComponent<PetController>();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            foreach (InteractionBinding binding in _bindings)
                if (binding.ApproachPoint != null)
                    Gizmos.DrawWireSphere(binding.ApproachPoint.position, 0.15f);
        }

        private void Update()
        {
            if (GameplayInputBlock.IsBlocked || !_enableInteraction || !isActiveAndEnabled || !Input.GetKeyDown(_interactKey))
            {
                return;
            }

            if (s_lastProcessedInteractFrame == Time.frameCount)
            {
                return;
            }

            s_lastProcessedInteractFrame = Time.frameCount;
            if (TryHandlePriorityInteraction()) return;
            if (!TryHandleGlobalInteractKey(_interactKey))
            {
                Debug.LogWarning($"[PetPlayerFurnitureInteraction] No eligible binding found for key '{_interactKey}' on frame {Time.frameCount}.");
            }
        }

        public bool TryHandleWorldPoint(Vector2 worldPoint)
        {
            if (GameplayInputBlock.IsBlocked) return false;
            if (!_enableInteraction || !isActiveAndEnabled)
            {
                return false;
            }

            if (_petController == null)
            {
                _petController = GetComponent<PetController>();
            }

            if (!TryGetBindingForWorldPoint(transform.position, worldPoint, out InteractionBinding? binding, out string targetName, out GameObject? targetObject))
            {
                return false;
            }

            return TryStartResolvedInteraction(binding, targetName, targetObject, "world point");
        }

        private bool TryGetClosestBinding(Vector2 petPosition, out InteractionBinding? bestBinding, out string bestTargetName, out GameObject? bestTargetObject)
        {
            return TryGetClosestBinding(
                petPosition,
                out bestBinding,
                out bestTargetName,
                out bestTargetObject,
                out _);
        }

        private bool TryGetClosestBinding(
            Vector2 petPosition,
            out InteractionBinding? bestBinding,
            out string bestTargetName,
            out GameObject? bestTargetObject,
            out float bestBindingDistance)
        {
            bestBinding = null;
            bestTargetName = string.Empty;
            bestTargetObject = null;
            bestBindingDistance = float.MaxValue;

            for (int i = 0; i < _bindings.Length; i++)
            {
                InteractionBinding binding = _bindings[i];
                if (!TryResolveInteractionPoint(binding, out Vector2 interactionPoint, out string targetName, out GameObject? targetObject))
                {
                    continue;
                }

                float distance = Vector2.Distance(petPosition, interactionPoint);
                if (distance > binding.ActivationDistance || distance >= bestBindingDistance)
                {
                    continue;
                }

                bestBindingDistance = distance;
                bestBinding = binding;
                bestTargetName = targetName;
                bestTargetObject = targetObject;
            }

            return bestBinding != null && !string.IsNullOrWhiteSpace(bestTargetName);
        }

        /// <summary>
        /// 自动交互专用：与 <see cref="TryGetClosestBinding(Vector2, out InteractionBinding?, out string, out GameObject?)"/>
        /// 相同，但搜索半径在绑定激活距离上增加 <paramref name="radiusPadding"/>。
        /// 手动路径在玩家已走近家具时才判定；自动路径在宠物撞上家具“卡住”时触发，
        /// 宠物可能在大型家具边缘而交互点在家具中心，放宽半径才能命中。
        /// </summary>
        private bool TryGetClosestAutoBinding(
            Vector2 petPosition,
            float radiusPadding,
            out InteractionBinding? bestBinding,
            out string bestTargetName,
            out GameObject? bestTargetObject,
            out float bestBindingDistance)
        {
            bestBinding = null;
            bestTargetName = string.Empty;
            bestTargetObject = null;
            bestBindingDistance = float.MaxValue;

            for (int i = 0; i < _bindings.Length; i++)
            {
                InteractionBinding binding = _bindings[i];
                if (!TryResolveInteractionPoint(binding, out Vector2 interactionPoint, out string targetName, out GameObject? targetObject))
                {
                    continue;
                }

                float distance = Vector2.Distance(petPosition, interactionPoint);
                float activationDistance = binding.ActivationDistance + radiusPadding;
                if (distance > activationDistance || distance >= bestBindingDistance)
                {
                    continue;
                }

                bestBindingDistance = distance;
                bestBinding = binding;
                bestTargetName = targetName;
                bestTargetObject = targetObject;
            }

            return bestBinding != null && !string.IsNullOrWhiteSpace(bestTargetName);
        }

        /// <summary>
        /// 漫游自动交互候选：返回离宠物最近、且在激活距离（含放宽）内的家具绑定。
        /// 与手动 F 键/点击共用同一套绑定（硬编码交互点 + 显式动画状态名），
        /// 因此不依赖 FurnitureService 是否注册了家具——公寓场景里
        /// ApartmentSceneFurnitureBindings 全部解析失败、_placedFurniture 为空，
        /// 只有这套绑定可解析，自动交互必须复用它才能命中并播放正确动画。
        /// </summary>
        public bool TryGetAutoInteractionCandidate(out AutoInteractionCandidate candidate)
        {
            candidate = default;
            if (!_enableInteraction || !isActiveAndEnabled)
            {
                return false;
            }

            if (_petController == null)
            {
                _petController = GetComponent<PetController>();
            }

            if (_petController == null ||
                !TryGetClosestAutoBinding(
                    transform.position,
                    AutoInteractionRadiusPadding,
                    out InteractionBinding? binding,
                    out string targetName,
                    out GameObject? targetObject,
                    out _))
            {
                return false;
            }

            if (binding is null ||
                !TryResolveInteractionPoint(binding, out Vector2 interactionPoint, out _, out _))
            {
                return false;
            }

            candidate = new AutoInteractionCandidate(
                BuildInteractionRequest(binding, targetName, targetObject),
                interactionPoint);
            return true;
        }

        /// <summary>
        /// 行为权重驱动专用：按 <see cref="FurnitureInteractionType"/> 精确查找绑定
        /// （不做距离检查——行为系统会先走到交互点再启动交互）。
        /// 与 <see cref="TryGetAutoInteractionCandidate"/> 返回同样的完整请求与固定交互点。
        /// </summary>
        public bool TryGetInteractionCandidateByType(FurnitureInteractionType interactionType, out AutoInteractionCandidate candidate)
        {
            candidate = default;
            if (!_enableInteraction || !isActiveAndEnabled)
            {
                return false;
            }

            for (int i = 0; i < _bindings.Length; i++)
            {
                InteractionBinding binding = _bindings[i];
                if (binding.InteractionType != interactionType)
                {
                    continue;
                }

                if (!TryResolveInteractionPoint(binding, out Vector2 interactionPoint, out string targetName, out GameObject? targetObject))
                {
                    continue;
                }

                candidate = new AutoInteractionCandidate(
                    BuildInteractionRequest(binding, targetName, targetObject),
                    binding.ApproachPoint != null ? (Vector2)binding.ApproachPoint.position : interactionPoint);
                return true;
            }

            return false;
        }

        private bool TryEnsurePetController(out PetController? petController)
        {
            if (_petController == null)
            {
                _petController = GetComponent<PetController>();
            }

            petController = _petController;
            return petController != null;
        }

        private bool ShouldTraceDevilInteraction()
        {
            return TryEnsurePetController(out PetController? petController) &&
                   petController.PetId == PetId.Devil;
        }

        private void LogDevilTrace(string message)
        {
            if (!ShouldTraceDevilInteraction())
            {
                return;
            }

            Debug.Log($"{DevilFTracePrefix} {message}");
        }

        private void LogDevilTraceWarning(string message)
        {
            if (!ShouldTraceDevilInteraction())
            {
                return;
            }

            Debug.LogWarning($"{DevilFTracePrefix} {message}");
        }

        private bool IsPetCurrentlyControlled()
        {
            return TryEnsurePetController(out PetController? petController) &&
                   petController.IsPlayerControlEnabled;
        }

        private bool TryPromoteToActiveController()
        {
            if (!TryGetComponent(out PetPlayerInputController inputController))
            {
                return false;
            }

            inputController.TakeControl();
            return inputController.IsActiveController;
        }

        private bool TryStartResolvedInteraction(
            InteractionBinding binding,
            string targetName,
            GameObject? targetObject,
            string triggerSource)
        {
            if (!TryEnsurePetController(out PetController? petController))
            {
                Debug.LogWarning($"[PetPlayerFurnitureInteraction] Missing PetController on '{gameObject.name}' for {triggerSource} trigger.");
                return false;
            }

            if (!petController.IsPlayerControlEnabled)
            {
                if (!TryPromoteToActiveController() || !petController.IsPlayerControlEnabled)
                {
                    LogDevilTraceWarning(
                        $"Failed to acquire active control. trigger='{triggerSource}', label='{binding.Label}', target='{targetName}'");
                    Debug.LogWarning(
                        $"[PetPlayerFurnitureInteraction] '{gameObject.name}' could not acquire active control for {triggerSource} trigger.");
                    return false;
                }

                LogDevilTrace(
                    $"Auto-took control. trigger='{triggerSource}', label='{binding.Label}', target='{targetName}'");
                Debug.Log($"[PetPlayerFurnitureInteraction] '{gameObject.name}' auto-took control for {triggerSource} trigger.");
            }

            PetPlayerInteractionRequest request = BuildInteractionRequest(binding, targetName, targetObject);
            Debug.Log(
                $"[PetPlayerFurnitureInteraction] TriggerSource='{triggerSource}' label='{binding.Label}' target='{targetName}' " +
                $"variant='{request.AnimationVariant}' animatorState='{request.AnimatorStateNameOverride}'.");
            bool started = petController.TryStartPlayerInteraction(request);
            LogDevilTrace(
                $"TryStartResolvedInteraction trigger='{triggerSource}', label='{binding.Label}', target='{targetName}', " +
                $"variant='{request.AnimationVariant}', animatorState='{request.AnimatorStateNameOverride}', started={started}");
            return started;
        }

        private static bool TryHandleGlobalInteractKey(KeyCode interactKey)
        {
            if (!TrySelectInteractionCandidate(
                    interactKey,
                    out PetPlayerFurnitureInteractionController? controller,
                    out InteractionBinding? binding,
                    out string targetName,
                    out GameObject? targetObject,
                    out bool selectedFromActivePet))
            {
                return false;
            }

            Debug.Log(
                $"[PetPlayerFurnitureInteraction] Key '{interactKey}' selected pet '{controller.gameObject.name}' " +
                $"via {(selectedFromActivePet ? "active" : "fallback")} candidate.");
            controller.LogDevilTrace(
                $"Selected by F key. label='{binding.Label}', target='{targetName}', " +
                $"via={(selectedFromActivePet ? "active" : "fallback")}");
            return controller.TryStartResolvedInteraction(binding, targetName, targetObject, "F key");
        }

        private static bool TrySelectInteractionCandidate(
            KeyCode interactKey,
            out PetPlayerFurnitureInteractionController? bestController,
            out InteractionBinding? bestBinding,
            out string bestTargetName,
            out GameObject? bestTargetObject,
            out bool selectedFromActivePet)
        {
            bestController = null;
            bestBinding = null;
            bestTargetName = string.Empty;
            bestTargetObject = null;
            selectedFromActivePet = false;
            float bestDistance = float.MaxValue;
            bool foundActivePetCandidate = false;

            PetPlayerFurnitureInteractionController[] controllers =
                FindObjectsByType<PetPlayerFurnitureInteractionController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            for (int i = 0; i < controllers.Length; i++)
            {
                PetPlayerFurnitureInteractionController controller = controllers[i];
                if (controller == null ||
                    !controller.isActiveAndEnabled ||
                    !controller._enableInteraction ||
                    controller._interactKey != interactKey ||
                    !controller.TryEnsurePetController(out _))
                {
                    continue;
                }

                if (!controller.TryGetClosestBinding(
                        controller.transform.position,
                        out InteractionBinding? binding,
                        out string targetName,
                        out GameObject? targetObject,
                        out float distance))
                {
                    continue;
                }

                bool isActivePet = controller.IsPetCurrentlyControlled();
                if (isActivePet)
                {
                    if (!foundActivePetCandidate || distance < bestDistance)
                    {
                        foundActivePetCandidate = true;
                        bestDistance = distance;
                        bestController = controller;
                        bestBinding = binding;
                        bestTargetName = targetName;
                        bestTargetObject = targetObject;
                        selectedFromActivePet = true;
                    }

                    continue;
                }

                if (foundActivePetCandidate || distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestController = controller;
                bestBinding = binding;
                bestTargetName = targetName;
                bestTargetObject = targetObject;
                selectedFromActivePet = false;
            }

            return bestController != null &&
                   bestBinding != null &&
                   !string.IsNullOrWhiteSpace(bestTargetName);
        }

        private static PetPlayerInteractionRequest BuildInteractionRequest(
            InteractionBinding binding,
            string targetName,
            GameObject? targetObject)
        {
            GameObject? poseTarget = ResolvePoseTarget(binding, targetObject);
            GameObject? sortingTarget = binding.UseTargetSortingWhileInteracting
                ? ResolveSortingTarget(binding, poseTarget ?? targetObject)
                : null;

            return new PetPlayerInteractionRequest(
                targetName: !string.IsNullOrWhiteSpace(binding.Label) ? binding.Label : targetName,
                category: binding.Category,
                interactionType: binding.InteractionType,
                animationVariant: ResolveAnimationVariant(binding),
                animatorStateNameOverride: ResolveAnimatorStateOverride(binding),
                hideTargetWhileInteracting: binding.HideTargetWhileInteracting,
                visualHideTarget: binding.HideTargetWhileInteracting ? targetObject : null,
                visualPoseTarget: poseTarget,
                additionalVisualHideTargets: binding.AdditionalHideTargets,
                useTargetSortingWhileInteracting: binding.UseTargetSortingWhileInteracting,
                visualSortingTarget: sortingTarget,
                sortingOrderOffsetWhileInteracting: binding.SortingOrderOffsetWhileInteracting,
                usePetPoseOverride: binding.UsePetPoseOverride,
                useTargetPositionForPetPose: binding.UseTargetPositionForPetPose,
                petInteractionLocalOffset: binding.PetInteractionLocalOffset,
                petInteractionWorldPoint: binding.PetInteractionWorldPoint,
                petInteractionScale: binding.PetInteractionScale,
                interactionDurationSeconds: binding.InteractionDurationSeconds);
        }

        private bool TryGetBindingForWorldPoint(
            Vector2 petPosition,
            Vector2 worldPoint,
            out InteractionBinding? bestBinding,
            out string bestTargetName,
            out GameObject? bestTargetObject)
        {
            bestBinding = null;
            bestTargetName = string.Empty;
            bestTargetObject = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < _bindings.Length; i++)
            {
                InteractionBinding binding = _bindings[i];
                if (!TryResolveInteractionPoint(binding, out Vector2 interactionPoint, out string targetName, out GameObject? targetObject))
                {
                    continue;
                }

                float petDistance = Vector2.Distance(petPosition, interactionPoint);
                if (petDistance > binding.ActivationDistance)
                {
                    continue;
                }

                if (!DoesWorldPointMatchBinding(binding, worldPoint, interactionPoint, targetObject, out float score))
                {
                    continue;
                }

                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestBinding = binding;
                bestTargetName = targetName;
                bestTargetObject = targetObject;
            }

            return bestBinding != null && !string.IsNullOrWhiteSpace(bestTargetName);
        }

        private static string ResolveAnimationVariant(InteractionBinding binding)
        {
            return binding.TryGetPreferredAnimation(out _, out string variantKey) &&
                   !string.IsNullOrWhiteSpace(variantKey)
                ? variantKey
                : binding.AnimationVariant.ToVariantKey();
        }

        private static string ResolveAnimatorStateOverride(InteractionBinding binding)
        {
            return binding.TryGetPreferredAnimation(out string animatorStateName, out _) &&
                   !string.IsNullOrWhiteSpace(animatorStateName)
                ? animatorStateName
                : string.Empty;
        }

        private static GameObject? ResolveTarget(InteractionBinding binding)
        {
            return ResolveSceneObject(binding.Target, binding.FallbackTargetName, binding.SetResolvedTarget);
        }

        private static GameObject? ResolvePoseTarget(InteractionBinding binding, GameObject? defaultTargetObject)
        {
            GameObject? poseTarget = ResolveSceneObject(
                binding.PoseTarget,
                binding.FallbackPoseTargetName,
                binding.SetResolvedPoseTarget);

            return poseTarget ?? defaultTargetObject;
        }

        private static GameObject? ResolveSortingTarget(InteractionBinding binding, GameObject? defaultTargetObject)
        {
            GameObject? sortingTarget = ResolveSceneObject(
                binding.SortingTarget,
                binding.FallbackSortingTargetName,
                binding.SetResolvedSortingTarget);

            return sortingTarget ?? defaultTargetObject;
        }

        private static GameObject? ResolveSceneObject(
            GameObject? directTarget,
            string fallbackTargetName,
            Action<GameObject>? cacheResolvedTarget)
        {
            if (directTarget != null)
            {
                return directTarget;
            }

            if (string.IsNullOrWhiteSpace(fallbackTargetName))
            {
                return null;
            }

            GameObject[] sceneObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < sceneObjects.Length; i++)
            {
                GameObject candidate = sceneObjects[i];
                if (candidate.TryGetComponent(out SceneFurnitureDefinitionHint hint) &&
                    string.Equals(hint.DefinitionId, fallbackTargetName, StringComparison.Ordinal))
                {
                    cacheResolvedTarget?.Invoke(candidate);
                    return candidate;
                }

                if (candidate.TryGetComponent(out SpriteRenderer renderer) &&
                    renderer.sprite != null &&
                    string.Equals(renderer.sprite.name, fallbackTargetName, StringComparison.Ordinal))
                {
                    cacheResolvedTarget?.Invoke(candidate);
                    return candidate;
                }

                if (string.Equals(candidate.name, fallbackTargetName, StringComparison.Ordinal))
                {
                    cacheResolvedTarget?.Invoke(candidate);
                    return candidate;
                }
            }

            for (int i = 0; i < sceneObjects.Length; i++)
            {
                GameObject candidate = sceneObjects[i];
                if (candidate.TryGetComponent(out SceneFurnitureDefinitionHint hint) &&
                    hint.DefinitionId.Contains(fallbackTargetName, StringComparison.Ordinal))
                {
                    cacheResolvedTarget?.Invoke(candidate);
                    return candidate;
                }

                if (candidate.TryGetComponent(out SpriteRenderer renderer) &&
                    renderer.sprite != null &&
                    renderer.sprite.name.Contains(fallbackTargetName, StringComparison.Ordinal))
                {
                    cacheResolvedTarget?.Invoke(candidate);
                    return candidate;
                }

                if (candidate.name.Contains(fallbackTargetName, StringComparison.Ordinal))
                {
                    cacheResolvedTarget?.Invoke(candidate);
                    return candidate;
                }
            }

            return null;
        }

        private static bool TryResolveInteractionPoint(InteractionBinding binding, out Vector2 interactionPoint, out string targetName, out GameObject? targetObject)
        {
            if (binding.UseFallbackWorldPoint)
            {
                interactionPoint = binding.FallbackWorldPoint;
                targetObject = ResolveTarget(binding);
                if (targetObject != null)
                {
                    targetName = targetObject.name;
                    return true;
                }

                targetName = !string.IsNullOrWhiteSpace(binding.Label) ? binding.Label : binding.FallbackTargetName;
                return true;
            }

            targetObject = ResolveTarget(binding);
            if (targetObject != null)
            {
                interactionPoint = targetObject.transform.position;
                targetName = targetObject.name;
                return true;
            }

            interactionPoint = default;
            targetName = string.Empty;
            targetObject = null;
            return false;
        }

        private static bool DoesWorldPointMatchBinding(
            InteractionBinding binding,
            Vector2 worldPoint,
            Vector2 interactionPoint,
            GameObject? targetObject,
            out float score)
        {
            if (targetObject != null)
            {
                Collider2D[] colliders = targetObject.GetComponentsInChildren<Collider2D>(true);
                for (int i = 0; i < colliders.Length; i++)
                {
                    Collider2D collider = colliders[i];
                    if (collider != null && collider.OverlapPoint(worldPoint))
                    {
                        score = 0f;
                        return true;
                    }
                }
            }

            float hitRadius = Mathf.Max(0.35f, binding.ActivationDistance * 0.5f);
            score = Vector2.Distance(worldPoint, interactionPoint);
            return score <= hitRadius;
        }
    }

    /// <summary>
    /// 漫游自动交互的家具候选。由 <see cref="PetPlayerFurnitureInteractionController.TryGetAutoInteractionCandidate"/>
    /// 产生，携带与该宠物手动交互绑定完全一致的完整请求（显式动画状态、变体、时长、pose 数据）
    /// 以及绑定解析出的固定交互点。
    /// </summary>
    public readonly struct AutoInteractionCandidate
    {
        public AutoInteractionCandidate(PetPlayerInteractionRequest request, Vector2 interactionPoint)
        {
            Request = request;
            InteractionPoint = interactionPoint;
        }

        /// <summary>与手动路径一致、由绑定构建的完整交互请求（含显式动画状态与 pose 数据）。</summary>
        public PetPlayerInteractionRequest Request { get; }

        /// <summary>绑定解析出的固定交互点（硬编码 fallbackWorldPoint 或目标物体位置）。</summary>
        public Vector2 InteractionPoint { get; }
    }

    public readonly struct PetPlayerInteractionRequest
    {
        public PetPlayerInteractionRequest(
            string targetName,
            FurnitureCategory category,
            FurnitureInteractionType interactionType,
            string animationVariant,
            string animatorStateNameOverride,
            bool hideTargetWhileInteracting,
            GameObject? visualHideTarget,
            GameObject? visualPoseTarget,
            GameObject[]? additionalVisualHideTargets,
            bool useTargetSortingWhileInteracting,
            GameObject? visualSortingTarget,
            int sortingOrderOffsetWhileInteracting,
            bool usePetPoseOverride,
            bool useTargetPositionForPetPose,
            Vector2 petInteractionLocalOffset,
            Vector2 petInteractionWorldPoint,
            Vector3 petInteractionScale,
            float interactionDurationSeconds)
        {
            TargetName = targetName;
            Category = category;
            InteractionType = interactionType;
            AnimationVariant = animationVariant;
            AnimatorStateNameOverride = animatorStateNameOverride;
            HideTargetWhileInteracting = hideTargetWhileInteracting;
            VisualHideTarget = visualHideTarget;
            VisualPoseTarget = visualPoseTarget;
            AdditionalVisualHideTargets = additionalVisualHideTargets ?? Array.Empty<GameObject>();
            UseTargetSortingWhileInteracting = useTargetSortingWhileInteracting;
            VisualSortingTarget = visualSortingTarget;
            SortingOrderOffsetWhileInteracting = sortingOrderOffsetWhileInteracting;
            UsePetPoseOverride = usePetPoseOverride;
            UseTargetPositionForPetPose = useTargetPositionForPetPose;
            PetInteractionLocalOffset = petInteractionLocalOffset;
            PetInteractionWorldPoint = petInteractionWorldPoint;
            PetInteractionScale = petInteractionScale;
            InteractionDurationSeconds = interactionDurationSeconds;
        }

        public string TargetName { get; }
        public FurnitureCategory Category { get; }
        public FurnitureInteractionType InteractionType { get; }
        public string AnimationVariant { get; }
        public string AnimatorStateNameOverride { get; }
        public bool HideTargetWhileInteracting { get; }
        public GameObject? VisualHideTarget { get; }
        public GameObject? VisualPoseTarget { get; }
        public GameObject[] AdditionalVisualHideTargets { get; }
        public bool UseTargetSortingWhileInteracting { get; }
        public GameObject? VisualSortingTarget { get; }
        public int SortingOrderOffsetWhileInteracting { get; }
        public bool UsePetPoseOverride { get; }
        public bool UseTargetPositionForPetPose { get; }
        public Vector2 PetInteractionLocalOffset { get; }
        public Vector2 PetInteractionWorldPoint { get; }
        public Vector3 PetInteractionScale { get; }
        public float InteractionDurationSeconds { get; }
    }

    public enum PetSelfInteractionVariant
    {
        BesideDoor = 0,
        Flower = 1,
        PlayingMusic = 2,
        Read = 3,
        Sleep = 4,
        LookAround = 5,
        PlayGame = 6,
        Draw = 7,
        DevilSleep = 8
    }

    public static class PetSelfInteractionVariantExtensions
    {
        public static string ToVariantKey(this PetSelfInteractionVariant variant)
        {
            return variant switch
            {
                PetSelfInteractionVariant.BesideDoor => "beside door",
                PetSelfInteractionVariant.Flower => "flower",
                PetSelfInteractionVariant.PlayingMusic => "playing music",
                PetSelfInteractionVariant.Read => "read",
                PetSelfInteractionVariant.Sleep => "sleep",
                PetSelfInteractionVariant.LookAround => "look around",
                PetSelfInteractionVariant.PlayGame => "play game",
                PetSelfInteractionVariant.Draw => "draw",
                PetSelfInteractionVariant.DevilSleep => "devil sleep",
                _ => "read"
            };
        }
    }
}
