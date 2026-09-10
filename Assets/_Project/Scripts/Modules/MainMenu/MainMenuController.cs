#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.SceneFlow;
using GeminiLab.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.MainMenu
{
    /// <summary>
    /// 主菜单场景控制器。三个入口按钮：开始 / 存档 / 设置。
    /// 按钮图像由 UIArtCatalog 提供，代码只认 Button 引用。
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("入口按钮")]
        [SerializeField] private Button? _startButton;
        [SerializeField] private Button? _saveSlotsButton;
        [SerializeField] private Button? _settingsButton;

        private ISceneFlowService? _sceneFlow;
        private IUIRouter? _uiRouter;
        private const string ProloguePlayedKey = "HasPlayedPrologue";

        private void Awake()
        {
            ServiceLocator.TryResolve(out _sceneFlow);
            ServiceLocator.TryResolve(out _uiRouter);

            if (_startButton is not null)
            {
                _startButton.onClick.AddListener(OnStartClicked);
            }

            if (_saveSlotsButton is not null)
            {
                _saveSlotsButton.onClick.AddListener(OnSaveSlotsClicked);
            }

            if (_settingsButton is not null)
            {
                _settingsButton.onClick.AddListener(OnSettingsClicked);
            }
        }

        private void OnDestroy()
        {
            if (_startButton is not null)
            {
                _startButton.onClick.RemoveListener(OnStartClicked);
            }

            if (_saveSlotsButton is not null)
            {
                _saveSlotsButton.onClick.RemoveListener(OnSaveSlotsClicked);
            }

            if (_settingsButton is not null)
            {
                _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            }
        }

        private void OnStartClicked()
        {
            Debug.Log("[MainMenu] 按钮点击成功，进入 OnStartClicked");
            if (_sceneFlow is null && !ServiceLocator.TryResolve(out _sceneFlow))
            {
                Debug.LogError("[MainMenu] 未找到 ISceneFlowService，无法进入公寓场景");
                return;
            }

            Debug.Log("[MainMenu] SceneFlow 获取成功");

            bool hasPlayedPrologue =
                PlayerPrefs.GetInt(ProloguePlayedKey, 0) == 1;

            Debug.Log("[MainMenu] HasPlayedPrologue = " + hasPlayedPrologue);

            if (hasPlayedPrologue)
            {
                Debug.Log("[MainMenu] 准备跳转 Apartment");
                _sceneFlow!.LoadAsync(SceneId.Apartment);
            }
            else
            {
                Debug.Log("[MainMenu] 准备跳转 Prologue");
                _sceneFlow!.LoadAsync(SceneId.Prologue);
            }

            _sceneFlow!.LoadAsync(SceneId.Apartment);
        }

        private void OnSaveSlotsClicked()
        {
            if (_uiRouter is null && !ServiceLocator.TryResolve(out _uiRouter))
            {
                Debug.LogWarning("[MainMenu] 未找到 IUIRouter，跳过存档面板");
                return;
            }

            _uiRouter!.Open(PanelId.SaveSlots);
        }

        private void OnSettingsClicked()
        {
            if (_uiRouter is null && !ServiceLocator.TryResolve(out _uiRouter))
            {
                Debug.LogWarning("[MainMenu] 未找到 IUIRouter，跳过设置面板");
                return;
            }

            _uiRouter!.Open(PanelId.Settings);
        }
    }
}
