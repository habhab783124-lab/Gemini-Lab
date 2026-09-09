#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>显式绑定到公寓宠物。统一根坐标/脚底碰撞体坐标、固定帧运动和家具绕行。</summary>
    [DisallowMultipleComponent]
    public sealed class ApartmentPetMovement : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D? _body;
        [SerializeField] private CapsuleCollider2D? _footprint;
        [SerializeField] private BoxCollider2D? _room;
        [SerializeField] private BoxCollider2D? _connectedRoom;
        [SerializeField, Min(0.01f)] private float _skin = 0.04f;
        [SerializeField, Min(0.1f)] private float _obstacleRefreshSeconds = 0.5f;
        private readonly List<Collider2D> _hits = new();
        private readonly List<Rect> _obstacles = new();
        private readonly List<Rect> _nextObstacles = new();
        private readonly List<Vector2> _path = new();
        private Vector2 _offset, _halfSize, _velocity, _goal;
        private float _refreshAt;
        private float _followSpeed, _arrivalDistance, _blockedSeconds;
        private Vector2 _previousFixedPosition;
        private bool _hasPreviousFixedPosition;
        private int _pathIndex;
        private bool _following;
        private bool _pathValid;
        public bool IsReady => isActiveAndEnabled && _body != null && _footprint != null && _room != null;
        public IReadOnlyList<Vector2> Path => _path;
        public Vector2 SteeringVelocity => _velocity;
        public Rect WalkArea { get; private set; }
        public Vector2 Position => _body != null ? _body.position : (Vector2)transform.position;

        private void OnDisable() => Stop();
        public void Stop() { _velocity = Vector2.zero; _following = false; _path.Clear(); if (_body != null) _body.velocity = Vector2.zero; }
        public Vector2 Clamp(Vector2 point) { UpdateGeometry(); return ApartmentWalkPath.Clamp(point, WalkArea); }

        private void UpdateGeometry()
        {
            if (_room == null || _footprint == null) return;
            // 不用 bounds.center - 插值后的 transform.position：两者可能分属不同物理帧，
            // 会让导航障碍在移动时抖动。使用局部形状与矩阵计算稳定的脚底包围盒。
            _offset = _footprint.transform.TransformVector(_footprint.offset);
            if (_footprint.transform != transform)
                _offset += (Vector2)(_footprint.transform.position - transform.position);
            Vector3 x = _footprint.transform.TransformVector(new Vector3(_footprint.size.x * 0.5f, 0, 0));
            Vector3 y = _footprint.transform.TransformVector(new Vector3(0, _footprint.size.y * 0.5f, 0));
            _halfSize = new Vector2(Mathf.Abs(x.x) + Mathf.Abs(y.x), Mathf.Abs(x.y) + Mathf.Abs(y.y));
            // 左右翻转动画也必须保留同一条通路，取两种朝向的水平包络。
            _halfSize.x += Mathf.Abs(_offset.x);
            _offset.x = 0f;
            Bounds bounds = GetApartmentBounds();
            Vector2 min = (Vector2)bounds.min + _halfSize - _offset + Vector2.one * _skin;
            Vector2 max = (Vector2)bounds.max - _halfSize - _offset - Vector2.one * _skin;
            max = Vector2.Max(min, max);
            Rect area = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            if (area != WalkArea) _pathValid = false;
            WalkArea = area;
        }

        private Bounds GetApartmentBounds()
        {
            Bounds bounds = _room!.bounds;
            if (_connectedRoom != null) bounds.Encapsulate(_connectedRoom.bounds);
            return bounds;
        }

        public void RefreshObstacles()
        {
            if (!IsReady) return;
            UpdateGeometry();
            _hits.Clear(); _nextObstacles.Clear();
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
            Bounds apartment = GetApartmentBounds();
            Physics2D.OverlapBox(apartment.center, apartment.size, 0f, filter, _hits);
            foreach (Collider2D c in _hits)
            {
                if (c.attachedRigidbody == _body || c.isTrigger || !c.enabled || Physics2D.GetIgnoreCollision(_footprint!, c)) continue;
                // 移动中的宠物不作为固定障碍烘入路径，刚体接触仍由物理处理。
                if (c.attachedRigidbody != null && c.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic) continue;
                Bounds b = c.bounds;
                Vector2 min = (Vector2)b.min - _halfSize - _offset - Vector2.one * _skin;
                Vector2 max = (Vector2)b.max + _halfSize - _offset + Vector2.one * _skin;
                _nextObstacles.Add(Rect.MinMaxRect(min.x, min.y, max.x, max.y));
            }
            bool changed = _obstacles.Count != _nextObstacles.Count;
            for (int i = 0; !changed && i < _obstacles.Count; i++) changed = _obstacles[i] != _nextObstacles[i];
            if (changed) { _obstacles.Clear(); _obstacles.AddRange(_nextObstacles); _pathValid = false; }
            _refreshAt = Time.time + _obstacleRefreshSeconds;
        }

        public void SetManualVelocity(Vector2 velocity)
        { _following = false; _path.Clear(); _velocity = velocity; }

        public bool Follow(Vector2 goal, float speed, float arrivalDistance, out bool arrived)
        {
            arrived = false;
            if (!IsReady) return false;
            bool changed = !_following || (goal - _goal).sqrMagnitude > 0.0001f;
            if (changed) { _blockedSeconds = 0; _hasPreviousFixedPosition = false; }
            if (_blockedSeconds >= 2f) { _velocity = Vector2.zero; return false; }
            bool refreshed = Time.time >= _refreshAt;
            if (changed || refreshed) RefreshObstacles();
            _following = true; _goal = goal;
            _followSpeed = speed; _arrivalDistance = arrivalDistance;
            if (changed || !_pathValid)
            {
                _pathValid = ApartmentWalkPath.TryPlan(Position, goal, WalkArea, _obstacles, _path);
                _pathIndex = 0;
            }
            if (!_pathValid) { _velocity = Vector2.zero; return false; }
            arrived = Vector2.Distance(Position, goal) <= arrivalDistance;
            if (arrived) { _velocity = Vector2.zero; return true; }
            UpdatePathVelocity();
            return true;
        }

        private void UpdatePathVelocity()
        {
            if (_path.Count == 0 || Vector2.Distance(Position, _goal) <= _arrivalDistance) { _velocity = Vector2.zero; return; }
            while (_pathIndex < _path.Count - 1 && Vector2.Distance(Position, _path[_pathIndex]) < 0.01f) _pathIndex++;
            Vector2 delta = _path[_pathIndex] - Position;
            _velocity = delta.normalized * Mathf.Min(_followSpeed, delta.magnitude / Mathf.Max(0.001f, Time.fixedDeltaTime));
        }

        public void FixedTick(float deltaTime)
        {
            if (!IsReady || !_body!.simulated || !_footprint!.enabled) return;
            if (Time.time >= _refreshAt) RefreshObstacles();
            if (_following)
            {
                if (_hasPreviousFixedPosition && _velocity.sqrMagnitude > 0.001f)
                    _blockedSeconds = Vector2.Distance(Position, _previousFixedPosition) < 0.001f ? _blockedSeconds + deltaTime : 0f;
                _previousFixedPosition = Position; _hasPreviousFixedPosition = true;
                UpdatePathVelocity();
            }
            Vector2 from = Clamp(Position);
            // 房间外的旧存档/外力纠正到边界；正常行走只使用 MovePosition。
            if ((from - Position).sqrMagnitude > 0.000001f) _body.position = from;
            Vector2 next = ApartmentWalkPath.Slide(from, _velocity * deltaTime, WalkArea, _obstacles);
            _body.velocity = Vector2.zero;
            _body.MovePosition(next);
        }

        /// <summary>编辑器/布局变动时选择邻近且从当前位置可达的站立点，不改变交互姿势点。</summary>
        public bool TryFindStandPoint(Vector2 requested, float radius, out Vector2 result)
        {
            RefreshObstacles();
            var candidates = new List<Vector2> { Clamp(requested) };
            for (float r = 0.25f; r <= radius; r += 0.25f)
                for (int i = 0; i < 24; i++)
                { float angle = i * Mathf.PI / 12f; candidates.Add(Clamp(requested + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r)); }
            candidates.Sort((a, b) => (a - requested).sqrMagnitude.CompareTo((b - requested).sqrMagnitude));
            var scratch = new List<Vector2>();
            foreach (Vector2 point in candidates)
                if (Vector2.Distance(point, requested) <= radius && ApartmentWalkPath.TryPlan(Clamp(Position), point, WalkArea, _obstacles, scratch))
                { result = point; return true; }
            result = requested; return false;
        }

        private void OnDrawGizmosSelected()
        {
            if (!IsReady) return;
            UpdateGeometry(); Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(WalkArea.center, WalkArea.size);
            Vector2 previous = Position;
            for (int i = _pathIndex; i < _path.Count; i++) { Gizmos.DrawLine(previous, _path[i]); previous = _path[i]; }
        }
    }
}
