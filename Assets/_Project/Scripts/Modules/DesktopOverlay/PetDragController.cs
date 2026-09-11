using UnityEngine;
using Spine.Unity;

namespace GeminiLab.Modules.DesktopOverlay
{
    [RequireComponent(typeof(Collider2D))]
    public class PetDragController : MonoBehaviour
    {
        // =========================================================
        // Drag
        // =========================================================

        [Header("Drag Target")]
        [SerializeField]
        private Transform target;

        [SerializeField]
        private Camera dragCamera;


        // =========================================================
        // Spine Animation
        // =========================================================

        [Header("Spine Animation")]
        [SerializeField]
        private SkeletonAnimation skeletonAnimation;

        [SerializeField]
        private string idleAnimation = "idle";

        [SerializeField]
        private string dragAnimation = "drag";

        [SerializeField]
        private string clickAnimation = "click";


        // =========================================================
        // Click Judge
        // =========================================================

        [Header("Click Judge")]
        [SerializeField]
        private float clickMaxMovePixels = 8f;

        [SerializeField]
        private float clickMaxTime = 0.25f;


        // =========================================================
        // Position Save
        // =========================================================

        [Header("Position Save")]

        [Tooltip("Angel / Devil 必须使用不同的 Key。留空时自动使用物体名字。")]
        [SerializeField]
        private string saveKey;

        [SerializeField]
        private bool loadSavedPositionOnStart = true;


        // =========================================================
        // Screen Edge
        // =========================================================

        [Header("World Edge Protection")]

        [Tooltip("桌宠与 Camera 边缘保留的世界坐标距离。")]
        [SerializeField]
        private float edgeMargin = 0.05f;


        // =========================================================
        // Debug
        // =========================================================

        [Header("Debug")]
        [SerializeField]
        private bool debugPosition = false;


        // =========================================================
        // Runtime
        // =========================================================

        private Collider2D petCollider;

        private Vector3 offset;

        private bool isDragging;
        private bool hasMovedEnough;

        private Vector3 mouseDownScreenPos;
        private float mouseDownTime;

        private string currentAnimation;


        // =========================================================
        // Save Key
        // =========================================================

        private string SavePrefix =>
            $"DesktopOverlay_PetPosition_{saveKey}_";


        // =========================================================
        // Unity
        // =========================================================

        private void Awake()
        {
            if (target == null)
            {
                target = transform;
            }

            if (dragCamera == null)
            {
                dragCamera = Camera.main;
            }

            if (skeletonAnimation == null)
            {
                skeletonAnimation =
                    GetComponentInChildren<SkeletonAnimation>();
            }

            petCollider =
                GetComponent<Collider2D>();

            if (string.IsNullOrEmpty(saveKey))
            {
                saveKey =
                    gameObject.name;
            }
        }


        private void Start()
        {
            if (loadSavedPositionOnStart)
            {
                LoadPosition();
            }
        }


        // =========================================================
        // Mouse
        // =========================================================

        private void OnMouseDown()
        {
            if (dragCamera == null)
            {
                dragCamera =
                    Camera.main;
            }

            if (
                dragCamera == null ||
                target == null
            )
            {
                return;
            }


            mouseDownScreenPos =
                Input.mousePosition;

            mouseDownTime =
                Time.time;

            hasMovedEnough =
                false;

            isDragging =
                true;


            Vector3 mouseWorld =
                GetMouseWorldPosition();

            offset =
                target.position -
                mouseWorld;
        }


        private void OnMouseDrag()
        {
            if (!isDragging)
            {
                return;
            }

            if (
                target == null ||
                dragCamera == null
            )
            {
                return;
            }


            float moveDistance =
                Vector3.Distance(
                    Input.mousePosition,
                    mouseDownScreenPos
                );


            if (
                !hasMovedEnough &&
                moveDistance > clickMaxMovePixels
            )
            {
                hasMovedEnough =
                    true;

                PlayLoop(
                    dragAnimation
                );
            }


            Vector3 mouseWorld =
                GetMouseWorldPosition();

            Vector3 newPosition =
                mouseWorld +
                offset;


            // ★ 只在用户拖动的时候限制边界
            newPosition =
                ClampDragPosition(
                    newPosition
                );


            target.position =
                newPosition;
        }


        private void OnMouseUp()
        {
            if (!isDragging)
            {
                return;
            }


            isDragging =
                false;


            float moveDistance =
                Vector3.Distance(
                    Input.mousePosition,
                    mouseDownScreenPos
                );

            float holdTime =
                Time.time -
                mouseDownTime;


            bool isClick =
                moveDistance <= clickMaxMovePixels &&
                holdTime <= clickMaxTime;


            if (isClick)
            {
                PlayClickThenIdle();
            }
            else
            {
                PlayLoop(
                    idleAnimation
                );

                // ★ 松手以后直接保存当前世界坐标
                SavePosition();
            }
        }


        // =========================================================
        // Mouse Position
        // =========================================================

        private Vector3 GetMouseWorldPosition()
        {
            Vector3 screenPos =
                Input.mousePosition;


            Vector3 targetScreenPos =
                dragCamera.WorldToScreenPoint(
                    target.position
                );


            screenPos.z =
                targetScreenPos.z;


            Vector3 worldPos =
                dragCamera.ScreenToWorldPoint(
                    screenPos
                );


            worldPos.z =
                target.position.z;


            return worldPos;
        }


        // =========================================================
        // Simple World Boundary
        // =========================================================

        private Vector3 ClampDragPosition(
            Vector3 desiredPosition
        )
        {
            if (
                dragCamera == null ||
                target == null ||
                petCollider == null
            )
            {
                return desiredPosition;
            }


            // 你的桌宠场景使用正交 Camera。
            // 如果不是正交 Camera，就不做这个限制。
            if (!dragCamera.orthographic)
            {
                return desiredPosition;
            }


            // =====================================================
            // Camera 世界范围
            // =====================================================

            float cameraHalfHeight =
                dragCamera.orthographicSize;

            float cameraHalfWidth =
                cameraHalfHeight *
                dragCamera.aspect;


            Vector3 cameraPosition =
                dragCamera.transform.position;


            float cameraLeft =
                cameraPosition.x -
                cameraHalfWidth +
                edgeMargin;

            float cameraRight =
                cameraPosition.x +
                cameraHalfWidth -
                edgeMargin;

            float cameraBottom =
                cameraPosition.y -
                cameraHalfHeight +
                edgeMargin;

            float cameraTop =
                cameraPosition.y +
                cameraHalfHeight -
                edgeMargin;


            // =====================================================
            // Collider 当前世界尺寸
            // =====================================================

            Bounds bounds =
                petCollider.bounds;


            float halfWidth =
                bounds.extents.x;

            float halfHeight =
                bounds.extents.y;


            // Collider 中心不一定刚好等于 target.position，
            // 所以保留两者之间的偏移。
            Vector3 colliderCenterOffset =
                bounds.center -
                target.position;


            // =====================================================
            // 计算 Collider 中心允许的位置
            // =====================================================

            float minColliderCenterX =
                cameraLeft +
                halfWidth;

            float maxColliderCenterX =
                cameraRight -
                halfWidth;

            float minColliderCenterY =
                cameraBottom +
                halfHeight;

            float maxColliderCenterY =
                cameraTop -
                halfHeight;


            Vector3 desiredColliderCenter =
                desiredPosition +
                colliderCenterOffset;


            // 如果角色本身比 Camera 还大，
            // 就让它的中心保持在 Camera 中间。
            if (
                minColliderCenterX >
                maxColliderCenterX
            )
            {
                desiredColliderCenter.x =
                    cameraPosition.x;
            }
            else
            {
                desiredColliderCenter.x =
                    Mathf.Clamp(
                        desiredColliderCenter.x,
                        minColliderCenterX,
                        maxColliderCenterX
                    );
            }


            if (
                minColliderCenterY >
                maxColliderCenterY
            )
            {
                desiredColliderCenter.y =
                    cameraPosition.y;
            }
            else
            {
                desiredColliderCenter.y =
                    Mathf.Clamp(
                        desiredColliderCenter.y,
                        minColliderCenterY,
                        maxColliderCenterY
                    );
            }


            // Collider 中心转换回 target.position
            Vector3 correctedPosition =
                desiredColliderCenter -
                colliderCenterOffset;


            correctedPosition.z =
                desiredPosition.z;


            return correctedPosition;
        }


        // =========================================================
        // Position Save
        // =========================================================

        private void SavePosition()
        {
            if (target == null)
            {
                return;
            }


            PlayerPrefs.SetFloat(
                SavePrefix + "x",
                target.position.x
            );

            PlayerPrefs.SetFloat(
                SavePrefix + "y",
                target.position.y
            );

            PlayerPrefs.SetFloat(
                SavePrefix + "z",
                target.position.z
            );


            PlayerPrefs.Save();


            if (debugPosition)
            {
                Debug.Log(
                    $"[PetDrag] SAVE {saveKey} | " +
                    $"Position = {target.position}"
                );
            }
        }


        // =========================================================
        // Position Load
        // =========================================================

        private void LoadPosition()
        {
            if (target == null)
            {
                return;
            }


            // 没有世界坐标存档时，
            // 使用 Scene 里的默认位置。
            if (
                !PlayerPrefs.HasKey(
                    SavePrefix + "x"
                ) ||
                !PlayerPrefs.HasKey(
                    SavePrefix + "y"
                )
            )
            {
                if (debugPosition)
                {
                    Debug.Log(
                        $"[PetDrag] {saveKey} 没有世界坐标存档，" +
                        "使用 Scene 默认位置。"
                    );
                }

                return;
            }


            float x =
                PlayerPrefs.GetFloat(
                    SavePrefix + "x",
                    target.position.x
                );

            float y =
                PlayerPrefs.GetFloat(
                    SavePrefix + "y",
                    target.position.y
                );

            float z =
                PlayerPrefs.GetFloat(
                    SavePrefix + "z",
                    target.position.z
                );


            // ★ 只恢复。
            // 不 Clamp。
            // 不 Viewport 换算。
            // 不重新计算。
            target.position =
                new Vector3(
                    x,
                    y,
                    z
                );


            if (debugPosition)
            {
                Debug.Log(
                    $"[PetDrag] LOAD {saveKey} | " +
                    $"Position = {target.position}"
                );
            }
        }


        // =========================================================
        // Spine Animation
        // =========================================================

        private void PlayLoop(
            string animationName
        )
        {
            if (skeletonAnimation == null)
            {
                return;
            }

            if (
                string.IsNullOrEmpty(
                    animationName
                )
            )
            {
                return;
            }

            if (
                currentAnimation ==
                animationName
            )
            {
                return;
            }


            if (
                skeletonAnimation
                    .Skeleton
                    .Data
                    .FindAnimation(
                        animationName
                    ) == null
            )
            {
                Debug.LogWarning(
                    $"{name} 找不到动画：" +
                    $"{animationName}"
                );

                return;
            }


            skeletonAnimation
                .AnimationState
                .SetAnimation(
                    0,
                    animationName,
                    true
                );


            currentAnimation =
                animationName;
        }


        private void PlayClickThenIdle()
        {
            if (skeletonAnimation == null)
            {
                return;
            }


            if (
                string.IsNullOrEmpty(
                    clickAnimation
                )
            )
            {
                PlayLoop(
                    idleAnimation
                );

                return;
            }


            if (
                skeletonAnimation
                    .Skeleton
                    .Data
                    .FindAnimation(
                        clickAnimation
                    ) == null
            )
            {
                Debug.LogWarning(
                    $"{name} 找不到点击动画：" +
                    $"{clickAnimation}"
                );

                PlayLoop(
                    idleAnimation
                );

                return;
            }


            skeletonAnimation
                .AnimationState
                .SetAnimation(
                    0,
                    clickAnimation,
                    false
                );


            currentAnimation =
                clickAnimation;


            if (
                !string.IsNullOrEmpty(
                    idleAnimation
                ) &&
                skeletonAnimation
                    .Skeleton
                    .Data
                    .FindAnimation(
                        idleAnimation
                    ) != null
            )
            {
                skeletonAnimation
                    .AnimationState
                    .AddAnimation(
                        0,
                        idleAnimation,
                        true,
                        0f
                    );
            }
        }
    }
}
