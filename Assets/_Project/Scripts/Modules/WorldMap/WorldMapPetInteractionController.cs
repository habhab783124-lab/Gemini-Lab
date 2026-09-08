#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core;
using GeminiLab.Modules.Pet;
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    public enum WorldMapPetRole
    {
        Angel = 0,
        Devil = 1
    }

    // 保留给现有 EditMode 规则测试和 WorldMap 规则文档使用。
    public enum WorldMapPetBaseAnimation
    {
        Idle = 0,
        Move = 1
    }

    public enum WorldMapPetSpecialAction
    {
        AngelSit = 0,
        AngelPray = 1,
        AngelWater = 2,
        AngelHappy = 3,
        DevilSleep = 4,
        DevilCast = 5,
        DevilProud = 6
    }

    /// <summary>
    /// WorldMap 桌宠的纯规则入口。普通 Idle/Move、漫游、玩家移动和桥面高度均由
    /// PetController、RandomWander、PetPlayerInputController 与 WalkableSurface 负责。
    /// </summary>
    public static class WorldMapPetInteractionRules
    {
        public static WorldMapPetBaseAnimation ResolveBaseAnimation(float horizontalSpeed)
        {
            return Mathf.Abs(horizontalSpeed) > 0.0001f
                ? WorldMapPetBaseAnimation.Move
                : WorldMapPetBaseAnimation.Idle;
        }

        public static bool IsActionAllowed(WorldMapPetRole role, WorldMapPetSpecialAction action)
        {
            return role == WorldMapPetRole.Angel
                ? action is WorldMapPetSpecialAction.AngelSit or
                    WorldMapPetSpecialAction.AngelPray or
                    WorldMapPetSpecialAction.AngelWater or
                    WorldMapPetSpecialAction.AngelHappy
                : action is WorldMapPetSpecialAction.DevilSleep or
                    WorldMapPetSpecialAction.DevilCast or
                    WorldMapPetSpecialAction.DevilProud;
        }

        public static float DistanceToCollider(Vector2 point, Collider2D? collider)
        {
            if (collider == null || !collider.enabled) return float.PositiveInfinity;
            return Vector2.Distance(point, collider.ClosestPoint(point));
        }

        public static float DistanceToBounds(Vector2 point, Bounds bounds)
        {
            Vector3 nearest = bounds.ClosestPoint(new Vector3(point.x, point.y, bounds.center.z));
            return Vector2.Distance(point, nearest);
        }
    }

    /// <summary>
    /// WorldMap 专用桌宠点击目标。
    ///
    /// 该组件只负责：
    /// 1. 使用作者化的独立点击 Collider2D 命中整只桌宠；
    /// 2. 将玩家控制权交给该桌宠的 PetPlayerInputController；
    /// 3. 为 WorldMapCameraController 提供当前选中桌宠。
    ///
    /// 它不直接写 Transform/Rigidbody2D，不接管普通 Idle/Move，也不播放特殊动画。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldMapPetInteractionController : MonoBehaviour, IWorldMapSceneClickTarget
    {
        private static readonly List<WorldMapPetInteractionController> Instances = new();

        [Header("Identity")]
        [SerializeField] private WorldMapPetRole _role;

        [Header("Authored scene references")]
        [SerializeField] private Collider2D? _clickCollider;
        [SerializeField] private SpriteRenderer? _spriteRenderer;
        [SerializeField] private PetPlayerInputController? _playerInputController;
        [SerializeField] private PetController? _petController;

        [Header("WorldMap routing")]
        [SerializeField] private int _interactionPriority = 20;

        public static WorldMapPetInteractionController? Active { get; private set; }
        public static Transform? ActiveTransform => Active != null ? Active.transform : null;

        public WorldMapPetRole Role => _role;
        public bool IsPlayerControlled => _playerInputController != null && _playerInputController.InputEnabled;
        public bool IsSpecialActionActive => _petController != null && _petController.IsMovementLocked;
        public bool IsWorldMapInteractionEnabled => isActiveAndEnabled;
        public int WorldMapInteractionPriority => _interactionPriority;
        public Renderer? WorldMapSortingRenderer => _spriteRenderer;

        private void Awake()
        {
            _clickCollider ??= GetComponent<Collider2D>();
            _spriteRenderer ??= GetComponent<SpriteRenderer>();
            _playerInputController ??= GetComponent<PetPlayerInputController>();
            _petController ??= GetComponent<PetController>();

            if (_clickCollider == null)
            {
                Debug.LogError($"[WorldMapPetInteraction] '{name}' 缺少独立点击 Collider2D。", this);
            }

            if (_playerInputController == null)
            {
                Debug.LogError($"[WorldMapPetInteraction] '{name}' 缺少 PetPlayerInputController。", this);
            }

            if (_petController == null)
            {
                Debug.LogError($"[WorldMapPetInteraction] '{name}' 缺少 PetController。", this);
            }
        }

        private void OnEnable()
        {
            if (!Instances.Contains(this)) Instances.Add(this);
        }

        private void OnDisable()
        {
            Instances.Remove(this);
            if (!ReferenceEquals(Active, this)) return;

            PetPlayerInputController.ReleaseAllControl();
            Active = null;
        }

        public static void ReleaseAllControl()
        {
            PetPlayerInputController.ReleaseAllControl();
            Active = null;
        }

        public bool ContainsWorldPoint(Vector2 worldPoint)
        {
            _clickCollider ??= GetComponent<Collider2D>();
            return _clickCollider != null &&
                   _clickCollider.enabled &&
                   _clickCollider.OverlapPoint(worldPoint);
        }

        public void HandleWorldMapClick()
        {
            if (_playerInputController == null)
            {
                Debug.LogWarning($"[WorldMapPetInteraction] '{name}' cannot take control without PetPlayerInputController.", this);
                return;
            }

            _playerInputController.TakeControl();
            if (_playerInputController.IsActiveController)
            {
                Active = this;
            }
        }
    }
}
