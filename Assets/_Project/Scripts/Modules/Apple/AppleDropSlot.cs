#nullable enable
using GeminiLab.Core;
using TMPro;
using UnityEngine;

namespace GeminiLab.Modules.Apple
{
    /// <summary>
    /// A Scene-authored apple drop point. The object, renderer, collider and
    /// feedback text are created in the editor; runtime only toggles them.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AppleDropSlot : MonoBehaviour, IWorldMapSceneClickTarget
    {
        [SerializeField] private SpriteRenderer? _renderer;
        [SerializeField] private Collider2D? _collider;
        [SerializeField] private TMP_Text? _feedbackText;
        [SerializeField, Min(0.1f)] private float _feedbackDurationSeconds = 2f;
        [SerializeField] private Vector3 _feedbackLocalOffset = new(0f, 0.9f, -0.1f);
        [SerializeField] private int _interactionPriority = 30;

        private AppleTreeDropController? _owner;
        private Vector3 _targetWorldPosition;
        private Vector3 _fallStartWorldPosition;
        private int _amount;
        private float _fallElapsed;
        private float _fallDuration;
        private float _feedbackRemaining;
        private bool _occupied;
        private bool _falling;
        private bool _showingFeedback;

        public bool IsOccupied => _occupied;
        public int Amount => _amount;
        public bool IsWorldMapInteractionEnabled =>
            isActiveAndEnabled &&
            _occupied &&
            !_falling &&
            _owner != null &&
            _collider != null &&
            _collider.enabled;
        public int WorldMapInteractionPriority => _interactionPriority;
        public Renderer? WorldMapSortingRenderer => _renderer;

        private void Awake()
        {
            HideImmediate();
        }

        private void Update()
        {
            if (_falling)
            {
                _fallElapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(_fallElapsed / Mathf.Max(0.05f, _fallDuration));
                float eased = 1f - Mathf.Pow(1f - normalized, 3f);
                transform.position = Vector3.LerpUnclamped(
                    _fallStartWorldPosition,
                    _targetWorldPosition,
                    eased);

                if (normalized >= 1f)
                {
                    _falling = false;
                    if (_collider != null) _collider.enabled = true;
                }
            }

            if (!_showingFeedback) return;

            _feedbackRemaining -= Time.unscaledDeltaTime;
            if (_feedbackRemaining <= 0f)
            {
                _showingFeedback = false;
                SetFeedbackVisible(false);
            }
        }

        public void BeginDrop(
            AppleTreeDropController owner,
            int amount,
            float startHeight,
            float fallDuration)
        {
            _owner = owner;
            _amount = Mathf.Max(1, amount);
            _occupied = true;
            _showingFeedback = false;
            _feedbackRemaining = 0f;
            _fallElapsed = 0f;
            _fallDuration = Mathf.Max(0.05f, fallDuration);
            _targetWorldPosition = transform.position;
            _fallStartWorldPosition = _targetWorldPosition + Vector3.up * Mathf.Max(0f, startHeight);
            transform.position = _fallStartWorldPosition;

            if (_renderer != null) _renderer.enabled = true;
            if (_collider != null) _collider.enabled = false;
            SetFeedbackVisible(false);
            _falling = true;
        }

        public void CompleteCollection(int collectedAmount)
        {
            if (!_occupied) return;

            _occupied = false;
            _falling = false;
            if (_renderer != null) _renderer.enabled = false;
            if (_collider != null) _collider.enabled = false;

            if (_feedbackText == null) return;

            _feedbackText.text = $"收获 +{Mathf.Max(0, collectedAmount)}";
            _feedbackText.transform.position = transform.position +
                transform.TransformVector(_feedbackLocalOffset);
            SetFeedbackVisible(true);
            Color feedbackColor = _feedbackText.color;
            feedbackColor.a = 1f;
            _feedbackText.color = feedbackColor;
            _feedbackText.ForceMeshUpdate(true, true);
            _feedbackRemaining = Mathf.Max(0.1f, _feedbackDurationSeconds);
            _showingFeedback = true;
        }

        public void HideImmediate()
        {
            _owner = null;
            _amount = 0;
            _occupied = false;
            _falling = false;
            _showingFeedback = false;
            _feedbackRemaining = 0f;
            if (_renderer != null) _renderer.enabled = false;
            if (_collider != null) _collider.enabled = false;
            SetFeedbackVisible(false);
        }

        public bool ContainsWorldPoint(Vector2 worldPoint)
        {
            return IsWorldMapInteractionEnabled && _collider!.OverlapPoint(worldPoint);
        }

        public void HandleWorldMapClick()
        {
            if (IsWorldMapInteractionEnabled) _owner?.CollectDrop(this);
        }

        private void SetFeedbackVisible(bool visible)
        {
            if (_feedbackText == null) return;

            _feedbackText.gameObject.SetActive(visible);
            _feedbackText.enabled = visible;
            Renderer? renderer = _feedbackText.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = visible;
        }
    }
}
