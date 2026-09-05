#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core;
using UnityEngine;

namespace GeminiLab.Modules.Apple
{
    /// <summary>
    /// Coordinates one authored apple tree's shake and ground-drop slots.
    /// The service owns the currency total; this component owns presentation.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(2000)]
    public sealed class AppleTreeDropController : MonoBehaviour
    {
        [SerializeField] private string _treeId = string.Empty;
        [SerializeField] private Transform? _shakeTarget;
        [SerializeField] private Vector2 _shakeLocalPivot;
        [SerializeField] private AppleTreeFeedback? _feedback;
        [SerializeField] private AppleDropSlot[] _dropSlots = Array.Empty<AppleDropSlot>();

        [Header("快速晃动")]
        [SerializeField, Min(1)] private int _shakeCycles = 6;
        [SerializeField, Range(0f, 30f)] private float _shakeAmplitudeDegrees = 9f;
        [SerializeField, Min(0.05f)] private float _shakeDurationSeconds = 0.58f;

        [Header("苹果掉落")]
        [SerializeField, Range(1, 3)] private int _minDropCount = 1;
        [SerializeField, Range(1, 3)] private int _maxDropCount = 3;
        [SerializeField, Min(0f)] private float _dropStartHeight = 1.7f;
        [SerializeField, Min(0.05f)] private float _dropDurationSeconds = 0.28f;

        private IAppleService? _appleService;
        private float _shakeElapsed;
        private bool _isShaking;
        private Quaternion _shakeBaseRotation;
        private Vector3 _shakeBasePosition;
        private bool _hasShakeBase;

        public string TreeId => _treeId;
        public bool IsShaking => _isShaking;

        private void Awake()
        {
            _shakeTarget ??= transform;
            if (string.IsNullOrWhiteSpace(_treeId) &&
                TryGetComponent(out AppleTreeInteractable? interactable) && interactable != null)
            {
                _treeId = interactable.TreeId;
            }

            for (int index = 0; index < _dropSlots.Length; index++)
            {
                _dropSlots[index]?.HideImmediate();
            }
        }

        private void OnEnable()
        {
            _shakeTarget ??= transform;
        }

        private void LateUpdate()
        {
            if (!_isShaking || _shakeTarget == null) return;

            _shakeElapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(_shakeElapsed / Mathf.Max(0.05f, _shakeDurationSeconds));
            float envelope = 1f - normalized;
            float oscillation = Mathf.Sin(normalized * Mathf.Max(1, _shakeCycles) * Mathf.PI * 2f);
            ApplyShake(oscillation * envelope * _shakeAmplitudeDegrees);

            if (normalized >= 1f)
            {
                // Leave the authored transform exactly where it was before the
                // interaction. This prevents frame-to-frame rotation drift and
                // keeps the tree's baseline/ambient pose authoritative.
                _shakeTarget.rotation = _shakeBaseRotation;
                _shakeTarget.position = _shakeBasePosition;
                _isShaking = false;
                _hasShakeBase = false;
            }
        }

        public bool TryBeginHarvest()
        {
            if (HasActiveDrops()) return false;
            List<AppleDropSlot> availableSlots = GetAvailableSlots();
            if (availableSlots.Count == 0)
            {
                Debug.LogError($"[AppleTreeDrop] {_treeId} 没有作者化的掉落槽位。", this);
                _feedback?.ShowNotReady();
                return false;
            }

            _appleService ??= ServiceLocator.TryResolve(out IAppleService? service) ? service : null;
            if (_appleService == null)
            {
                Debug.LogWarning($"[AppleTreeDrop] {_treeId} 未找到 IAppleService。", this);
                _feedback?.ShowNotReady();
                return false;
            }

            if (!_appleService.TryBeginHarvest(_treeId, out int total) || total <= 0)
            {
                _feedback?.ShowNotReady();
                return false;
            }

            int maxDrops = Mathf.Min(3, availableSlots.Count, total);
            int minDrops = Mathf.Clamp(_minDropCount, 1, maxDrops);
            int upperDrops = Mathf.Max(minDrops, Mathf.Min(_maxDropCount, maxDrops));
            int dropCount = UnityEngine.Random.Range(minDrops, upperDrops + 1);
            int[] allocations = SplitTotal(total, dropCount);

            for (int index = 0; index < dropCount; index++)
            {
                availableSlots[index].BeginDrop(
                    this,
                    allocations[index],
                    _dropStartHeight,
                    _dropDurationSeconds);
            }

            _shakeBaseRotation = _shakeTarget != null ? _shakeTarget.rotation : transform.rotation;
            _shakeBasePosition = _shakeTarget != null ? _shakeTarget.position : transform.position;
            _hasShakeBase = true;
            _shakeElapsed = 0f;
            _isShaking = true;
            return true;
        }

        internal void CollectDrop(AppleDropSlot slot)
        {
            if (slot == null || !slot.IsOccupied || slot.Amount <= 0) return;
            _appleService ??= ServiceLocator.TryResolve(out IAppleService? service) ? service : null;
            if (_appleService == null) return;

            if (_appleService.TryCollectHarvest(_treeId, slot.Amount))
            {
                slot.CompleteCollection(slot.Amount);
            }
        }

        private bool HasActiveDrops()
        {
            for (int index = 0; index < _dropSlots.Length; index++)
            {
                if (_dropSlots[index] != null && _dropSlots[index].IsOccupied) return true;
            }

            return false;
        }

        private List<AppleDropSlot> GetAvailableSlots()
        {
            var result = new List<AppleDropSlot>(_dropSlots.Length);
            for (int index = 0; index < _dropSlots.Length; index++)
            {
                AppleDropSlot? slot = _dropSlots[index];
                if (slot != null && !slot.IsOccupied) result.Add(slot);
            }

            return result;
        }

        private void ApplyShake(float angle)
        {
            Transform target = _shakeTarget!;
            Quaternion baseRotation = _hasShakeBase ? _shakeBaseRotation : target.rotation;
            Vector3 basePosition = _hasShakeBase ? _shakeBasePosition : target.position;
            Vector3 scaledPivot = Vector3.Scale((Vector3)_shakeLocalPivot, target.lossyScale);
            Vector3 pivotWorld = basePosition + baseRotation * scaledPivot;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward) * baseRotation;
            target.rotation = rotation;
            target.position = pivotWorld - rotation * scaledPivot;
        }

        private static int[] SplitTotal(int total, int count)
        {
            var values = new int[count];
            int remaining = total;
            for (int index = 0; index < count - 1; index++)
            {
                int slotsAfter = count - index - 1;
                int value = UnityEngine.Random.Range(1, remaining - slotsAfter + 1);
                values[index] = value;
                remaining -= value;
            }

            values[count - 1] = remaining;

            // Shuffle so the largest remainder is not always the last slot.
            for (int index = values.Length - 1; index > 0; index--)
            {
                int swapIndex = UnityEngine.Random.Range(0, index + 1);
                (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
            }

            return values;
        }
    }
}
