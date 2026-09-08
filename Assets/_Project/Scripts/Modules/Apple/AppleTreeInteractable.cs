#nullable enable
using GeminiLab.Core;
using UnityEngine;

namespace GeminiLab.Modules.Apple
{
    /// <summary>
    /// WorldMap 苹果树的唯一树点击入口。
    /// 命中区域完全由树对象自身作者化的 Collider2D 决定。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class AppleTreeInteractable : MonoBehaviour, IWorldMapSceneClickTarget
    {
        [SerializeField] private string _treeId = string.Empty;
        [SerializeField] private AppleTreeFeedback? _feedback;
        [SerializeField] private AppleTreeDropController? _dropController;
        [SerializeField] private int _interactionPriority = 15;

        private Collider2D? _collider;
        private Renderer? _renderer;

        public string TreeId => _treeId;
        public bool IsWorldMapInteractionEnabled =>
            isActiveAndEnabled &&
            _collider != null &&
            _collider.enabled &&
            !string.IsNullOrWhiteSpace(_treeId);
        public int WorldMapInteractionPriority => _interactionPriority;
        public Renderer? WorldMapSortingRenderer => _renderer;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _renderer = GetComponent<Renderer>();
        }

        private void Start()
        {
            // Start runs after all scene Awake methods and after the core
            // bootstrap has registered IGameClock in direct WorldMap entry.
            AppleRuntimeBootstrap.InitializeCurrentScene();
        }

        public bool ContainsWorldPoint(Vector2 worldPoint)
        {
            return IsWorldMapInteractionEnabled && _collider!.OverlapPoint(worldPoint);
        }

        public void HandleWorldMapClick()
        {
            bool interactionEnabled = IsWorldMapInteractionEnabled;
            Debug.Log(
                $"[AppleTreeClick][Entered] object={name} tree={_treeId} " +
                $"enabled={interactionEnabled} collider={_collider != null && _collider.enabled} " +
                $"dropController={_dropController != null}",
                this);

            bool bootstrapInitialized = AppleRuntimeBootstrap.InitializeCurrentScene();
            Debug.Log(
                $"[AppleTreeClick][Bootstrap] tree={_treeId} initialized={bootstrapInitialized}",
                this);

            if (_dropController == null)
            {
                Debug.LogWarning(
                    $"[AppleTreeClick][Blocked] tree={_treeId} reason=missing-drop-controller",
                    this);
                _feedback?.ShowNotReady();
                return;
            }

            bool started = _dropController.TryBeginHarvest();
            Debug.Log(
                $"[AppleTreeClick][Result] tree={_treeId} started={started} " +
                $"shaking={_dropController.IsShaking}",
                this);
        }
    }
}
