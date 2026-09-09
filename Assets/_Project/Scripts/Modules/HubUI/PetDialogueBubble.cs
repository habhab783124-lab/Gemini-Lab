#nullable enable
using TMPro;
using GeminiLab.Modules.Pet;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI
{
    /// <summary>场景中作者化的宠物气泡，将宠物头顶投影到公寓视口，不随宠物翻转。</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class PetDialogueBubble : MonoBehaviour
    {
        [SerializeField] private Transform? _pet;
        [SerializeField] private Transform? _otherPet;
        [SerializeField] private Camera? _camera;
        [SerializeField] private RawImage? _viewport;
        [SerializeField] private GameObject? _content;
        [SerializeField] private TMP_Text? _text;
        [SerializeField] private RectTransform? _tail;
        [SerializeField] private Vector3 _headOffset = new(0, 1.6f, 0);
        [SerializeField] private Vector2 _offset = new(210, 24);
        [SerializeField] private float _edgePadding = 12;

        public bool IsVisible => _content != null && _content.activeInHierarchy;
        public string Message => _text != null ? _text.text : string.Empty;

        public void Show(string message)
        {
            if (_text != null) _text.text = message;
            if (_content != null) _content.SetActive(true);
            UpdatePosition();
        }

        public void Hide()
        {
            if (_content != null) _content.SetActive(false);
        }

        private void LateUpdate() => UpdatePosition();

        private void UpdatePosition()
        {
            if (_pet == null || _camera == null || _viewport == null || transform is not RectTransform bubble) return;
            Rect bounds = _viewport.rectTransform.rect;
            Vector3 petPosition = VisiblePosition(_pet);
            Vector3 projected = _camera.WorldToViewportPoint(petPosition + _headOffset);
            Rect uv = _viewport.uvRect;
            if (Mathf.Abs(uv.width) < .001f || Mathf.Abs(uv.height) < .001f) return;
            Vector2 head = new(bounds.xMin + (projected.x - uv.x) / uv.width * bounds.width,
                bounds.yMin + (projected.y - uv.y) / uv.height * bounds.height);
            float separation = _otherPet != null ? petPosition.x - VisiblePosition(_otherPet).x : 0;
            float side = Mathf.Abs(separation) > .05f ? Mathf.Sign(separation) : Mathf.Sign(_offset.x);
            Vector2 position = head + new Vector2(side * Mathf.Abs(_offset.x), _offset.y);
            float halfWidth = bubble.rect.width * .5f;
            position.x = Mathf.Clamp(position.x, bounds.xMin + halfWidth + _edgePadding, bounds.xMax - halfWidth - _edgePadding);
            position.y = Mathf.Clamp(position.y, bounds.yMin + _edgePadding, bounds.yMax - bubble.rect.height - _edgePadding);
            // 根节点直接位于视口下，中心锚点、底边 pivot；不依赖屏幕分辨率或宠物缩放。
            bubble.anchoredPosition = position;
            if (_tail != null)
                _tail.anchoredPosition = new Vector2(Mathf.Clamp(head.x - position.x, -halfWidth + 22, halfWidth - 22), 4);
        }

        private static Vector3 VisiblePosition(Transform pet) =>
            pet.TryGetComponent<PetController>(out var controller) ? controller.VisiblePosition : pet.position;
    }
}
