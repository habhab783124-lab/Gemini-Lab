#nullable enable
using System.Collections.Generic;
using GeminiLab.Core.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GeminiLab.Modules.Furniture
{
    /// <summary>
    /// Minimal V-Decor build mode controller.
    /// </summary>
    public sealed class BuildModeController : MonoBehaviour
    {
        [SerializeField] private KeyCode _toggleKey = KeyCode.V;

        private IFurnitureService? _furnitureService;
        private bool _isBuildMode;
        private int _selectedIndex;
        private static readonly HashSet<BuildModeController> Controllers = new();
        public static bool IsAnyBuildModeEnabled
        {
            get { foreach (var controller in Controllers) if (controller != null && controller._isBuildMode) return true; return false; }
        }
        private void OnEnable() => Controllers.Add(this);
        private void OnDisable() { Controllers.Remove(this); _isBuildMode = false; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetControllers() => Controllers.Clear();

        public bool IsBuildModeEnabled => _isBuildMode;

        private void Awake()
        {
            _furnitureService = FindFirstObjectByType<FurnitureService>();
        }

        private void Update()
        {
            if (GameplayInputBlock.IsBlocked) return;
            if (Input.GetKeyDown(_toggleKey))
            {
                _isBuildMode = !_isBuildMode;
            }

            if (!_isBuildMode || _furnitureService is null)
            {
                return;
            }

            IReadOnlyList<FurnitureDefinitionSO> palette = _furnitureService.GetBuildPalette();
            if (palette.Count == 0)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                _selectedIndex = 0;
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                _selectedIndex = Mathf.Min(1, palette.Count - 1);
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _selectedIndex = (_selectedIndex + 1) % palette.Count;
            }

            Vector2 world = Camera.main is null
                ? Vector2.zero
                : Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // When pointer is over the UI viewport, the viewport bridge owns the click
            // and forwards the translated world point back into build mode.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                _ = TryHandleViewportWorldPoint(world, isPrimaryAction: true);
            }
            else if (Input.GetMouseButtonDown(1))
            {
                _ = TryHandleViewportWorldPoint(world, isPrimaryAction: false);
            }
        }

        public bool TryHandleViewportWorldPoint(Vector2 worldPoint, bool isPrimaryAction)
        {
            if (!_isBuildMode || _furnitureService is null)
            {
                return false;
            }

            IReadOnlyList<FurnitureDefinitionSO> palette = _furnitureService.GetBuildPalette();
            if (palette.Count == 0)
            {
                return false;
            }

            if (isPrimaryAction)
            {
                FurnitureDefinitionSO selected = palette[Mathf.Clamp(_selectedIndex, 0, palette.Count - 1)];
                return _furnitureService.TryPlaceFurniture(selected, worldPoint, 0f, out Furniture? _, out string _);
            }

            return _furnitureService.TryRemoveNearestFurniture(worldPoint, 1.2f, out string _);
        }
    }
}
