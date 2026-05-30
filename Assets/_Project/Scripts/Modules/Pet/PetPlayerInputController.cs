#nullable enable
using UnityEngine;

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
        [SerializeField, Min(0f)] private float _moveSpeed = 2.5f;

        public bool InputEnabled => _enableInput && isActiveAndEnabled && ReferenceEquals(s_activeController, this);

        public float MoveSpeed => _moveSpeed;

        public bool IsActiveController => ReferenceEquals(s_activeController, this);

        public void TakeControl()
        {
            if (!_enableInput || !isActiveAndEnabled)
            {
                return;
            }

            s_activeController = this;
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

            if (s_activeController == null || _preferControlOnEnable)
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
                if (controller == null || !controller._enableInput || !controller.isActiveAndEnabled)
                {
                    continue;
                }

                s_activeController = controller;
                return;
            }
        }
    }
}
