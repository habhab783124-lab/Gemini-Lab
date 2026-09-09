#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI
{
    /// <summary>
    /// 挂到 Button 所在 GameObject 上，点击时打开指定 PanelId。
    /// 若 IUIRouter 尚未注册则自动创建。
    /// </summary>
    public sealed class PanelOpenButton : MonoBehaviour
    {
        [SerializeField] private PanelId _panelId;

        private void Awake()
        {
            var btn = GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OnClick);
        }

        public void OnClick()
        {
            var router = ResolveOrCreateRouter();
            router?.CloseAll();
            router?.Open(ResolvePanelIdForCompatibility());
        }

        private PanelId ResolvePanelIdForCompatibility()
        {
            // 旧版邮箱作者化曾把 enumValueIndex(12) 写入场景，而不是实际值 29。
            // 场景已修正为 29；这里兼容仍在内存中的旧对象，避免本次编辑器会话点击失效。
            if ((int)_panelId == 12 && !Enum.IsDefined(typeof(PanelId), _panelId))
            {
                return PanelId.DailySummaryMailbox;
            }

            return _panelId;
        }

        private static IUIRouter? ResolveOrCreateRouter()
        {
            if (ServiceLocator.TryResolve(out IUIRouter? router))
                return router;

            var eventBus = new EventBus();
            ServiceLocator.Register(eventBus);
            router = new UIRouter(eventBus);
            ServiceLocator.Register<IUIRouter>(router);
            return router;
        }
    }
}
