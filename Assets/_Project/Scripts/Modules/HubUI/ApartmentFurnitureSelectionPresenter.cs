#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core;
using TMPro;
using UnityEngine;

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

            public GameObject? Target => _target;
            public GameObject? Highlight => _highlight;
            public string DefinitionId => _definitionId;
            public string Message => _message;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();
        [SerializeField] private GameObject? _messageRoot;
        [SerializeField] private TMP_Text? _messageText;

        private Entry? _selected;

        public IReadOnlyList<Entry> Entries => _entries;

        public bool TryHandleWorldPoint(Vector2 worldPoint)
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
    }
}
