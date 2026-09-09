#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.RoomRelic
{
    public abstract class RoomRelicPanelBase : MonoBehaviour, IUIPanel
    {
        [SerializeField] private GameObject? _content;
        [SerializeField] private Button? _closeButton;

        private IUIRouter? _router;
        public abstract PanelId Id { get; }

        protected virtual void Awake()
        {
            if (_content != null && Application.isPlaying)
            {
                _content.SetActive(false);
            }

            _router = ResolveOrCreateRouter();
            _router.Register(this);

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(CloseSelf);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(CloseSelf);
            }

            if (_router != null)
            {
                _router.Unregister(Id);
            }
            else if (ServiceLocator.TryResolve(out IUIRouter? router))
            {
                router.Unregister(Id);
            }
        }

        public virtual void OnOpen(object? payload)
        {
            if (_content != null)
            {
                _content.SetActive(true);
            }
        }

        public virtual void OnClose()
        {
            if (_content != null)
            {
                _content.SetActive(false);
            }
        }

        protected void CloseSelf()
        {
            _router?.Close(Id);
        }

        private static IUIRouter ResolveOrCreateRouter()
        {
            if (ServiceLocator.TryResolve(out IUIRouter? router) && router is not null)
            {
                return router;
            }

            if (!ServiceLocator.TryResolve(out EventBus? eventBus) || eventBus is null)
            {
                eventBus = new EventBus();
                ServiceLocator.Register(eventBus);
            }

            router = new UIRouter(eventBus);
            ServiceLocator.Register<IUIRouter>(router);
            return router;
        }
    }
}
