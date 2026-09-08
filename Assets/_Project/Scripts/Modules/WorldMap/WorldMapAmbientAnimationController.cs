#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// Plays the authored ambient motion for the outdoor WorldMap.  All visual
    /// targets are serialized by the editor authoring pass; this component only
    /// animates their existing transforms at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1500)]
    public sealed class WorldMapAmbientAnimationController : MonoBehaviour
    {
        [Serializable]
        public sealed class TreeBinding
        {
            [SerializeField] private Transform? _target;
            [SerializeField] private Vector2 _localPivot;

            public TreeBinding()
            {
            }

            public TreeBinding(Transform target, Vector2 localPivot)
            {
                _target = target;
                _localPivot = localPivot;
            }

            public Transform? Target => _target;

            public Vector2 LocalPivot => _localPivot;
        }

        private sealed class FlowerPose
        {
            public FlowerPose(Transform target, Quaternion baseRotation, float phase)
            {
                Target = target;
                BaseRotation = baseRotation;
                Phase = phase;
            }

            public Transform Target { get; }

            public Quaternion BaseRotation { get; }

            public float Phase { get; }
        }

        private sealed class TreePose
        {
            public TreePose(Transform target, Vector2 localPivot, Vector3 basePosition,
                Quaternion baseRotation, Vector3 baseScale, float phase)
            {
                Target = target;
                LocalPivot = localPivot;
                BasePosition = basePosition;
                BaseRotation = baseRotation;
                BaseScale = baseScale;
                Phase = phase;
            }

            public Transform Target { get; }

            public Vector2 LocalPivot { get; }

            public Vector3 BasePosition { get; }

            public Quaternion BaseRotation { get; }

            public Vector3 BaseScale { get; }

            public float Phase { get; }
        }

        [Header("作者化引用")]
        [SerializeField] private SpriteRenderer? _cloudRenderer;
        [SerializeField] private SpriteRenderer? _skyRenderer;
        [SerializeField] private Transform? _flowerVisualRoot;
        [SerializeField] private TreeBinding[] _treeBindings = Array.Empty<TreeBinding>();

        [Header("云层")]
        [SerializeField, Min(0f)] private float _cloudMoveSpeed = 0.06f;

        [Header("单朵花")]
        [SerializeField, Range(0f, 15f)] private float _singleFlowerRotationAngle = 8f;
        [SerializeField, Min(0f)] private float _singleFlowerRotationSpeed = 1.4f;

        [Header("树木")]
        [SerializeField, Range(0f, 8f)] private float _treeRotationAngle = 2.6f;
        [SerializeField, Min(0f)] private float _treeRotationSpeed = 0.5f;

        private Vector3 _cloudBaseLocalPosition;
        private float _cloudMoveCenterLocalX;
        private float _cloudMoveHalfRangeLocalX;
        private bool _cloudMotionConfigured;
        private readonly List<FlowerPose> _flowerPoses = new();
        private readonly List<TreePose> _treePoses = new();

        public int SingleFlowerVisualCount => _flowerPoses.Count;

        public int TreeCount => _treePoses.Count;

        public void ConfigureForAuthoring(
            SpriteRenderer? cloudRenderer,
            Transform? flowerVisualRoot,
            IReadOnlyList<TreeBinding> treeBindings)
        {
            _cloudRenderer = cloudRenderer;
            _flowerVisualRoot = flowerVisualRoot;
            _treeBindings = new TreeBinding[treeBindings.Count];
            for (int index = 0; index < treeBindings.Count; index++)
            {
                _treeBindings[index] = treeBindings[index];
            }

            CacheAuthoredPoses();
        }

        private void Awake()
        {
            CacheAuthoredPoses();
        }

        private void OnEnable()
        {
            CacheAuthoredPoses();
        }

        private void Update()
        {
            float time = Time.unscaledTime;
            AnimateCloud(time);
            AnimateSingleFlowers(time);
            AnimateTrees(time);
        }

        private void CacheAuthoredPoses()
        {
            _cloudBaseLocalPosition = _cloudRenderer != null
                ? _cloudRenderer.transform.localPosition
                : Vector3.zero;
            CacheCloudMotionBounds();

            _flowerPoses.Clear();
            if (_flowerVisualRoot != null)
            {
                Transform[] visuals = _flowerVisualRoot.GetComponentsInChildren<Transform>(true);
                for (int index = 0; index < visuals.Length; index++)
                {
                    Transform visual = visuals[index];
                    // Placement authoring names every authored visual with a
                    // _Single/_Cluster suffix. Only single flowers animate;
                    // clusters must remain completely still.
                    if (!visual.name.EndsWith("_Single", StringComparison.Ordinal)) continue;
                    if (visual.GetComponentInChildren<SpriteRenderer>(true) == null) continue;
                    _flowerPoses.Add(new FlowerPose(
                        visual,
                        visual.localRotation,
                        StablePhase(GetHierarchyPath(visual), index)));
                }
            }

            _treePoses.Clear();
            for (int index = 0; index < _treeBindings.Length; index++)
            {
                TreeBinding binding = _treeBindings[index];
                if (binding == null || binding.Target == null) continue;
                Transform target = binding.Target;
                _treePoses.Add(new TreePose(
                    target,
                    binding.LocalPivot,
                    target.position,
                    target.rotation,
                    target.lossyScale,
                    StablePhase(target.name, index + 1000)));
            }
        }

        private void CacheCloudMotionBounds()
        {
            _cloudMotionConfigured = false;
            if (_cloudRenderer == null || _skyRenderer == null) return;

            Bounds skyBounds = _skyRenderer.bounds;
            Bounds cloudBounds = _cloudRenderer.bounds;
            float cloudHalfWidth = cloudBounds.extents.x;
            float minWorldX = skyBounds.min.x + cloudHalfWidth;
            float maxWorldX = skyBounds.max.x - cloudHalfWidth;
            if (maxWorldX <= minWorldX) return;

            Transform? cloudParent = _cloudRenderer.transform.parent;
            Vector3 minWorldPoint = _cloudRenderer.transform.position;
            minWorldPoint.x = minWorldX;
            Vector3 maxWorldPoint = _cloudRenderer.transform.position;
            maxWorldPoint.x = maxWorldX;

            if (cloudParent == null)
            {
                _cloudMoveCenterLocalX = (minWorldPoint.x + maxWorldPoint.x) * 0.5f;
                _cloudMoveHalfRangeLocalX = (maxWorldPoint.x - minWorldPoint.x) * 0.5f;
            }
            else
            {
                float minLocalX = cloudParent.InverseTransformPoint(minWorldPoint).x;
                float maxLocalX = cloudParent.InverseTransformPoint(maxWorldPoint).x;
                _cloudMoveCenterLocalX = (minLocalX + maxLocalX) * 0.5f;
                _cloudMoveHalfRangeLocalX = (maxLocalX - minLocalX) * 0.5f;
            }

            _cloudMotionConfigured = _cloudMoveHalfRangeLocalX > Mathf.Epsilon;
        }

        private void AnimateCloud(float time)
        {
            if (_cloudRenderer == null || !_cloudMotionConfigured) return;

            Vector3 position = _cloudBaseLocalPosition;
            position.x = _cloudMoveCenterLocalX
                + Mathf.Sin(time * _cloudMoveSpeed) * _cloudMoveHalfRangeLocalX;
            _cloudRenderer.transform.localPosition = position;
        }

        private void AnimateSingleFlowers(float time)
        {
            float angle = _singleFlowerRotationAngle;
            if (angle <= 0f || _singleFlowerRotationSpeed <= 0f) return;

            for (int index = 0; index < _flowerPoses.Count; index++)
            {
                FlowerPose pose = _flowerPoses[index];
                if (pose.Target == null) continue;
                float z = Mathf.Sin(time * _singleFlowerRotationSpeed + pose.Phase) * angle;
                pose.Target.localRotation = Quaternion.AngleAxis(z, Vector3.forward) * pose.BaseRotation;
            }
        }

        private void AnimateTrees(float time)
        {
            float angle = _treeRotationAngle;
            if (angle <= 0f || _treeRotationSpeed <= 0f) return;

            for (int index = 0; index < _treePoses.Count; index++)
            {
                TreePose pose = _treePoses[index];
                if (pose.Target == null) continue;

                float z = Mathf.Sin(time * _treeRotationSpeed + pose.Phase) * angle;
                Quaternion rotation = Quaternion.AngleAxis(z, Vector3.forward) * pose.BaseRotation;
                Vector3 scaledPivot = Vector3.Scale((Vector3)pose.LocalPivot, pose.BaseScale);
                Vector3 pivotWorld = pose.BasePosition + pose.BaseRotation * scaledPivot;
                pose.Target.rotation = rotation;
                pose.Target.position = pivotWorld - rotation * scaledPivot;
            }
        }

        private static float StablePhase(string value, int salt)
        {
            unchecked
            {
                uint hash = 2166136261u + (uint)salt;
                for (int index = 0; index < value.Length; index++)
                {
                    hash ^= value[index];
                    hash *= 16777619u;
                }

                return (hash & 0x00ffffffu) / 16777215f * Mathf.PI * 2f;
            }
        }

        private static string GetHierarchyPath(Transform target)
        {
            var names = new List<string>();
            Transform? current = target;
            while (current != null)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }
    }
}
