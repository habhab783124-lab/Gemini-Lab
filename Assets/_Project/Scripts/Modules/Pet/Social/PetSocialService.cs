#nullable enable
using System;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using UnityEngine;

namespace GeminiLab.Modules.Pet.Social
{
    /// <summary>
    /// 双宠物交流系统默认实现（数值规则文档 §14-19）。
    /// - 亲密度为“对子”级数据，0-100，初始 30，无自然衰减（§14）
    /// - 回应类型：对方 Energy&lt;30 或 Mood&lt;30 → NEED_SPACE（最高优先级）；
    ///   否则 Friendship&gt;=60 → WARM；其余 NORMAL（§15）
    /// - 发起者 Energy&lt;10 不允许发起（§16）
    /// - 结算表（§17）：NEED_SPACE 双方 E0/M0、F+0；NORMAL 双方 E-1/M+1、F+1；WARM 双方 E-2/M+3、F+2
    /// - 防刷：获得亲密度后 300s 内再次交流 FriendshipDelta=0（心情/精力照常结算，§18）
    /// - 特殊事件直接增减亲密度，不受防刷限制（§19）
    /// 实现 <see cref="IPersistentService"/>（Key = "pet_social"），由 PetRuntimeBootstrap 注册。
    /// </summary>
    public sealed class PetSocialService : IPetSocialService, IPersistentService
    {
        public const float FriendshipMin = 0f;
        public const float FriendshipMax = 100f;
        public const float InitialFriendship = 30f;
        public const float NeedSpaceEnergyThreshold = 30f;
        public const float NeedSpaceMoodThreshold = 30f;
        public const float WarmFriendshipThreshold = 60f;
        public const float InitiatorMinEnergy = 10f;
        public const float FriendshipGainCooldownSeconds = 300f;

        private readonly IPetRoster _roster;
        private readonly EventBus? _eventBus;
        private readonly Func<DateTime> _utcNow;

        private float _friendship = InitialFriendship;
        private DateTime? _lastFriendshipGainUtc;

        public PetSocialService(IPetRoster roster, EventBus? eventBus = null, Func<DateTime>? utcNow = null)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _eventBus = eventBus;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public float Friendship => _friendship;

        public string FriendshipStageLabel => GetStageLabel(_friendship);

        /// <summary>§14 阶段划分：0-19 疏远 / 20-39 普通 / 40-59 熟悉 / 60-79 亲近 / 80-100 高亲密。</summary>
        public static string GetStageLabel(float friendship)
        {
            if (friendship < 20f) return "疏远";
            if (friendship < 40f) return "普通";
            if (friendship < 60f) return "熟悉";
            if (friendship < 80f) return "亲近";
            return "高亲密";
        }

        public bool CanInitiate(PetId initiator)
        {
            var data = _roster.TryGet(initiator);
            return data != null && data.Energy >= InitiatorMinEnergy;
        }

        public SocialResponseType ResolveResponseType(PetId target)
        {
            var data = _roster.TryGet(target);
            if (data == null) return SocialResponseType.NeedSpace;
            if (data.Energy < NeedSpaceEnergyThreshold || data.Mood < NeedSpaceMoodThreshold)
            {
                return SocialResponseType.NeedSpace;
            }
            return _friendship >= WarmFriendshipThreshold ? SocialResponseType.Warm : SocialResponseType.Normal;
        }

        public PetSocialOutcome TrySocialize(PetId initiator, PetId target)
        {
            if (initiator == target) return PetSocialOutcome.NotInitiated;

            var initiatorData = _roster.TryGet(initiator);
            var targetData = _roster.TryGet(target);
            if (initiatorData == null || targetData == null) return PetSocialOutcome.NotInitiated;

            // §16：发起者精力不足，交流不发生，所有数值不变。
            if (initiatorData.Energy < InitiatorMinEnergy) return PetSocialOutcome.NotInitiated;

            SocialResponseType response = ResolveResponseType(target);

            // §17 结算表。
            float initiatorEnergyDelta, initiatorMoodDelta, targetEnergyDelta, targetMoodDelta, friendshipGain;
            switch (response)
            {
                case SocialResponseType.NeedSpace:
                    initiatorEnergyDelta = 0f; initiatorMoodDelta = 0f;
                    targetEnergyDelta = 0f; targetMoodDelta = 0f;
                    friendshipGain = 0f;
                    break;
                case SocialResponseType.Warm:
                    initiatorEnergyDelta = -2f; initiatorMoodDelta = 3f;
                    targetEnergyDelta = -2f; targetMoodDelta = 3f;
                    friendshipGain = 2f;
                    break;
                default: // Normal
                    initiatorEnergyDelta = -1f; initiatorMoodDelta = 1f;
                    targetEnergyDelta = -1f; targetMoodDelta = 1f;
                    friendshipGain = 1f;
                    break;
            }

            StatTickService.ApplyEnvironmentalBuff(initiatorData, initiatorMoodDelta, initiatorEnergyDelta);
            StatTickService.ApplyEnvironmentalBuff(targetData, targetMoodDelta, targetEnergyDelta);

            // §18：防刷冷却只影响亲密度获得，心情/精力上面已照常结算。
            bool gainAllowed = friendshipGain > 0f && IsFriendshipGainAllowed();
            float appliedDelta = gainAllowed ? friendshipGain : 0f;
            if (gainAllowed)
            {
                SetFriendship(_friendship + appliedDelta);
                _lastFriendshipGainUtc = _utcNow();
            }

            var outcome = new PetSocialOutcome(
                true, response,
                initiatorEnergyDelta, initiatorMoodDelta,
                targetEnergyDelta, targetMoodDelta,
                appliedDelta, gainAllowed);

            var evt = new PetSocialInteractionEvent(
                initiator, target, response, appliedDelta, _friendship, gainAllowed);
            SocialInteractionCompleted?.Invoke(evt);
            _eventBus?.Publish(evt);

            return outcome;
        }

        public void ApplySpecialEventFriendship(float delta)
        {
            if (Mathf.Approximately(delta, 0f)) return;
            // §19：特殊事件不受防刷冷却限制，也不刷新冷却时间。
            SetFriendship(_friendship + delta);
        }

        public event Action<PetSocialInteractionEvent>? SocialInteractionCompleted;

        /// <summary>亲密度实际变化后触发（含特殊事件）；参数为变化后的值。</summary>
        public event Action<float>? FriendshipChanged;

        private bool IsFriendshipGainAllowed()
        {
            if (_lastFriendshipGainUtc is not { } lastGain) return true;
            return (_utcNow() - lastGain).TotalSeconds >= FriendshipGainCooldownSeconds;
        }

        private void SetFriendship(float value)
        {
            float clamped = Mathf.Clamp(value, FriendshipMin, FriendshipMax);
            if (Mathf.Approximately(clamped, _friendship)) return;
            _friendship = clamped;
            FriendshipChanged?.Invoke(_friendship);
        }

        // ---- IPersistentService ----

        public string Key => "pet_social";

        [Serializable]
        private struct SavePayload
        {
            public int version;
            public float friendship;
            public long lastFriendshipGainUtcTicks; // 0 = 从未获得过
        }

        public string CaptureJson()
        {
            return JsonUtility.ToJson(new SavePayload
            {
                version = 1,
                friendship = _friendship,
                lastFriendshipGainUtcTicks = _lastFriendshipGainUtc?.Ticks ?? 0L
            });
        }

        public bool RestoreJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                var payload = JsonUtility.FromJson<SavePayload>(json);
                _friendship = Mathf.Clamp(payload.friendship, FriendshipMin, FriendshipMax);
                _lastFriendshipGainUtc = payload.lastFriendshipGainUtcTicks > 0L
                    ? new DateTime(payload.lastFriendshipGainUtcTicks, DateTimeKind.Utc)
                    : null;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
