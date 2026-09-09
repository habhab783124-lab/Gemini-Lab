#nullable enable
using System;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.Pet.Social;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    /// <summary>交流系统测试（数值规则文档 §14-19）。</summary>
    public sealed class PetSocialServiceTests
    {
        private PetRoster _roster = null!;
        private PetRuntimeData _angel = null!;
        private PetRuntimeData _devil = null!;
        private DateTime _now;

        [SetUp]
        public void SetUp()
        {
            _roster = new PetRoster();
            _angel = new PetRuntimeData { Energy = 80f, Mood = 60f };
            _devil = new PetRuntimeData { Energy = 80f, Mood = 60f };
            _roster.Register(PetId.Angel, _angel);
            _roster.Register(PetId.Devil, _devil);
            _now = new DateTime(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);
        }

        private PetSocialService CreateService() => new(_roster, utcNow: () => _now);

        [Test]
        public void InitialState_MatchesDoc()
        {
            var service = CreateService();

            Assert.AreEqual(30f, service.Friendship, 0.001f); // §14/§23：初始 30
            Assert.AreEqual("普通", service.FriendshipStageLabel); // 20~39 普通
        }

        [Test]
        public void StageLabels_MatchDocBands()
        {
            Assert.AreEqual("疏远", PetSocialService.GetStageLabel(0f));
            Assert.AreEqual("疏远", PetSocialService.GetStageLabel(19.9f));
            Assert.AreEqual("普通", PetSocialService.GetStageLabel(20f));
            Assert.AreEqual("熟悉", PetSocialService.GetStageLabel(40f));
            Assert.AreEqual("亲近", PetSocialService.GetStageLabel(60f));
            Assert.AreEqual("高亲密", PetSocialService.GetStageLabel(80f));
        }

        [Test]
        public void CanInitiate_ForbiddenBelowEnergy10()
        {
            var service = CreateService();
            _angel.Energy = 9.9f;
            Assert.IsFalse(service.CanInitiate(PetId.Angel)); // §16

            _angel.Energy = 10f;
            Assert.IsTrue(service.CanInitiate(PetId.Angel));
        }

        [Test]
        public void ResolveResponseType_NeedSpaceHasHighestPriority()
        {
            var service = CreateService();
            service.ApplySpecialEventFriendship(50f); // 亲密度 80，本应是 WARM

            _devil.Energy = 20f; // §15：对方 Energy<30 → NEED_SPACE 优先于 WARM
            Assert.AreEqual(SocialResponseType.NeedSpace, service.ResolveResponseType(PetId.Devil));

            _devil.Energy = 80f;
            _devil.Mood = 20f; // 对方 Mood<30 → 同样 NEED_SPACE
            Assert.AreEqual(SocialResponseType.NeedSpace, service.ResolveResponseType(PetId.Devil));
        }

        [Test]
        public void ResolveResponseType_WarmRequiresFriendship60()
        {
            var service = CreateService();
            Assert.AreEqual(SocialResponseType.Normal, service.ResolveResponseType(PetId.Devil));

            service.ApplySpecialEventFriendship(30f); // 30 → 60
            Assert.AreEqual(SocialResponseType.Warm, service.ResolveResponseType(PetId.Devil));
        }

        [Test]
        public void TrySocialize_InitiatorTooTired_NothingHappens()
        {
            var service = CreateService();
            _angel.Energy = 5f;

            PetSocialOutcome outcome = service.TrySocialize(PetId.Angel, PetId.Devil);

            Assert.IsFalse(outcome.Initiated);
            Assert.AreEqual(5f, _angel.Energy, 0.001f);
            Assert.AreEqual(80f, _devil.Energy, 0.001f);
            Assert.AreEqual(30f, service.Friendship, 0.001f);
        }

        [Test]
        public void TrySocialize_NeedSpace_AppliesDocDeltas()
        {
            var service = CreateService();
            _devil.Energy = 20f; // 触发 NEED_SPACE

            PetSocialOutcome outcome = service.TrySocialize(PetId.Angel, PetId.Devil);

            // §17：双方 E0 / M0 / F+0
            Assert.IsTrue(outcome.Initiated);
            Assert.AreEqual(SocialResponseType.NeedSpace, outcome.ResponseType);
            Assert.AreEqual(80f, _angel.Energy, 0.001f);
            Assert.AreEqual(60f, _angel.Mood, 0.001f);
            Assert.AreEqual(20f, _devil.Energy, 0.001f);
            Assert.AreEqual(60f, _devil.Mood, 0.001f);
            Assert.AreEqual(30f, service.Friendship, 0.001f);
            Assert.IsFalse(outcome.FriendshipGainApplied);
        }

        [Test]
        public void TrySocialize_Normal_AppliesDocDeltasAndPublishesEvent()
        {
            var service = CreateService();
            PetSocialInteractionEvent? captured = null;
            service.SocialInteractionCompleted += e => captured = e;

            PetSocialOutcome outcome = service.TrySocialize(PetId.Angel, PetId.Devil);

            // §17：双方 E-1 / M+1 / F+1
            Assert.AreEqual(SocialResponseType.Normal, outcome.ResponseType);
            Assert.AreEqual(79f, _angel.Energy, 0.001f);
            Assert.AreEqual(61f, _angel.Mood, 0.001f);
            Assert.AreEqual(79f, _devil.Energy, 0.001f);
            Assert.AreEqual(61f, _devil.Mood, 0.001f);
            Assert.AreEqual(31f, service.Friendship, 0.001f);
            Assert.IsTrue(outcome.FriendshipGainApplied);

            Assert.NotNull(captured);
            Assert.AreEqual(PetId.Angel, captured!.Value.Initiator);
            Assert.AreEqual(PetId.Devil, captured.Value.Target);
            Assert.AreEqual(1f, captured.Value.FriendshipDelta, 0.001f);
            Assert.AreEqual(31f, captured.Value.Friendship, 0.001f);
        }

        [Test]
        public void TrySocialize_Warm_AppliesDocDeltas()
        {
            var service = CreateService();
            service.ApplySpecialEventFriendship(40f); // 30 → 70 ≥ 60 → WARM

            PetSocialOutcome outcome = service.TrySocialize(PetId.Angel, PetId.Devil);

            // §17：双方 E-2 / M+3 / F+2
            Assert.AreEqual(SocialResponseType.Warm, outcome.ResponseType);
            Assert.AreEqual(63f, _angel.Mood, 0.001f);
            Assert.AreEqual(63f, _devil.Mood, 0.001f);
            Assert.AreEqual(72f, service.Friendship, 0.001f);
        }

        [Test]
        public void TrySocialize_AntiFarmCooldown_OnlyBlocksFriendship()
        {
            var service = CreateService();

            service.TrySocialize(PetId.Angel, PetId.Devil); // F 30 → 31，开始 300s 冷却
            _now = _now.AddSeconds(100);

            PetSocialOutcome second = service.TrySocialize(PetId.Angel, PetId.Devil);

            // §18：冷却中心情/精力照常结算，亲密度不变。
            Assert.IsTrue(second.Initiated);
            Assert.IsFalse(second.FriendshipGainApplied);
            Assert.AreEqual(0f, second.FriendshipDelta, 0.001f);
            Assert.AreEqual(31f, service.Friendship, 0.001f);
            Assert.AreEqual(62f, _angel.Mood, 0.001f); // 两次 M+1 都生效

            _now = _now.AddSeconds(201); // 距上次获得 301s
            PetSocialOutcome third = service.TrySocialize(PetId.Angel, PetId.Devil);
            Assert.IsTrue(third.FriendshipGainApplied);
            Assert.AreEqual(32f, service.Friendship, 0.001f);
        }

        [Test]
        public void SpecialEventFriendship_BypassesCooldownAndDoesNotRefreshIt()
        {
            var service = CreateService();

            service.TrySocialize(PetId.Angel, PetId.Devil); // F 31，冷却从 t0 起算
            _now = _now.AddSeconds(10);
            service.ApplySpecialEventFriendship(5f); // §19：不受冷却限制
            Assert.AreEqual(36f, service.Friendship, 0.001f);

            _now = _now.AddSeconds(10);
            PetSocialOutcome outcome = service.TrySocialize(PetId.Angel, PetId.Devil);
            // 特殊事件不刷新冷却：仍按 t0 起算，t0+20 在冷却中。
            Assert.IsFalse(outcome.FriendshipGainApplied);
            Assert.AreEqual(36f, service.Friendship, 0.001f);
        }

        [Test]
        public void Friendship_ClampedToDocRange()
        {
            var service = CreateService();

            service.ApplySpecialEventFriendship(500f);
            Assert.AreEqual(100f, service.Friendship, 0.001f);

            service.ApplySpecialEventFriendship(-500f);
            Assert.AreEqual(0f, service.Friendship, 0.001f);
        }

        [Test]
        public void Persistence_RoundTrip_PreservesFriendshipAndCooldown()
        {
            var service = CreateService();
            service.TrySocialize(PetId.Angel, PetId.Devil); // F 31 @ t0
            string json = service.CaptureJson();

            var restored = new PetSocialService(_roster, utcNow: () => _now);
            Assert.IsTrue(restored.RestoreJson(json));
            Assert.AreEqual(31f, restored.Friendship, 0.001f);

            // 冷却截止时刻也随存档恢复：t0+100 仍在冷却中。
            _now = _now.AddSeconds(100);
            PetSocialOutcome outcome = restored.TrySocialize(PetId.Angel, PetId.Devil);
            Assert.IsFalse(outcome.FriendshipGainApplied);
        }

        [Test]
        public void Persistence_CorruptJson_FailsAndKeepsState()
        {
            var service = CreateService();

            Assert.IsFalse(service.RestoreJson("{not json"));
            Assert.IsFalse(service.RestoreJson(string.Empty));
            Assert.AreEqual(30f, service.Friendship, 0.001f);
        }

        [Test]
        public void TrySocialize_MissingPet_NotInitiated()
        {
            var roster = new PetRoster(); // 空 roster
            var service = new PetSocialService(roster, utcNow: () => _now);

            Assert.IsFalse(service.TrySocialize(PetId.Angel, PetId.Devil).Initiated);
            Assert.IsFalse(service.CanInitiate(PetId.Angel));
        }
    }
}
