#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace GeminiLab.Modules.HubUI
{
    /// <summary>
    /// Handles authored indoor furniture selection feedback for the apartment viewport.
    /// The bridge supplies world points; this component only toggles already-authored
    /// highlight objects and fills an already-authored text field.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ApartmentFurnitureSelectionPresenter : MonoBehaviour, IApartmentWorldPointInteractable
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private GameObject? _target;
            [SerializeField] private GameObject? _highlight;
            [SerializeField] private string _definitionId = string.Empty;
            [SerializeField, TextArea(2, 4)] private string _message = string.Empty;
            [SerializeField] private SpriteRenderer? _hitRenderer;
            private Sprite? _cachedSprite;
            private Vector2[] _vertices = Array.Empty<Vector2>();
            private ushort[] _triangles = Array.Empty<ushort>();

            public GameObject? Target => _target;
            public GameObject? Highlight => _highlight;
            public string DefinitionId => _definitionId;
            public string Message => _message;
            public SpriteRenderer? HitRenderer => _hitRenderer;
            public bool Contains(Vector2 worldPoint)
            {
                if (_hitRenderer == null || !_hitRenderer.enabled || !_hitRenderer.gameObject.activeInHierarchy || !_hitRenderer.sprite) return false;
                if (_cachedSprite != _hitRenderer.sprite)
                {
                    _cachedSprite = _hitRenderer.sprite;
                    _vertices = _cachedSprite.vertices;
                    _triangles = _cachedSprite.triangles;
                }
                Vector2 point = _hitRenderer.transform.InverseTransformPoint(worldPoint);
                if (_hitRenderer.flipX) point.x = -point.x;
                if (_hitRenderer.flipY) point.y = -point.y;
                for (int i = 0; i + 2 < _triangles.Length; i += 3)
                    if (InsideTriangle(point, _vertices[_triangles[i]], _vertices[_triangles[i+1]], _vertices[_triangles[i+2]])) return true;
                return false;
            }
            private static float Cross(Vector2 a, Vector2 b) => a.x*b.y-a.y*b.x;
            private static bool InsideTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
            {
                if (Mathf.Abs(Cross(b-a,c-a)) < 0.000001f) return false;
                float x=Cross(b-a,p-a), y=Cross(c-b,p-b), z=Cross(a-c,p-c);
                return !((x < -0.00001f || y < -0.00001f || z < -0.00001f) && (x > 0.00001f || y > 0.00001f || z > 0.00001f));
            }
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();
        [SerializeField] private GameObject? _messageRoot;
        [SerializeField] private TMP_Text? _messageText;

        private Entry? _selected;
        public bool HasSelection => _selected != null;

        public IReadOnlyList<Entry> Entries => _entries;

        public bool TryHandleWorldPoint(Vector2 worldPoint)
        {
            SetHoverWorldPoint(worldPoint);
            // Feedback must never consume a furniture action click.
            return false;
        }

        public bool SetHoverWorldPoint(Vector2 worldPoint)
        {
            Entry? entry = FindTopmostEntry(worldPoint);
            if (entry == null)
            {
                ClearSelection();
                return false;
            }

            Select(entry);
            return true;
        }

        public void ClearSelection()
        {
            if (_selected?.Highlight != null)
            {
                _selected.Highlight.SetActive(false);
            }

            _selected = null;
            if (_messageText != null)
            {
                _messageText.text = string.Empty;
            }

            if (_messageRoot != null)
            {
                _messageRoot.SetActive(false);
            }
        }

        private void OnEnable()
        {
            ClearSelection();
        }

        private void OnDisable()
        {
            ClearSelection();
        }

        private void Select(Entry entry)
        {
            if (ReferenceEquals(_selected, entry)) return;
            if (_selected != null && !ReferenceEquals(_selected, entry) && _selected.Highlight != null)
            {
                _selected.Highlight.SetActive(false);
            }

            _selected = entry;
            if (entry.Highlight != null)
            {
                entry.Highlight.SetActive(true);
            }

            if (_messageText != null)
            {
                _messageText.text = entry.Message;
            }

            if (_messageRoot != null)
            {
                _messageRoot.SetActive(true);
            }
        }

        private Entry? FindTopmostEntry(Vector2 worldPoint)
        {
            Entry? visual = null;
            bool hasVisualBindings = false;
            foreach (Entry entry in _entries)
            {
                if (entry == null || entry.HitRenderer == null) continue;
                hasVisualBindings = true;
                if (entry.Target == null || !entry.Target.activeInHierarchy || !entry.Contains(worldPoint)) continue;
                if (visual == null || CompareVisualOrder(entry.HitRenderer.gameObject, visual.HitRenderer!.gameObject) > 0) visual = entry;
            }
            if (hasVisualBindings)
            {
                if (visual == null) return null;
                // Foreground pets and authored relics may occlude furniture. Furniture uses visual mesh hits,
                // not its smaller walking footprint. Ignore non-visual room trigger volumes.
                if (ClickOcclusionUtility.TryGetTopmostColliderAtWorldPoint(worldPoint, out Collider2D? front) && front != null)
                {
                    bool furnitureCollider = false;
                    foreach (Entry entry in _entries)
                        if (entry.Target != null && (front.transform == entry.Target.transform || front.transform.IsChildOf(entry.Target.transform))) { furnitureCollider = true; break; }
                    if (!furnitureCollider && (front.GetComponentInParent<Renderer>() != null || front.GetComponentInParent<SortingGroup>() != null) &&
                        CompareVisualOrder(front.gameObject, visual.HitRenderer!.gameObject) > 0) return null;
                }
                return visual;
            }
            if (!ClickOcclusionUtility.TryGetTopmostColliderAtWorldPoint(worldPoint, out Collider2D? topmostCollider) ||
                topmostCollider == null)
            {
                return null;
            }

            for (int i = 0; i < _entries.Length; i++)
            {
                Entry? entry = _entries[i];
                if (entry?.Target == null || !entry.Target.activeInHierarchy)
                {
                    continue;
                }

                Transform targetTransform = entry.Target.transform;
                Transform hitTransform = topmostCollider.transform;
                if (hitTransform == targetTransform || hitTransform.IsChildOf(targetTransform))
                {
                    return entry;
                }
            }

            return null;
        }
        private static int CompareVisualOrder(GameObject left, GameObject right)
        {
            Priority(left, out int ll, out int lo); Priority(right, out int rl, out int ro);
            int comparison = ll.CompareTo(rl);
            if (comparison == 0) comparison = lo.CompareTo(ro);
            return comparison != 0 ? comparison : right.transform.position.z.CompareTo(left.transform.position.z);
        }
        private static void Priority(GameObject target, out int layer, out int order)
        {
            var group = target.GetComponentInParent<SortingGroup>();
            var renderer = target.GetComponentInParent<Renderer>();
            layer = SortingLayer.GetLayerValueFromID(group != null ? group.sortingLayerID : renderer != null ? renderer.sortingLayerID : 0);
            order = group != null ? group.sortingOrder : renderer != null ? renderer.sortingOrder : 0;
        }
    }
}
