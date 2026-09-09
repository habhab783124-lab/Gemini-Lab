#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.Time;
using GeminiLab.Modules.Pet.Social;
using UnityEngine;

namespace GeminiLab.Modules.RoomRelic
{
    /// <summary>
    /// 在 Apartment 场景中创建并注册 <see cref="RoomRelicService"/>。
    /// 由场景作者化脚本挂在 RoomRelic 根节点上；不创建任何视觉对象。
    /// </summary>
    [DefaultExecutionOrder(-350)]
    public sealed class RoomRelicRuntimeBootstrap : MonoBehaviour
    {
        [SerializeField] private RoomRelicCatalogSO? _catalog;

        private RoomRelicService? _service;

        private void Start()
        {
            if (_catalog == null)
            {
                Debug.LogWarning("[RoomRelicBootstrap] Catalog 未绑定，跳过初始化。", this);
                return;
            }

            if (ServiceLocator.TryResolve(out IRoomRelicService? existing) && existing is not null)
            {
                _service = existing as RoomRelicService;
                _service?.Resume();
                return;
            }

            if (!ServiceLocator.TryResolve(out IGameClock? clock) || clock is null ||
                !ServiceLocator.TryResolve(out IPetSocialService? social) || social is null)
            {
                Debug.LogWarning("[RoomRelicBootstrap] 缺少 IGameClock 或 IPetSocialService，跳过初始化。", this);
                return;
            }

            _service = new RoomRelicService(clock, social, _catalog);
            ServiceLocator.Register<IRoomRelicService>(_service);

            if (ServiceLocator.TryResolve(out IPersistentServiceRegistry? registry) && registry is not null)
            {
                registry.Register(_service);
            }
        }

        private void OnDestroy()
        {
            _service?.Dispose();
        }
    }
}
