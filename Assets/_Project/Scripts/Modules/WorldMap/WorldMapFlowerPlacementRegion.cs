#nullable enable
using System;
using GeminiLab.Modules.EmotionGarden;
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// Scene-authored owner region for WorldMap flower placement.
    /// The BoxCollider2D is an editable data boundary; it is not used for physics.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class WorldMapFlowerPlacementRegion : MonoBehaviour
    {
        [SerializeField] private string _owner = EmotionFlowerCatalog.OwnerAngel;
        [SerializeField] private BoxCollider2D? _boundsCollider;
        [SerializeField] private bool _defaultsInitialized;

        public string Owner => EmotionFlowerCatalog.NormalizeOwner(_owner);
        public BoxCollider2D? BoundsCollider => _boundsCollider != null
            ? _boundsCollider
            : GetComponent<BoxCollider2D>();
        public bool DefaultsInitialized => _defaultsInitialized;

        public bool MatchesOwner(string owner)
        {
            return string.Equals(Owner, EmotionFlowerCatalog.NormalizeOwner(owner), StringComparison.Ordinal);
        }

        public Bounds WorldBounds
        {
            get
            {
                BoxCollider2D? collider = BoundsCollider;
                if (collider == null) return new Bounds(transform.position, Vector3.zero);

                Vector3 scale = collider.transform.lossyScale;
                Vector2 absoluteScale = new(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                Vector2 size = Vector2.Scale(collider.size, absoluteScale);
                Vector2 offset = Vector2.Scale(collider.offset, absoluteScale);
                Vector2 center = (Vector2)collider.transform.position + offset;
                return new Bounds(center, size);
            }
        }

        public bool Contains(Rect worldRect)
        {
            Bounds bounds = WorldBounds;
            return worldRect.xMin >= bounds.min.x &&
                   worldRect.xMax <= bounds.max.x &&
                   worldRect.yMin >= bounds.min.y &&
                   worldRect.yMax <= bounds.max.y;
        }

        public bool ContainsPoint(Vector2 worldPoint)
        {
            Bounds bounds = WorldBounds;
            return worldPoint.x >= bounds.min.x && worldPoint.x <= bounds.max.x &&
                   worldPoint.y >= bounds.min.y && worldPoint.y <= bounds.max.y;
        }

        private void Reset()
        {
            _boundsCollider = GetComponent<BoxCollider2D>();
        }

        private void OnValidate()
        {
            _boundsCollider ??= GetComponent<BoxCollider2D>();
            _owner = EmotionFlowerCatalog.NormalizeOwner(_owner);
        }
    }
}
