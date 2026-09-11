using UnityEngine;

namespace GeminiLab.Modules.DesktopOverlay
{
    public class DesktopPetContextMenu : MonoBehaviour
    {
        public static DesktopPetContextMenu Instance { get; private set; }

        [SerializeField]
        private RectTransform menuRoot;

        [SerializeField]
        private Canvas canvas;

        // 记录菜单打开的帧
        // 防止右键打开菜单后，同一帧又被关闭
        private int showFrame = -1;


        private void Awake()
        {
            Instance = this;

            if (canvas == null && menuRoot != null)
            {
                canvas = menuRoot.GetComponentInParent<Canvas>();
            }

            if (menuRoot != null)
            {
                menuRoot.gameObject.SetActive(false);
            }
        }


        private void Update()
        {
            if (menuRoot == null)
                return;

            // 菜单没有打开，不处理
            if (!menuRoot.gameObject.activeSelf)
                return;

            // 打开菜单的这一帧不要检测关闭
            if (Time.frameCount == showFrame)
                return;

            // 左键或者右键点击
            if (Input.GetMouseButtonDown(0) ||
                Input.GetMouseButtonDown(1))
            {
                // 点在菜单外面
                if (!IsPointerInsideMenu())
                {
                    Hide();
                }
            }
        }


        public void Show(Vector2 screenPosition)
        {
            if (menuRoot == null || canvas == null)
                return;

            // 记住打开菜单的这一帧
            showFrame = Time.frameCount;

            menuRoot.gameObject.SetActive(true);

            RectTransform canvasRect =
                canvas.transform as RectTransform;

            Camera uiCamera =
                canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : canvas.worldCamera;

            if (
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPosition,
                    uiCamera,
                    out Vector2 localPoint
                )
            )
            {
                // 继续保持你现在的：
                // 菜单出现在鼠标附近
                menuRoot.anchoredPosition = localPoint;
            }
        }


        public void Hide()
        {
            if (menuRoot != null)
            {
                menuRoot.gameObject.SetActive(false);
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Hide();
            }
        }



        private bool IsPointerInsideMenu()
        {
            if (menuRoot == null)
                return false;

            Camera uiCamera = null;

            if (canvas != null &&
                canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = canvas.worldCamera;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(
                menuRoot,
                Input.mousePosition,
                uiCamera
            );
        }


        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
