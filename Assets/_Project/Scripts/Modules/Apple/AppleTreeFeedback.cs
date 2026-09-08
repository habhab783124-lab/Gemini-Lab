#nullable enable
using TMPro;
using UnityEngine;

namespace GeminiLab.Modules.Apple
{
    /// <summary>
    /// 苹果树无可领取/领取成功反馈。文本和两片落叶节点由 WorldMap Scene 作者化，
    /// 运行时只切换显示状态和播放轻量位移动画。
    /// </summary>
    public sealed class AppleTreeFeedback : MonoBehaviour
    {
        [SerializeField] private TMP_Text? _statusText;
        [SerializeField] private TMP_Text? _leftLeaf;
        [SerializeField] private TMP_Text? _rightLeaf;
        [SerializeField, Min(0.1f)] private float _durationSeconds = 1.6f;
        [SerializeField, Min(0f)] private float _leafFallDistance = 0.9f;
        [SerializeField, Min(0f)] private float _leafSideDistance = 0.35f;

        private Vector3 _leftLeafOrigin;
        private Vector3 _rightLeafOrigin;
        private float _remaining;
        private bool _isShowing;

        private void Awake()
        {
            CacheOrigins();
            Hide();
        }

        private void OnEnable()
        {
            CacheOrigins();
            Hide();
        }

        private void Update()
        {
            if (!_isShowing)
            {
                return;
            }

            _remaining -= Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(1f - (_remaining / Mathf.Max(0.1f, _durationSeconds)));
            float eased = 1f - Mathf.Pow(1f - normalized, 2f);

            if (_leftLeaf != null)
            {
                _leftLeaf.transform.localPosition = _leftLeafOrigin +
                    new Vector3(-_leafSideDistance * eased, -_leafFallDistance * eased, 0f);
                SetAlpha(_leftLeaf, 1f - normalized);
            }

            if (_rightLeaf != null)
            {
                _rightLeaf.transform.localPosition = _rightLeafOrigin +
                    new Vector3(_leafSideDistance * eased, -_leafFallDistance * eased, 0f);
                SetAlpha(_rightLeaf, 1f - normalized);
            }

            if (_remaining <= 0f)
            {
                Hide();
            }
        }

        public void ShowNotReady()
        {
            Show("还没成熟哦");
        }

        public void ShowCollected(int amount)
        {
            Show($"获得苹果 ×{Mathf.Max(0, amount)}");
        }

        private void Show(string message)
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            CacheOrigins();
            _isShowing = true;
            _remaining = Mathf.Max(0.1f, _durationSeconds);

            if (_statusText != null)
            {
                _statusText.text = message;
                SetTextVisible(_statusText, true);
                SetAlpha(_statusText, 1f);
                _statusText.ForceMeshUpdate(true, true);
            }

            ResetLeaf(_leftLeaf, _leftLeafOrigin);
            ResetLeaf(_rightLeaf, _rightLeafOrigin);
        }

        private void Hide()
        {
            _isShowing = false;
            _remaining = 0f;
            SetTextVisible(_statusText, false);
            SetTextVisible(_leftLeaf, false);
            SetTextVisible(_rightLeaf, false);
        }

        private void CacheOrigins()
        {
            if (_leftLeaf != null) _leftLeafOrigin = _leftLeaf.transform.localPosition;
            if (_rightLeaf != null) _rightLeafOrigin = _rightLeaf.transform.localPosition;
        }

        private static void ResetLeaf(TMP_Text? leaf, Vector3 origin)
        {
            if (leaf == null) return;
            leaf.transform.localPosition = origin;
            SetTextVisible(leaf, true);
            SetAlpha(leaf, 1f);
            leaf.ForceMeshUpdate(true, true);
        }

        private static void SetTextVisible(TMP_Text? text, bool visible)
        {
            if (text == null) return;

            text.gameObject.SetActive(visible);
            text.enabled = visible;
            Renderer? renderer = text.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = visible;
        }

        private static void SetAlpha(TMP_Text text, float alpha)
        {
            Color color = text.color;
            color.a = Mathf.Clamp01(alpha);
            text.color = color;
        }
    }
}
