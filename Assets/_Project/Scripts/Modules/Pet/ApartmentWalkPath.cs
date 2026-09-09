#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>公寓平面导航。障碍为已按宠物脚底体积扩张的矩形，路径点使用宠物根坐标。</summary>
    public static class ApartmentWalkPath
    {
        private const float Clearance = 0.02f;

        public static Vector2 Clamp(Vector2 point, Rect area) => new(
            Mathf.Clamp(point.x, area.xMin, area.xMax), Mathf.Clamp(point.y, area.yMin, area.yMax));

        public static bool IsFree(Vector2 point, Rect area, IReadOnlyList<Rect> obstacles)
        {
            if (point.x < area.xMin || point.x > area.xMax || point.y < area.yMin || point.y > area.yMax) return false;
            for (int i = 0; i < obstacles.Count; i++)
                if (obstacles[i].Contains(point)) return false;
            return true;
        }

        public static bool IsClear(Vector2 from, Vector2 to, IReadOnlyList<Rect> obstacles)
        {
            for (int i = 0; i < obstacles.Count; i++)
                if (Intersect(from, to - from, obstacles[i], out _, out _)) return false;
            return true;
        }

        /// <summary>可见图最短路；只在目标或障碍变化时调用，不在每个物理帧重新规划。</summary>
        public static bool TryPlan(Vector2 from, Vector2 to, Rect area, IReadOnlyList<Rect> obstacles, List<Vector2> path)
        {
            path.Clear();
            if (!IsFree(from, area, obstacles) || !IsFree(to, area, obstacles)) return false;
            if (IsClear(from, to, obstacles)) { path.Add(to); return true; }
            var nodes = new List<Vector2> { from, to };
            for (int i = 0; i < obstacles.Count; i++)
            {
                Rect r = obstacles[i];
                AddCorner(new Vector2(r.xMin - Clearance, r.yMin - Clearance));
                AddCorner(new Vector2(r.xMin - Clearance, r.yMax + Clearance));
                AddCorner(new Vector2(r.xMax + Clearance, r.yMin - Clearance));
                AddCorner(new Vector2(r.xMax + Clearance, r.yMax + Clearance));
            }
            void AddCorner(Vector2 p) { if (IsFree(p, area, obstacles)) nodes.Add(p); }
            var distances = new float[nodes.Count];
            var previous = new int[nodes.Count];
            var visited = new bool[nodes.Count];
            for (int i = 0; i < nodes.Count; i++) { distances[i] = float.PositiveInfinity; previous[i] = -1; }
            distances[0] = 0;
            for (int iteration = 0; iteration < nodes.Count; iteration++)
            {
                int best = -1;
                for (int i = 0; i < nodes.Count; i++)
                    if (!visited[i] && (best < 0 || distances[i] < distances[best])) best = i;
                if (best < 0 || float.IsPositiveInfinity(distances[best])) return false;
                if (best == 1)
                {
                    for (int index = 1; index != 0; index = previous[index]) path.Add(nodes[index]);
                    path.Reverse();
                    return true;
                }
                visited[best] = true;
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (visited[i]) continue;
                    float distance = distances[best] + Vector2.Distance(nodes[best], nodes[i]);
                    if (distance >= distances[i] || !IsClear(nodes[best], nodes[i], obstacles)) continue;
                    distances[i] = distance;
                    previous[i] = best;
                }
            }
            return false;
        }

        /// <summary>扫掠移动并沿障碍边滑动；不会因为单帧位移大而穿过薄家具。</summary>
        public static Vector2 Slide(Vector2 from, Vector2 delta, Rect area, IReadOnlyList<Rect> obstacles)
        {
            delta = Clamp(from + delta, area) - from;
            Vector2 position = from;
            for (int pass = 0; pass < 2 && delta.sqrMagnitude > 0.00000001f; pass++)
            {
                float fraction = 1f;
                Vector2 normal = Vector2.zero;
                for (int i = 0; i < obstacles.Count; i++)
                    if (Intersect(position, delta, obstacles[i], out float hit, out Vector2 n) && hit < fraction)
                    { fraction = hit; normal = n; }
                float safeFraction = Mathf.Max(0f, fraction - 0.001f / Mathf.Max(0.001f, delta.magnitude));
                position += delta * (fraction < 1f ? safeFraction : 1f);
                delta *= 1f - fraction;
                delta -= normal * Vector2.Dot(delta, normal);
            }
            return Clamp(position, area);
        }

        private static bool Intersect(Vector2 start, Vector2 delta, Rect rect, out float fraction, out Vector2 normal)
        {
            float enter = 0f, exit = 1f;
            normal = Vector2.zero;
            for (int axis = 0; axis < 2; axis++)
            {
                float origin = start[axis], direction = delta[axis];
                float min = axis == 0 ? rect.xMin : rect.yMin;
                float max = axis == 0 ? rect.xMax : rect.yMax;
                if (Mathf.Abs(direction) < 0.000001f)
                { if (origin < min || origin > max) { fraction = 1f; return false; } continue; }
                float a = (min - origin) / direction, b = (max - origin) / direction;
                float near = Mathf.Min(a, b), far = Mathf.Max(a, b);
                if (near >= enter)
                { enter = near; normal = axis == 0 ? new Vector2(-Mathf.Sign(direction), 0) : new Vector2(0, -Mathf.Sign(direction)); }
                exit = Mathf.Min(exit, far);
                if (enter > exit) { fraction = 1f; return false; }
            }
            fraction = enter;
            return exit >= 0f && enter <= 1f;
        }
    }
}
