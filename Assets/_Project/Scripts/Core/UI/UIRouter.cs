#nullable enable
using System.Collections.Generic;
using GeminiLab.Core.Events;
using UnityEngine;

namespace GeminiLab.Core.UI
{
    /// <summary>
    /// 内存级 UI Router：维护一个面板栈。
    /// 不做 Prefab 实例化 / 销毁，面板生命周期由注册方负责。
    /// </summary>
    public sealed class UIRouter : IUIRouter
    {
        private readonly Dictionary<PanelId, IUIPanel> _panels = new();
        private readonly List<PanelId> _stack = new();
        private readonly EventBus? _eventBus;

        public UIRouter(EventBus? eventBus = null)
        {
            _eventBus = eventBus;
        }

        public PanelId? Top => _stack.Count == 0 ? null : _stack[_stack.Count - 1];

        public void Register(IUIPanel panel)
        {
            if (panel is null)
            {
                return;
            }

            _panels[panel.Id] = panel;
        }

        public void Unregister(PanelId id)
        {
            _panels.Remove(id);
            _stack.Remove(id);
        }

        public bool Open(PanelId id, object? payload = null)
        {
            if (!_panels.TryGetValue(id, out IUIPanel? panel))
            {
                Debug.LogWarning($"[UIRouter] 未注册的面板：{id}");
                return false;
            }

            if (IsRoomRelicPopup(id))
            {
                // 遗留物弹窗覆盖当前页面；关闭后继续留在原房间视口。
                if (Top.HasValue && IsRoomRelicPopup(Top.Value)) CloseTop();
            }
            else if (_stack.Count > 0)
            {
                CloseAll();
            }

            _stack.Add(id);
            panel.OnOpen(payload);
            _eventBus?.Publish(new UIPanelOpenedEvent(id));
            return true;
        }

        private static bool IsRoomRelicPopup(PanelId id)
        {
            return id == PanelId.RoomNote || id == PanelId.RoomRelicDetail || id == PanelId.RoomGiftObtained;
        }

        public bool Close(PanelId id)
        {
            int index = _stack.IndexOf(id);
            if (index < 0)
            {
                return false;
            }

            _stack.RemoveAt(index);
            if (_panels.TryGetValue(id, out IUIPanel? panel))
            {
                panel.OnClose();
            }

            _eventBus?.Publish(new UIPanelClosedEvent(id));
            return true;
        }

        public bool CloseTop()
        {
            if (_stack.Count == 0)
            {
                return false;
            }

            PanelId top = _stack[_stack.Count - 1];
            return Close(top);
        }

        public void CloseAll()
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                PanelId id = _stack[i];
                if (_panels.TryGetValue(id, out IUIPanel? panel))
                {
                    panel.OnClose();
                }

                _eventBus?.Publish(new UIPanelClosedEvent(id));
            }

            _stack.Clear();
        }
    }

    public readonly struct UIPanelOpenedEvent
    {
        public PanelId Id { get; }
        public UIPanelOpenedEvent(PanelId id) { Id = id; }
    }

    public readonly struct UIPanelClosedEvent
    {
        public PanelId Id { get; }
        public UIPanelClosedEvent(PanelId id) { Id = id; }
    }
}
