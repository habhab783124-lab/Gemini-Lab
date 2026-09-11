using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.DesktopOverlay
{
    public class DesktopPetSizeController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField]
        private Slider sizeSlider;

        [Header("Pets")]
        [SerializeField]
        private Transform angel;

        [SerializeField]
        private Transform devil;


        // 保存当前大小档位
        private const string SizeLevelKey =
            "DesktopOverlay_PetSizeLevel";

        // 默认档位：2 = 100%
        private const int DefaultSizeLevel = 2;


        // 五档倍率
        private readonly float[] scaleLevels =
        {
            0.6f,
            0.8f,
            1.0f,
            1.2f,
            1.4f
        };


        // 两只桌宠各自原始 Scale
        private Vector3 angelBaseScale;
        private Vector3 devilBaseScale;


        private void Awake()
        {
            if (angel != null)
            {
                angelBaseScale = angel.localScale;
            }

            if (devil != null)
            {
                devilBaseScale = devil.localScale;
            }
        }


        private void OnEnable()
        {
            // 读取之前保存的档位
            int savedLevel = PlayerPrefs.GetInt(
                SizeLevelKey,
                DefaultSizeLevel
            );

            savedLevel = Mathf.Clamp(
                savedLevel,
                0,
                scaleLevels.Length - 1
            );


            // 先恢复 Slider 显示
            if (sizeSlider != null)
            {
                sizeSlider.SetValueWithoutNotify(savedLevel);

                sizeSlider.onValueChanged.AddListener(
                    HandleSizeChanged
                );
            }


            // 再恢复桌宠实际大小
            ApplySize(savedLevel);
        }


        private void OnDisable()
        {
            if (sizeSlider != null)
            {
                sizeSlider.onValueChanged.RemoveListener(
                    HandleSizeChanged
                );
            }
        }


        private void HandleSizeChanged(float value)
        {
            int level = Mathf.Clamp(
                Mathf.RoundToInt(value),
                0,
                scaleLevels.Length - 1
            );

            // 改桌宠大小
            ApplySize(level);

            // 保存档位
            PlayerPrefs.SetInt(
                SizeLevelKey,
                level
            );

            PlayerPrefs.Save();
        }


        private void ApplySize(int level)
        {
            level = Mathf.Clamp(
                level,
                0,
                scaleLevels.Length - 1
            );

            float multiplier =
                scaleLevels[level];


            if (angel != null)
            {
                angel.localScale =
                    angelBaseScale * multiplier;
            }


            if (devil != null)
            {
                devil.localScale =
                    devilBaseScale * multiplier;
            }
        }
    }
}
