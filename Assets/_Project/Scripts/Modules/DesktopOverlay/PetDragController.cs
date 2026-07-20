using UnityEngine;
using Spine.Unity;

[RequireComponent(typeof(Collider2D))]
public class PetDragController : MonoBehaviour
{
    [Header("Drag Target")]
    public Transform target;
    public Camera dragCamera;

    [Header("Spine Animation")]
    public SkeletonAnimation skeletonAnimation;
    public string idleAnimation = "idle";
    public string dragAnimation = "drag";
    public string clickAnimation = "click";

    [Header("Click Judge")]
    public float clickMaxMovePixels = 8f;
    public float clickMaxTime = 0.25f;

    [Header("Position Save")]
    public bool loadSavedPositionOnStart = true;
    public bool savePositionOnRelease = true;
    public string saveKey = "";


    private Vector3 offset;
    private bool isDragging;
    private bool hasMovedEnough;
    private Vector3 mouseDownScreenPos;
    private float mouseDownTime;
    private string currentAnimation;

    private void Awake()
    {
        if (target == null)
            target = transform;

        if (dragCamera == null)
            dragCamera = Camera.main;

        if (skeletonAnimation == null)
            skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
    }

        private void Start()
    {
        if (string.IsNullOrEmpty(saveKey))
            saveKey = gameObject.name;

        if (loadSavedPositionOnStart)
            LoadPosition();
    }


    private void OnMouseDown()
    {
        if (dragCamera == null)
            dragCamera = Camera.main;

        mouseDownScreenPos = Input.mousePosition;
        mouseDownTime = Time.time;
        hasMovedEnough = false;
        isDragging = true;

        Vector3 mouseWorld = GetMouseWorldPosition();
        offset = target.position - mouseWorld;
    }

    private void OnMouseDrag()
    {
        if (!isDragging)
            return;

        float moveDistance = Vector3.Distance(Input.mousePosition, mouseDownScreenPos);

        if (!hasMovedEnough && moveDistance > clickMaxMovePixels)
        {
            hasMovedEnough = true;
            PlayLoop(dragAnimation);
        }

        Vector3 mouseWorld = GetMouseWorldPosition();
        Vector3 newPosition = mouseWorld + offset;

        target.position = newPosition;
    }

    private void OnMouseUp()
    {
        if (!isDragging)
            return;

        isDragging = false;

        float moveDistance = Vector3.Distance(Input.mousePosition, mouseDownScreenPos);
        float holdTime = Time.time - mouseDownTime;

        bool isClick = moveDistance <= clickMaxMovePixels && holdTime <= clickMaxTime;

        if (isClick)
        {
            PlayClickThenIdle();
        }
        else
        {
            PlayLoop(idleAnimation);
        }

        if (savePositionOnRelease)
        {
            SavePosition();
        }

    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 screenPos = Input.mousePosition;

        Vector3 targetScreenPos = dragCamera.WorldToScreenPoint(target.position);
        screenPos.z = targetScreenPos.z;

        Vector3 worldPos = dragCamera.ScreenToWorldPoint(screenPos);
        worldPos.z = target.position.z;

        return worldPos;
    }

    private void PlayLoop(string animationName)
    {
        if (skeletonAnimation == null)
            return;

        if (string.IsNullOrEmpty(animationName))
            return;

        if (currentAnimation == animationName)
            return;

        if (skeletonAnimation.Skeleton.Data.FindAnimation(animationName) == null)
        {
            Debug.LogWarning($"{name} 找不到动画：{animationName}");
            return;
        }

        skeletonAnimation.AnimationState.SetAnimation(0, animationName, true);
        currentAnimation = animationName;
    }

    private void PlayClickThenIdle()
    {
        if (skeletonAnimation == null)
            return;

        if (string.IsNullOrEmpty(clickAnimation))
        {
            PlayLoop(idleAnimation);
            return;
        }

        if (skeletonAnimation.Skeleton.Data.FindAnimation(clickAnimation) == null)
        {
            Debug.LogWarning($"{name} 找不到点击动画：{clickAnimation}");
            PlayLoop(idleAnimation);
            return;
        }

        skeletonAnimation.AnimationState.SetAnimation(0, clickAnimation, false);
        currentAnimation = clickAnimation;

        if (!string.IsNullOrEmpty(idleAnimation) &&
            skeletonAnimation.Skeleton.Data.FindAnimation(idleAnimation) != null)
        {
            skeletonAnimation.AnimationState.AddAnimation(0, idleAnimation, true, 0f);
        }
    }

    private string SavePrefix
{
    get
    {
        return "DesktopPet_" + saveKey + "_";
    }
}

private void SavePosition()
{
    if (target == null)
        return;

    PlayerPrefs.SetFloat(SavePrefix + "x", target.position.x);
    PlayerPrefs.SetFloat(SavePrefix + "y", target.position.y);
    PlayerPrefs.SetFloat(SavePrefix + "z", target.position.z);
    PlayerPrefs.Save();

    Debug.Log($"{name} 已保存位置：{target.position}");
}

    private void LoadPosition()
    {
        if (target == null)
            return;

        if (!PlayerPrefs.HasKey(SavePrefix + "x"))
            return;

        float x = PlayerPrefs.GetFloat(SavePrefix + "x");
        float y = PlayerPrefs.GetFloat(SavePrefix + "y");
        float z = PlayerPrefs.GetFloat(SavePrefix + "z");

        target.position = new Vector3(x, y, z);

        Debug.Log($"{name} 已读取位置：{target.position}");
    }

    [ContextMenu("Clear Saved Position")]
    private void ClearSavedPosition()
    {
        PlayerPrefs.DeleteKey(SavePrefix + "x");
        PlayerPrefs.DeleteKey(SavePrefix + "y");
        PlayerPrefs.DeleteKey(SavePrefix + "z");
        PlayerPrefs.Save();

        Debug.Log($"{name} 已清除保存位置");
    }

}
