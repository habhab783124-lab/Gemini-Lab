#nullable enable
using System;
using GeminiLab.Modules.Furniture;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.RoomRelic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GeminiLab.Modules.HubUI
{
    /// <summary>
    /// Bridges pointer clicks from a RawImage viewport into Apartment world-space interaction.
    /// Current version forwards pet clicks first, then falls back to pet furniture interaction.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ApartmentViewportInputBridge : MonoBehaviour, IPointerClickHandler
    {
        // 视口点击桥显式限定 UnityEngine.Object，避免与 System.Object 发生歧义。
        [SerializeField] private RawImage? _viewportImage;
        [SerializeField] private Camera? _viewportCamera;
        [SerializeField] private float _worldPlaneZ;
        [SerializeField] private ApartmentDoorInteraction? _indoorDoor;
        [SerializeField] private FurniturePageLink[] _furniturePageLinks = System.Array.Empty<FurniturePageLink>();
        [Tooltip("Scene-authored world click handlers, evaluated in array order after Build Mode and before pets.")]
        [SerializeField] private MonoBehaviour[] _worldPointInteractables = Array.Empty<MonoBehaviour>();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left &&
                eventData.button != PointerEventData.InputButton.Right) return;
            if (!TryScreenPointToWorldPoint(
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 worldPoint,
                    out _))
            {
                return;
            }

            if (TryHandleBuildMode(worldPoint, eventData.button))
            {
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (_indoorDoor != null && _indoorDoor.TryHandleWorldPoint(worldPoint)) return;
            foreach (FurniturePageLink link in _furniturePageLinks)
                if (link != null && link.TryHandleWorldPoint(worldPoint)) return;

            if (eventData.button == PointerEventData.InputButton.Left &&
                TryHandleWorldPointInteractables(worldPoint))
            {
                return;
            }

            PetClickReactionController[] petClicks = UnityEngine.Object.FindObjectsByType<PetClickReactionController>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < petClicks.Length; i++)
            {
                PetClickReactionController petClick = petClicks[i];
                if (petClick != null && petClick.TryHandleWorldPoint(worldPoint))
                {
                    return;
                }
            }

            RoomRelicInteraction[] relicInteractions = UnityEngine.Object.FindObjectsByType<RoomRelicInteraction>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < relicInteractions.Length; i++)
            {
                RoomRelicInteraction relicInteraction = relicInteractions[i];
                if (relicInteraction != null && relicInteraction.TryHandleWorldPoint(worldPoint))
                {
                    return;
                }
            }

            PetPlayerFurnitureInteractionController[] furnitureInteractions = UnityEngine.Object.FindObjectsByType<PetPlayerFurnitureInteractionController>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < furnitureInteractions.Length; i++)
            {
                PetPlayerFurnitureInteractionController interaction = furnitureInteractions[i];
                if (interaction != null && interaction.TryHandleWorldPoint(worldPoint))
                {
                    return;
                }
            }

            // 点击空地 → 取消所有宠物的选中，双方都进入自由漫游
            PetPlayerInputController.ReleaseAllControl();
        }

        private bool TryHandleWorldPointInteractables(Vector2 worldPoint)
        {
            for (int i = 0; i < _worldPointInteractables.Length; i++)
            {
                MonoBehaviour candidate = _worldPointInteractables[i];
                if (candidate is IApartmentWorldPointInteractable interactable &&
                    candidate.isActiveAndEnabled &&
                    interactable.TryHandleWorldPoint(worldPoint))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryScreenPointToWorldPoint(
            Vector2 screenPoint,
            Camera? eventCamera,
            out Vector2 worldPoint,
            out Vector2 viewportPoint)
        {
            worldPoint = default;
            viewportPoint = default;

            if (_viewportCamera == null)
            {
                return false;
            }

            RectTransform targetRect = _viewportImage != null
                ? _viewportImage.rectTransform
                : (RectTransform)transform;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    targetRect,
                    screenPoint,
                    eventCamera,
                    out Vector2 localPoint))
            {
                return false;
            }

            Rect rect = targetRect.rect;
            if (!TryLocalPointToViewportPoint(localPoint, rect, out viewportPoint))
            {
                return false;
            }

            float cameraDepth = _worldPlaneZ - _viewportCamera.transform.position.z;
            if (_viewportCamera.orthographic)
            {
                cameraDepth = 0f;
            }
            else if (cameraDepth < _viewportCamera.nearClipPlane)
            {
                cameraDepth = _viewportCamera.nearClipPlane;
            }

            Vector3 worldPoint3 = _viewportCamera.ViewportToWorldPoint(new Vector3(
                viewportPoint.x,
                viewportPoint.y,
                cameraDepth));
            worldPoint = new Vector2(worldPoint3.x, worldPoint3.y);
            return true;
        }

        public static bool TryLocalPointToViewportPoint(Vector2 localPoint, Rect rect, out Vector2 viewportPoint)
        {
            viewportPoint = default;

            if (rect.width <= 0.001f || rect.height <= 0.001f)
            {
                return false;
            }

            if (localPoint.x < rect.xMin || localPoint.x > rect.xMax ||
                localPoint.y < rect.yMin || localPoint.y > rect.yMax)
            {
                return false;
            }

            viewportPoint = new Vector2(
                Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x),
                Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y));
            return true;
        }

        private static bool TryHandleBuildMode(Vector2 worldPoint, PointerEventData.InputButton button)
        {
            if (button != PointerEventData.InputButton.Left && button != PointerEventData.InputButton.Right)
            {
                return false;
            }

            BuildModeController[] buildControllers = UnityEngine.Object.FindObjectsByType<BuildModeController>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < buildControllers.Length; i++)
            {
                BuildModeController buildController = buildControllers[i];
                if (buildController == null || !buildController.IsBuildModeEnabled)
                {
                    continue;
                }

                bool isPrimaryAction = button == PointerEventData.InputButton.Left;
                _ = buildController.TryHandleViewportWorldPoint(worldPoint, isPrimaryAction);
                return true;
            }

            return false;
        }
    }
}
