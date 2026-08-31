#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.UI;
using GeminiLab.Modules.EmotionGarden;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 每日情绪输入面板。读取心情文本，提交后生成对应的情绪花。
    /// </summary>
    public sealed class EmotionInputPanelStub : StubPanelBase
    {
        public override PanelId Id => PanelId.EmotionInput;

        [Header("UI 控件")]
        [SerializeField] private TMP_InputField? _inputField;
        [SerializeField] private Button? _submitButton;
        [SerializeField] private TMP_Text? _statusText;
        [SerializeField] private TMP_Text? _ownerText;

        [Header("培育者主题（由 Scene 预先作者化）")]
        [SerializeField] private GameObject? _angelTheme;
        [SerializeField] private GameObject? _demonTheme;
        [SerializeField] private Button? _angelOwnerButton;
        [SerializeField] private Button? _demonOwnerButton;

        private IEmotionGardenService? _service;
        private IUIRouter? _router;
        private string _owner = EmotionFlowerCatalog.OwnerAngel;

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);

            if (payload is string owner)
            {
                _owner = EmotionFlowerCatalog.NormalizeOwner(owner);
            }

            RefreshOwnerTheme();

            _service ??= ServiceLocator.TryResolve(out IEmotionGardenService? service) ? service : null;
            _router ??= ServiceLocator.TryResolve(out IUIRouter? router) ? router : null;

            if (_service == null)
            {
                SetInteractable(false);
                if (_statusText != null) _statusText.text = "情绪花园服务未就绪";
                return;
            }

            if (!_service.CanSubmitToday())
            {
                SetInteractable(false);
                if (_statusText != null) _statusText.text = "今天已经提交过心情了";
            }
            else
            {
                SetInteractable(true);
                if (_statusText != null) _statusText.text = string.Empty;
            }
        }

        public void OnSubmitClick()
        {
            if (_service == null)
            {
                if (_statusText != null) _statusText.text = "情绪花园服务未就绪";
                return;
            }

            if (!_service.CanSubmitToday())
            {
                if (_statusText != null) _statusText.text = "今天已经提交过心情了";
                return;
            }

            var detail = _inputField != null ? _inputField.text : string.Empty;
            if (string.IsNullOrWhiteSpace(detail))
            {
                if (_statusText != null) _statusText.text = "请输入心情";
                return;
            }

            var flower = _service.SubmitEmotion(string.Empty, detail, _owner);
            if (flower == null)
            {
                if (_statusText != null) _statusText.text = "提交失败";
                return;
            }

            SetInteractable(false);
            if (_statusText != null)
            {
                _statusText.text = $"已生成 {flower.Value.FlowerName}（{flower.Value.EmotionType}）";
            }

            _router?.Open(PanelId.WeeklyGardenView);
        }

        /// <summary>
        /// 切换当前输入心情的培育者。输入面板中的员工卡是可点击的切换入口，
        /// 这样不会额外叠加与参考图不一致的选择器 UI。
        /// </summary>
        public void ToggleOwner()
        {
            SetOwner(_owner == EmotionFlowerCatalog.OwnerAngel
                ? EmotionFlowerCatalog.OwnerDemon
                : EmotionFlowerCatalog.OwnerAngel);
        }

        public void SelectAngel()
        {
            SetOwner(EmotionFlowerCatalog.OwnerAngel);
        }

        public void SelectDemon()
        {
            SetOwner(EmotionFlowerCatalog.OwnerDemon);
        }

        private void SetOwner(string owner)
        {
            _owner = EmotionFlowerCatalog.NormalizeOwner(owner);
            RefreshOwnerTheme();

            if (_service != null && !_service.CanSubmitToday())
            {
                SetInteractable(false);
                if (_statusText != null) _statusText.text = "今天已经提交过心情了";
            }
        }

        private void RefreshOwnerTheme()
        {
            bool angel = _owner == EmotionFlowerCatalog.OwnerAngel;
            if (_angelTheme != null) _angelTheme.SetActive(angel);
            if (_demonTheme != null) _demonTheme.SetActive(!angel);
            if (_ownerText != null)
            {
                _ownerText.text = $"培育者: {EmotionFlowerCatalog.ResolveOwnerDisplayName(_owner)}";
            }
        }

        private void SetInteractable(bool interactable)
        {
            if (_inputField != null) _inputField.interactable = interactable;
            if (_submitButton != null) _submitButton.interactable = interactable;
            if (_angelOwnerButton != null) _angelOwnerButton.interactable = interactable;
            if (_demonOwnerButton != null) _demonOwnerButton.interactable = interactable;
        }

        protected override void Awake()
        {
            base.Awake();
            if (_submitButton != null) _submitButton.onClick.AddListener(OnSubmitClick);
            if (_angelOwnerButton != null) _angelOwnerButton.onClick.AddListener(ToggleOwner);
            if (_demonOwnerButton != null) _demonOwnerButton.onClick.AddListener(ToggleOwner);
        }
    }
}
