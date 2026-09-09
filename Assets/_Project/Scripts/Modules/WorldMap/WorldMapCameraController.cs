#nullable enable
using GeminiLab.Core;
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// WorldMap 场景的横板摄像头控制器。
    /// 未选中桌宠时：A/D 键、左键拖拽平移。
    /// 选中桌宠时：自动跟随桌宠 X 坐标（平滑插值）。
    /// 点击空白区域取消选中桌宠。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class WorldMapCameraController : MonoBehaviour
    {
        [Header("移动范围（世界坐标 X 上下限）")]
        [SerializeField] private float _minX = -20f;
        [SerializeField] private float _maxX = 20f;

        [Header("键盘输入")]
        [SerializeField] private float _keyboardSpeed = 5f;
        [SerializeField] private KeyCode _leftKey = KeyCode.A;
        [SerializeField] private KeyCode _rightKey = KeyCode.D;

        [Header("拖拽")]
        [SerializeField] private float _dragSpeed = 1f;

        [Header("跟随")]
        [SerializeField] private float _followSmoothTime = 0.3f;

        [Header("箭头按钮滚动")]
        [SerializeField] private float _arrowScrollSpeed = 5f;
        [SerializeField] private float _arrowSmoothTime = 0.25f;

        private Camera? _camera;
        private Vector3? _lastDragMouseWorld;
        private float _followVelocity;
        private float _scrollTargetX;
        private float _scrollVelocity;
        private bool _hasScrollTarget;
        private bool _blockLeftDragUntilMouseUp;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Update()
        {
            if (_camera is null) return;

            // 左键点击非桌宠区域 → 取消选中（必须在跟随/平移分支之前，否则选中状态下不会执行）
            if (Input.GetMouseButtonDown(0))
            {
                WorldMapSceneInteractionRouter? interactionRouter = WorldMapSceneInteractionRouter.Active;
                bool pointerBlocked = interactionRouter != null
                    ? interactionRouter.IsPointerBlockedByUI()
                    : ClickOcclusionUtility.IsPointerOverUI();
                bool clickedOutdoorPet = !pointerBlocked && interactionRouter != null &&
                    interactionRouter.TryGetTargetAtScreenPoint(
                        Input.mousePosition,
                        out IWorldMapSceneClickTarget? clickTarget) &&
                    clickTarget is WorldMapPetInteractionController;
                if (!pointerBlocked && !clickedOutdoorPet)
                {
                    WorldMapPetInteractionController.ReleaseAllControl();
                    _blockLeftDragUntilMouseUp = true;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                _blockLeftDragUntilMouseUp = false;
            }

            Transform? followTarget = WorldMapPetInteractionController.ActiveTransform;

            Vector3 pos = transform.position;

            if (followTarget != null)
            {
                // 跟随模式：平滑跟随选中桌宠的 X 坐标
                float targetX = followTarget.position.x;
                pos.x = Mathf.SmoothDamp(pos.x, targetX, ref _followVelocity, _followSmoothTime);
                pos.x = Mathf.Clamp(pos.x, _minX, _maxX);
                transform.position = pos;
                _lastDragMouseWorld = null;
                return;
            }

            // 自由平移模式：键盘 A/D
            float delta = 0f;

            if (Input.GetKey(_leftKey)) delta -= _keyboardSpeed * Time.unscaledDeltaTime;
            if (Input.GetKey(_rightKey)) delta += _keyboardSpeed * Time.unscaledDeltaTime;

            // 左键拖拽平移（不穿透 UI）
            bool allowLeftDrag = Input.GetMouseButton(0) && !_blockLeftDragUntilMouseUp;
            if (allowLeftDrag
                && !ClickOcclusionUtility.IsPointerOverUI())
            {
                Vector3 cur = _camera.ScreenToWorldPoint(Input.mousePosition);
                if (_lastDragMouseWorld.HasValue)
                {
                    delta += (_lastDragMouseWorld.Value.x - cur.x) * _dragSpeed;
                }
                _lastDragMouseWorld = cur;
            }
            else
            {
                _lastDragMouseWorld = null;
            }

            bool hasManualInput = Mathf.Abs(delta) > 0.0001f;

            // 手动输入时取消平滑滚动目标
            if (hasManualInput)
            {
                _hasScrollTarget = false;
            }

            // 平滑滚动模式：箭头按钮设置的 target
            if (_hasScrollTarget && !hasManualInput)
            {
                pos.x = Mathf.SmoothDamp(pos.x, _scrollTargetX, ref _scrollVelocity, _arrowSmoothTime);
                pos.x = Mathf.Clamp(pos.x, _minX, _maxX);
                transform.position = pos;

                if (Mathf.Abs(pos.x - _scrollTargetX) < 0.01f)
                {
                    pos.x = _scrollTargetX;
                    transform.position = pos;
                    _hasScrollTarget = false;
                }
                return;
            }

            pos.x = Mathf.Clamp(pos.x + delta, _minX, _maxX);
            transform.position = pos;
        }

        public void SetBounds(float minX, float maxX)
        {
            _minX = minX;
            _maxX = maxX;
        }

        public void ScrollLeft()
        {
            _scrollTargetX = Mathf.Clamp(transform.position.x - _arrowScrollSpeed, _minX, _maxX);
            _hasScrollTarget = true;
            _scrollVelocity = 0f;
        }

        public void ScrollRight()
        {
            _scrollTargetX = Mathf.Clamp(transform.position.x + _arrowScrollSpeed, _minX, _maxX);
            _hasScrollTarget = true;
            _scrollVelocity = 0f;
        }
    }
}
