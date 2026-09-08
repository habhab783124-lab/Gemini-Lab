#nullable enable
using System;
using GeminiLab.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// WorldMap 专用点击目标。点击范围由目标自身的可见 Sprite 或作者化 Collider 决定。
    /// </summary>
    /// <summary>
    /// 只在 Scene 中明确登记的 WorldMap 交互目标之间裁决点击。
    /// 背景、基线、种植区域等非交互 Collider 不再吞掉标牌和邮箱点击。
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-500)]
    public sealed class WorldMapSceneInteractionRouter : MonoBehaviour
    {
        [SerializeField] private Camera? _worldCamera;
        [SerializeField] private MonoBehaviour[] _targets = Array.Empty<MonoBehaviour>();

        private static WorldMapSceneInteractionRouter? s_active;

        public static WorldMapSceneInteractionRouter? Active => s_active;

        private void Awake()
        {
            _worldCamera ??= Camera.main;
            s_active = this;
        }

        private void OnEnable()
        {
            s_active = this;
        }

        private void OnDisable()
        {
            if (ReferenceEquals(s_active, this)) s_active = null;
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            Vector2 screenPoint = Input.mousePosition;
            bool blockedByUi = IsPointerBlockedByUI();
            Debug.Log(
                $"[WorldMapClick][Input] screen={screenPoint} blockedByUI={blockedByUi}",
                this);

            if (blockedByUi) return;
            TryHandleScreenPoint(screenPoint);
        }

        public bool TryHandleScreenPoint(Vector2 screenPoint)
        {
            if (!TryGetTargetAtScreenPoint(screenPoint, out IWorldMapSceneClickTarget? target) || target == null)
            {
                Debug.LogWarning($"[WorldMapClick][Miss] screen={screenPoint} target=<none>", this);
                return false;
            }

            bool invoked = InvokeTarget(target);
            Debug.Log(
                $"[WorldMapClick][Invoke] target={DescribeTarget(target)} invoked={invoked}",
                this);
            return invoked;
        }

        public bool TryGetTargetAtScreenPoint(
            Vector2 screenPoint,
            out IWorldMapSceneClickTarget? target)
        {
            Camera? camera = _worldCamera != null ? _worldCamera : Camera.main;
            if (camera == null)
            {
                target = null;
                Debug.LogWarning("[WorldMapClick][Camera] no WorldMap camera is available.", this);
                return false;
            }

            Vector3 world = camera.ScreenToWorldPoint(screenPoint);
            bool found = TryGetTargetAtWorldPoint(world, out target);
            Debug.Log(
                $"[WorldMapClick][HitTest] screen={screenPoint} world={world} found={found} target={DescribeTarget(target)}",
                this);
            return found;
        }

        public bool TryGetTargetAtWorldPoint(
            Vector2 worldPoint,
            out IWorldMapSceneClickTarget? target)
        {
            target = null;
            InteractionPriority best = default;
            bool found = false;

            for (int index = 0; index < _targets.Length; index++)
            {
                if (_targets[index] is not IWorldMapSceneClickTarget candidate ||
                    !candidate.IsWorldMapInteractionEnabled ||
                    !candidate.ContainsWorldPoint(worldPoint))
                {
                    continue;
                }

                InteractionPriority priority = ResolvePriority(candidate);
                if (found && Compare(priority, best) <= 0) continue;
                found = true;
                best = priority;
                target = candidate;
            }

            return found && target != null;
        }

        public bool IsRegistered(IWorldMapSceneClickTarget target)
        {
            for (int index = 0; index < _targets.Length; index++)
            {
                if (ReferenceEquals(_targets[index], target)) return true;
            }

            return false;
        }

        public bool IsPointerBlockedByUI()
        {
            EventSystem? eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            var eventData = new PointerEventData(eventSystem) { position = Input.mousePosition };
            var results = new System.Collections.Generic.List<RaycastResult>();
            eventSystem.RaycastAll(eventData, results);
            for (int index = 0; index < results.Count; index++)
            {
                if (IsVisibleUiGraphic(results[index].gameObject))
                {
                    Debug.LogWarning(
                        $"[WorldMapClick][UIBlocked] object={results[index].gameObject.name}",
                        results[index].gameObject);
                    return true;
                }
            }

            return false;
        }

        private static bool IsVisibleUiGraphic(GameObject? hit)
        {
            if (hit == null || !hit.activeInHierarchy) return false;

            Graphic? graphic = hit.GetComponent<Graphic>();
            if (graphic == null || !graphic.isActiveAndEnabled || !graphic.raycastTarget)
            {
                return false;
            }

            float effectiveAlpha = graphic.color.a;
            CanvasGroup[] groups = hit.GetComponentsInParent<CanvasGroup>(true);
            for (int index = 0; index < groups.Length; index++)
            {
                CanvasGroup group = groups[index];
                if (!group.isActiveAndEnabled) return false;
                effectiveAlpha *= group.alpha;
            }

            return effectiveAlpha > 0.001f && !graphic.canvasRenderer.cull;
        }

        private static bool InvokeTarget(IWorldMapSceneClickTarget target)
        {
            if (!target.IsWorldMapInteractionEnabled)
            {
                Debug.LogWarning(
                    $"[WorldMapClick][DisabledTarget] target={DescribeTarget(target)}",
                    target as UnityEngine.Object);
                return false;
            }

            target.HandleWorldMapClick();
            return true;
        }

        private static string DescribeTarget(IWorldMapSceneClickTarget? target)
        {
            if (target is MonoBehaviour behaviour && behaviour != null)
            {
                return $"{behaviour.name} ({behaviour.GetType().Name})";
            }

            return target?.GetType().Name ?? "<none>";
        }

        private static InteractionPriority ResolvePriority(IWorldMapSceneClickTarget target)
        {
            Renderer? renderer = target.WorldMapSortingRenderer;
            if (renderer == null)
            {
                return new InteractionPriority(target.WorldMapInteractionPriority, 0, 0, 0f);
            }

            int layerValue = SortingLayer.GetLayerValueFromID(renderer.sortingLayerID);
            return new InteractionPriority(
                target.WorldMapInteractionPriority,
                layerValue,
                renderer.sortingOrder,
                -renderer.transform.position.z);
        }

        private static int Compare(InteractionPriority left, InteractionPriority right)
        {
            int value = left.Explicit.CompareTo(right.Explicit);
            if (value != 0) return value;
            value = left.SortingLayer.CompareTo(right.SortingLayer);
            if (value != 0) return value;
            value = left.SortingOrder.CompareTo(right.SortingOrder);
            if (value != 0) return value;
            return left.FrontDepth.CompareTo(right.FrontDepth);
        }

        private readonly struct InteractionPriority
        {
            public InteractionPriority(int explicitPriority, int sortingLayer, int sortingOrder, float frontDepth)
            {
                Explicit = explicitPriority;
                SortingLayer = sortingLayer;
                SortingOrder = sortingOrder;
                FrontDepth = frontDepth;
            }

            public int Explicit { get; }
            public int SortingLayer { get; }
            public int SortingOrder { get; }
            public float FrontDepth { get; }
        }
    }
}
