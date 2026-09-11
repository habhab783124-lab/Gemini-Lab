using UnityEngine;
using Spine;
using Spine.Unity;

namespace GeminiLab.Modules.DesktopOverlay
{
    public class PetRandomAnimationController : MonoBehaviour
    {
        [Header("Spine")]
        [SerializeField]
        private SkeletonAnimation skeletonAnimation;

        [Header("Idle")]
        [SerializeField]
        private string idleAnimation = "idle";

        [Header("Random Animations")]
        [SerializeField]
        private string[] randomAnimations;

        [Header("Random Interval")]
        [SerializeField]
        private float minInterval = 8f;

        [SerializeField]
        private float maxInterval = 18f;


        private float nextPlayTime;
        private int lastAnimationIndex = -1;


        private void Awake()
        {
            if (skeletonAnimation == null)
            {
                skeletonAnimation =
                    GetComponentInChildren<SkeletonAnimation>();
            }
        }


        private void OnEnable()
        {
            ScheduleNext();
        }


        private void Update()
        {
            if (skeletonAnimation == null)
                return;

            if (randomAnimations == null ||
                randomAnimations.Length == 0)
                return;

            if (Time.unscaledTime < nextPlayTime)
                return;


            TrackEntry current =
                skeletonAnimation.AnimationState.GetCurrent(0);

            string currentAnimationName =
                current?.Animation?.Name;


            // 只有当前真正处于 idle 时，
            // 才允许触发随机状态动画。
            if (currentAnimationName != idleAnimation)
            {
                ScheduleNext();
                return;
            }


            string randomAnimation =
                GetRandomAnimation();

            if (string.IsNullOrEmpty(randomAnimation))
            {
                ScheduleNext();
                return;
            }


            // 播放一次随机动作
            skeletonAnimation.AnimationState.SetAnimation(
                0,
                randomAnimation,
                false
            );

            // 播完自动回 idle
            skeletonAnimation.AnimationState.AddAnimation(
                0,
                idleAnimation,
                true,
                0f
            );


            ScheduleNext();
        }


        private void ScheduleNext()
        {
            float min = Mathf.Min(
                minInterval,
                maxInterval
            );

            float max = Mathf.Max(
                minInterval,
                maxInterval
            );

            nextPlayTime =
                Time.unscaledTime +
                Random.Range(min, max);
        }


        private string GetRandomAnimation()
        {
            if (randomAnimations.Length == 1)
            {
                return ValidateAnimation(
                    randomAnimations[0],
                    0
                );
            }


            // 尽量避免连续两次播放同一个动画
            for (int i = 0;
                 i < randomAnimations.Length * 2;
                 i++)
            {
                int index =
                    Random.Range(
                        0,
                        randomAnimations.Length
                    );

                if (index == lastAnimationIndex)
                    continue;

                string animation =
                    ValidateAnimation(
                        randomAnimations[index],
                        index
                    );

                if (!string.IsNullOrEmpty(animation))
                {
                    return animation;
                }
            }


            return null;
        }


        private string ValidateAnimation(
            string animationName,
            int index
        )
        {
            if (string.IsNullOrEmpty(animationName))
                return null;

            if (skeletonAnimation.Skeleton.Data
                .FindAnimation(animationName) == null)
            {
                Debug.LogWarning(
                    $"{name} 找不到随机动画：{animationName}"
                );

                return null;
            }


            lastAnimationIndex = index;

            return animationName;
        }
    }
}
