#nullable enable
using System;
using GeminiLab.Modules.EmotionGarden;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI
{
    /// <summary>
    /// Scene 中预先保存 Image 资源的变体视图。
    /// 运行时只切换已有对象，不写入 Sprite，也不创建视觉节点。
    /// </summary>
    public sealed class SceneAuthoredImageVariantView : MonoBehaviour
    {
        [Serializable]
        private struct Variant
        {
            public string Key;
            public GameObject? Target;
        }

        [SerializeField] private Image? _previewImage;
        [SerializeField] private GameObject? _previewTarget;
        [SerializeField] private string _previewKey = string.Empty;
        [SerializeField] private Variant[] _variants = Array.Empty<Variant>();

        // Page state is only used by scene-authored paged panels. Existing
        // flower/codex callers continue to use Show/Hide directly.
        private int _activePageIndex = -1;

        public static string BuildFlowerKey(string emotionType, string owner, GrowthState state)
        {
            return BuildKey(
                EmotionFlowerCatalog.NormalizeEmotionType(emotionType),
                EmotionFlowerCatalog.NormalizeOwner(owner),
                ((int)state).ToString());
        }

        public static string BuildKey(string category, string owner, string state)
        {
            return $"{category}|{owner}|{state}";
        }

        public void Show(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                Hide();
                return;
            }

            bool previewShown = string.Equals(key, _previewKey, StringComparison.Ordinal);
            bool variantShown = false;
            SetAllVariantsActive(false);

            if (_previewTarget != null)
            {
                _previewTarget.SetActive(previewShown);
            }

            if (_previewImage != null)
            {
                _previewImage.enabled = previewShown;
            }

            for (int i = 0; i < _variants.Length; i++)
            {
                Variant variant = _variants[i];
                bool shouldShow = !previewShown && !variantShown &&
                    string.Equals(variant.Key, key, StringComparison.Ordinal);
                if (variant.Target != null)
                {
                    variant.Target.SetActive(shouldShow);
                }

                if (shouldShow)
                {
                    variantShown = true;
                }
            }

            if (!previewShown && !variantShown)
            {
                Hide();
                return;
            }

            _activePageIndex = ResolvePageIndex(key);
            gameObject.SetActive(true);
        }

        public void ShowPreview()
        {
            if (!string.IsNullOrWhiteSpace(_previewKey))
            {
                Show(_previewKey);
                return;
            }

            SetAllVariantsActive(false);
            if (_previewTarget != null)
            {
                _previewTarget.SetActive(true);
            }

            if (_previewImage != null)
            {
                _previewImage.enabled = true;
            }

            _activePageIndex = 0;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Shows the next scene-authored page. Page zero is the preview target;
        /// serialized variants follow in their Inspector order.
        /// </summary>
        public void ShowNext()
        {
            ShowRelativePage(1);
        }

        /// <summary>
        /// Shows the previous scene-authored page, clamped at the first page.
        /// </summary>
        public void ShowPrevious()
        {
            ShowRelativePage(-1);
        }

        public void Hide()
        {
            if (_previewImage != null)
            {
                _previewImage.enabled = false;
            }

            if (_previewTarget != null)
            {
                _previewTarget.SetActive(false);
            }

            SetAllVariantsActive(false);
            _activePageIndex = -1;
            gameObject.SetActive(false);
        }

        private void ShowRelativePage(int delta)
        {
            int pageCount = (_previewTarget != null || _previewImage != null ? 1 : 0) + _variants.Length;
            if (pageCount == 0)
            {
                return;
            }

            if (_activePageIndex < 0)
            {
                _activePageIndex = delta > 0 ? 0 : pageCount - 1;
            }

            int nextIndex = Mathf.Clamp(_activePageIndex + delta, 0, pageCount - 1);
            if (nextIndex == 0)
            {
                ShowPreview();
                return;
            }

            int variantIndex = nextIndex - 1;
            if (variantIndex >= 0 && variantIndex < _variants.Length)
            {
                Show(_variants[variantIndex].Key);
            }
        }

        private int ResolvePageIndex(string key)
        {
            if ((_previewTarget != null || _previewImage != null) && string.Equals(key, _previewKey, StringComparison.Ordinal))
            {
                return 0;
            }

            for (int i = 0; i < _variants.Length; i++)
            {
                if (string.Equals(_variants[i].Key, key, StringComparison.Ordinal))
                {
                    return (_previewTarget != null || _previewImage != null ? 1 : 0) + i;
                }
            }

            return -1;
        }

        private void SetAllVariantsActive(bool active)
        {
            if (_previewTarget != null)
            {
                _previewTarget.SetActive(active);
            }

            for (int i = 0; i < _variants.Length; i++)
            {
                GameObject? target = _variants[i].Target;
                if (target != null)
                {
                    target.SetActive(active);
                }
            }
        }
    }
}
