#nullable enable
using GeminiLab.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Reads local keyboard input for direct player-controlled pet movement.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PetPlayerInputController : MonoBehaviour
    {
        private static PetPlayerInputController? s_activeController;

        [SerializeField] private bool _enableInput = true;
        [SerializeField] private bool _preferControlOnEnable;
        [SerializeField] private bool _acceptWasd = true;
        [SerializeField] private bool _acceptArrowKeys = true;
        [SerializeField] private bool _horizontalOnly = false;
        [SerializeField, Min(0f)] private float _moveSpeed = 2.5f;
        [SerializeField] private bool _allowLegacyMouseTakeover = true;

        public static Transform? ActiveTransform => s_activeController != null ? s_activeController.transform : null;

        public bool InputEnabled => _enableInput && isActiveAndEnabled && ReferenceEquals(s_activeController, this);

        public float MoveSpeed => _moveSpeed;

        public bool HorizontalOnly => _horizontalOnly;

        public bool IsActiveController => ReferenceEquals(s_activeController, this);

        public void TakeControl()
        {
            if (!_enableInput || !isActiveAndEnabled)
            {
                return;
            }

            s_activeController = this;
        }

        /// <summary>
        /// 取消所有宠物的选中状态，使双方都回到自由漫游模式。
        /// </summary>
        public static void ReleaseAllControl()
        {
            s_activeController = null;
        }

        private void Awake()
        {
            TryBecomeActiveController();
            Debug.Log($"[PetInput] Awake on '{gameObject.name}' — IsActiveController={IsActiveController}");
        }

        private void OnEnable()
        {
            TryBecomeActiveController();
            Debug.Log($"[PetInput] OnEnable on '{gameObject.name}' — IsActiveController={IsActiveController}, _preferControlOnEnable={_preferControlOnEnable}");
        }

        private void OnDisable()
        {
            if (!ReferenceEquals(s_activeController, this))
            {
                return;
            }

            s_activeController = null;
            PromoteFallbackController();
        }

        private void OnMouseDown()
        {
            // WorldMap 的唯一点击处理者是 WorldMapSceneInteractionRouter。
            // 保留默认 true 以兼容室内场景；WorldMap 不再让旧 OnMouseDown 与路由竞争。
            if (!_allowLegacyMouseTakeover ||
                string.Equals(SceneManager.GetActiveScene().name, "WorldMap_Main", System.StringComparison.Ordinal))
            {
                return;
            }

            if (ClickOcclusionUtility.IsPointerOverUI())
            {
                return;
            }

            if (!ClickOcclusionUtility.IsTopmostColliderUnderMouse(GetComponent<Collider2D>()))
            {
                return;
            }

            TakeControl();
        }

        public bool TryGetMovementInput(out Vector2 movement)
        {
            return TryGetMovementInput(out movement, out _);
        }

        public bool TryGetMovementInput(out Vector2 movement, out Vector2 rawInput)
        {
            if (!InputEnabled)
            {
                movement = default;
                rawInput = default;
                return false;
            }

            rawInput = ReadRawInputVector(_acceptWasd, _acceptArrowKeys);
            if (_horizontalOnly) rawInput.y = 0f;
            movement = rawInput.sqrMagnitude > 1f ? rawInput.normalized : rawInput;
            return rawInput.sqrMagnitude > 0.0001f;
        }

        public static Vector2 ComposeMovementVector(bool left, bool right, bool up, bool down)
        {
            Vector2 rawInput = ComposeRawInputVector(left, right, up, down);
            return rawInput.sqrMagnitude > 1f
                ? rawInput.normalized
                : rawInput;
        }

        public static Vector2 ComposeRawInputVector(bool left, bool right, bool up, bool down)
        {
            float horizontal = 0f;
            if (left)
            {
                horizontal -= 1f;
            }

            if (right)
            {
                horizontal += 1f;
            }

            float vertical = 0f;
            if (down)
            {
                vertical -= 1f;
            }

            if (up)
            {
                vertical += 1f;
            }

            return new Vector2(horizontal, vertical);
        }

        private static Vector2 ReadRawInputVector(bool acceptWasd, bool acceptArrowKeys)
        {
            bool left = (acceptWasd && Input.GetKey(KeyCode.A)) || (acceptArrowKeys && Input.GetKey(KeyCode.LeftArrow));
            bool right = (acceptWasd && Input.GetKey(KeyCode.D)) || (acceptArrowKeys && Input.GetKey(KeyCode.RightArrow));
            bool up = (acceptWasd && Input.GetKey(KeyCode.W)) || (acceptArrowKeys && Input.GetKey(KeyCode.UpArrow));
            bool down = (acceptWasd && Input.GetKey(KeyCode.S)) || (acceptArrowKeys && Input.GetKey(KeyCode.DownArrow));
            return ComposeRawInputVector(left, right, up, down);
        }

        private void TryBecomeActiveController()
        {
            if (!_enableInput || !isActiveAndEnabled)
            {
                return;
            }

            // 自由行走场景默认不抢占控制权；只有明确标记 PreferControlOnEnable
            // 的公寓实例才会在启用时接管键盘。点击桌宠仍会通过 TakeControl() 接管。
            if (_preferControlOnEnable)
            {
                s_activeController = this;
            }
        }

        private static void PromoteFallbackController()
        {
            PetPlayerInputController[] controllers = FindObjectsByType<PetPlayerInputController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < controllers.Length; i++)
            {
                PetPlayerInputController controller = controllers[i];
                if (controller == null || !controller._enableInput || !controller._preferControlOnEnable || !controller.isActiveAndEnabled)
                {
                    continue;
                }

                s_activeController = controller;
                return;
            }
        }
    }
}
