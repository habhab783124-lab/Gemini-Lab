#nullable enable
using System;
using System.Collections.Generic;

namespace GeminiLab.Core.Persistence
{
    /// <summary>
    /// 统一收口所有需要进存档的 <see cref="IPersistentService"/> 实例。
    ///
    /// 为什么不直接用 <see cref="ServiceLocator"/>：
    /// - ServiceLocator 以类型为 key，没有"枚举所有实现 IPersistentService 的服务"的能力
    /// - 多个接口可能共用一个实现类（例如 SettingsService 既是 ISettingsService 也是 IPersistentService），
    ///   不能靠反射扫 ServiceLocator 的 Type 集合
    ///
    /// 使用方式：
    /// - GameBootstrap 注册默认实现到 ServiceLocator
    /// - 每个业务 Bootstrap 在注册自身 Service 后调 <see cref="Register"/>
    /// - Phase E 的 SaveCoordinator 通过这里枚举所有参与存档的服务
    /// </summary>
    public interface IPersistentServiceRegistry
    {
        /// <summary>服务注册完成后通知读档协调器，恢复延迟进入场景的模块。</summary>
        event Action<IPersistentService> Registered;

        /// <summary>注册一个参与存档的服务。相同 Key 会覆盖。</summary>
        void Register(IPersistentService service);

        /// <summary>注销（例如服务 OnDestroy 时）。</summary>
        void Unregister(string key);

        /// <summary>按 Key 取出；未找到返回 null。</summary>
        IPersistentService? TryGet(string key);

        /// <summary>当前所有已注册的服务。</summary>
        IReadOnlyCollection<IPersistentService> All { get; }
    }
}
