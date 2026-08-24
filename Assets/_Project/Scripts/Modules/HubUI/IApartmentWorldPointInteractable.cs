#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.HubUI
{
    /// <summary>
    /// Receives a world-space click converted by <see cref="ApartmentViewportInputBridge"/>.
    /// Implementations must decide whether the point belongs to their authored Scene colliders.
    /// </summary>
    public interface IApartmentWorldPointInteractable
    {
        /// <returns><see langword="true"/> when this handler consumed the click.</returns>
        bool TryHandleWorldPoint(Vector2 worldPoint);
    }
}
