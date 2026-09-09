#nullable enable
using System.Collections.Generic;
using GeminiLab.Core.Persistence;

namespace GeminiLab.Modules.Apple
{
    /// <summary>
    /// 苹果资源门面。苹果是独立于金币的可持久化资源，所有消费方只能通过此接口增删。
    /// </summary>
    public interface IAppleService : IPersistentService
    {
        int Balance { get; }
        int InitialBalance { get; }
        int GenerationIntervalMinutes { get; }
        int GenerationIntervalMinMinutes { get; }
        int GenerationIntervalMaxMinutes { get; }
        int MaxRoundsPerDay { get; }
        /// <summary>兼容旧调用方的属性；新版需求不再限制未领取缓存上限。</summary>
        int MaxPendingPerTree { get; }

        void Add(int amount);
        bool TrySpend(int amount);
        void EnsureTree(string treeId);
        int GetPendingCount(string treeId);
        int ShakeTree(string treeId);
        bool TryBeginHarvest(string treeId, out int total);
        bool TryCollectHarvest(string treeId, int amount);
        int GetHarvestRemaining(string treeId);
        IReadOnlyList<AppleTreeState> GetTreeStates();
    }
}
