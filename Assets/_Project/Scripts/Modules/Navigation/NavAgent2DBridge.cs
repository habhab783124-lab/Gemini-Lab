#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Navigation
{
    /// <summary>
    /// Movement helper that keeps motion in XY plane.
    /// </summary>
    public static class NavAgent2DBridge
    {
        public static Vector2 MoveTowards(Vector2 current, Vector2 target, float speed, float deltaTime)
        {
            return Vector2.MoveTowards(current, target, speed * deltaTime);
        }
    }
}
