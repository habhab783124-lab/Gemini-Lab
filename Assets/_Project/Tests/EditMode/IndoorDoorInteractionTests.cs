using System.Collections.Generic;
using GeminiLab.Core;
using GeminiLab.Modules.HubUI;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.Pet.Social;
using NUnit.Framework;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    public sealed class IndoorDoorInteractionTests
    {
        private readonly List<GameObject> _objects = new();
        private ApartmentDoorInteraction _door;
        private PetPlayerInputController _angelInput;
        private PetPlayerInputController _devilInput;
        private PetRuntimeData _angel;
        private PetRuntimeData _devil;
        private PetSocialService _social;
        private PetDialogueBubble _angelBubble;
        private PetDialogueBubble _devilBubble;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Reset();
            _angel = new PetRuntimeData {Energy=80, Mood=60};
            _devil = new PetRuntimeData {Energy=80, Mood=60};
            var roster = new PetRoster(); roster.Register(PetId.Angel,_angel); roster.Register(PetId.Devil,_devil);
            _social = new PetSocialService(roster); ServiceLocator.Register<IPetSocialService>(_social);
            var angel = New("test angel").AddComponent<PetController>(); _angelInput = angel.gameObject.AddComponent<PetPlayerInputController>();
            var devil = New("test devil").AddComponent<PetController>(); _devilInput = devil.gameObject.AddComponent<PetPlayerInputController>();
            var ds = new SerializedObject(devil); ds.FindProperty("_petId").intValue=(int)PetId.Devil; ds.ApplyModifiedPropertiesWithoutUndo();
            _door = New("test door").AddComponent<ApartmentDoorInteraction>();
            var area = _door.gameObject.AddComponent<BoxCollider2D>(); area.isTrigger=true;
            _angelBubble = Bubble("angel bubble"); _devilBubble = Bubble("devil bubble");
            var open = New("test open"); open.SetActive(false);
            var so = new SerializedObject(_door);
            Bind(so,"_angel",angel); Bind(so,"_devil",devil); Bind(so,"_angelApproach",_door.transform); Bind(so,"_devilApproach",_door.transform);
            Bind(so,"_angelBubble",_angelBubble); Bind(so,"_devilBubble",_devilBubble); Bind(so,"_clickArea",area); Bind(so,"_openVisual",open);
            Bind(so,"_dialogues",Catalog()); so.ApplyModifiedPropertiesWithoutUndo();
            var angelRoom = New("angel room").AddComponent<BoxCollider2D>();
            angelRoom.isTrigger = true; angelRoom.size = new Vector2(10, 10); angelRoom.transform.position = new Vector3(6, 0);
            var devilRoom = New("devil room").AddComponent<BoxCollider2D>();
            devilRoom.isTrigger = true; devilRoom.size = new Vector2(10, 10); devilRoom.transform.position = new Vector3(-6, 0);
            so.Update(); Bind(so, "_angelRoom", angelRoom); Bind(so, "_devilRoom", devilRoom); so.ApplyModifiedPropertiesWithoutUndo();
            _angelInput.TakeControl(); Physics2D.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            PetPlayerInputController.ReleaseAllControl();
            foreach (var item in _objects) Object.DestroyImmediate(item);
            _objects.Clear(); ServiceLocator.Reset();
        }

        [Test]
        public void ClosedDoorOrDistantPetCannotSettleSocialStats()
        {
            Assert.IsFalse(_door.TryInteract()); Assert.AreEqual(80,_angel.Energy); Assert.IsFalse(_door.IsDialogueVisible);
            _door.SetOpen(true); _angelInput.transform.position = new Vector3(20,20);
            Assert.IsFalse(_door.TryInteract()); Assert.AreEqual(30,_social.Friendship);
        }

        [Test]
        public void ClickingDoorTogglesWithoutSettling_SelectionDoesNotSettle()
        {
            Assert.IsTrue(_door.TryHandleWorldPoint(Vector2.zero)); Assert.IsTrue(_door.IsOpen);
            _devilInput.TakeControl(); _angelInput.TakeControl();
            Assert.IsTrue(_door.TryHandleWorldPoint(Vector2.zero)); Assert.IsFalse(_door.IsOpen);
            Assert.AreEqual(80,_angel.Energy); Assert.AreEqual(80,_devil.Energy); Assert.AreEqual(30,_social.Friendship);
        }

        [TestCase(false)] [TestCase(true)]
        public void EitherPetCanInitiate_ClosingDoesNotSettleTwice(bool devilInitiates)
        {
            if (devilInitiates) _devilInput.TakeControl();
            BeginConversation(devilInitiates); Assert.IsTrue(_door.IsDialogueVisible);
            Assert.AreEqual(80,_angel.Energy); Advance(Deadline("_replyAt"));
            Assert.AreEqual(79,_angel.Energy); Assert.AreEqual(79,_devil.Energy); Assert.AreEqual(61,_angel.Mood); Assert.AreEqual(31,_social.Friendship);
            _door.CloseDialogue(); Assert.IsFalse(_door.IsDialogueVisible);
            Assert.AreEqual(79,_angel.Energy); Assert.AreEqual(31,_social.Friendship);
        }

        [Test]
        public void ClickingBodySelectsPet_WithoutUsingSolidFootprintOrSettling()
        {
            _angelInput.transform.position = new Vector3(50,50);
            var foot = _angelInput.gameObject.AddComponent<CapsuleCollider2D>(); foot.size=Vector2.one; foot.offset=new Vector2(0,-3);
            var body = New("selection area"); body.transform.SetParent(_angelInput.transform,false);
            var area = body.AddComponent<BoxCollider2D>(); area.isTrigger=true; area.size=new Vector2(3.2f,5.5f);
            var click = _angelInput.gameObject.AddComponent<PetClickReactionController>();
            var so = new SerializedObject(click); Bind(so,"_selectionArea",area);
            so.FindProperty("_enableClickReaction").boolValue=false;
            so.FindProperty("_socializeOnClick").boolValue=false; so.ApplyModifiedPropertiesWithoutUndo();
            PetPlayerInputController.ReleaseAllControl(); Physics2D.SyncTransforms();
            Vector2 head = (Vector2)_angelInput.transform.position + Vector2.up;
            Assert.IsFalse(foot.OverlapPoint(head)); Assert.IsTrue(click.TryHandleWorldPoint(head));
            Assert.IsTrue(_angelInput.IsActiveController); Assert.AreEqual(80,_angel.Energy); Assert.AreEqual(30,_social.Friendship);
        }

        [Test]
        public void NeedSpaceChangesNoStats_AndPrecedesWarm()
        {
            _social.ApplySpecialEventFriendship(30); _devil.Mood=29;
            BeginConversation();
            Advance(Deadline("_replyAt"));
            Assert.IsTrue(_door.IsDialogueVisible); Assert.AreEqual(80,_angel.Energy); Assert.AreEqual(80,_devil.Energy);
            Assert.AreEqual(60,_angel.Mood); Assert.AreEqual(29,_devil.Mood); Assert.AreEqual(60,_social.Friendship);
        }

        [Test]
        public void ClosingOccupiedPassageIsRejected_UntilPetLeaves()
        {
            var blocker=New("door blocker").AddComponent<BoxCollider2D>(); blocker.size=new Vector2(1.5f,3.5f);
            var so=new SerializedObject(_door); Bind(so,"_passageBlocker",blocker); so.ApplyModifiedPropertiesWithoutUndo();
            var foot=_angelInput.gameObject.AddComponent<CapsuleCollider2D>(); foot.size=Vector2.one;
            _door.SetOpen(true); Physics2D.SyncTransforms(); Assert.IsFalse(blocker.enabled);
            _door.SetOpen(false); Assert.IsTrue(_door.IsOpen); Assert.IsFalse(blocker.enabled);
            _angelInput.transform.position=new Vector3(5,0); Physics2D.SyncTransforms();
            _door.SetOpen(false); Assert.IsFalse(_door.IsOpen); Assert.IsTrue(blocker.enabled);
        }

        [Test]
        public void VisitorCanStartDialogueFromOtherSideOfDoor()
        {
            var home=New("home approach");home.transform.position=new Vector3(5,0);
            var other=New("other approach");other.transform.position=new Vector3(-5,0);
            var so=new SerializedObject(_door);Bind(so,"_angelApproach",home.transform);Bind(so,"_devilApproach",other.transform);so.ApplyModifiedPropertiesWithoutUndo();
            _angelInput.transform.position=other.transform.position;
            _door.SetOpen(true);Assert.IsTrue(_door.TryInteract());Assert.IsTrue(_door.IsChoiceVisible);
            Assert.IsFalse(_door.IsDialogueVisible); Assert.AreEqual(80,_angel.Energy);
        }

        [TestCase(29,60,60,SocialResponseType.NeedSpace)]
        [TestCase(30,29,60,SocialResponseType.NeedSpace)]
        [TestCase(30,30,59,SocialResponseType.Normal)]
        [TestCase(30,30,60,SocialResponseType.Warm)]
        public void ExactThresholdsUseReceiverValues(float energy,float mood,float friendship,SocialResponseType expected)
        {
            _angel.Energy=10; _angel.Mood=5; _devil.Energy=energy; _devil.Mood=mood;
            _social.ApplySpecialEventFriendship(friendship-30);
            Assert.AreEqual(expected,_social.ResolveResponseType(PetId.Devil));
        }

        [TestCase(PetId.Angel)] [TestCase(PetId.Devil)]
        public void AuthoredFiveTopicsHaveAllResponsesAndCycle(PetId initiator)
        {
            var catalog=Catalog(); var ids=new HashSet<string>();
            for(int i=0;i<5;i++)
            {
                var t=catalog.GetTopic(initiator,i); Assert.NotNull(t); Assert.IsTrue(ids.Add(t.Id));
                Assert.IsNotEmpty(t.Opening); Assert.IsNotEmpty(t.Reply(SocialResponseType.Warm));
                Assert.IsNotEmpty(t.Reply(SocialResponseType.Normal)); Assert.IsNotEmpty(t.Reply(SocialResponseType.NeedSpace));
            }
            Assert.AreSame(catalog.GetTopic(initiator,0),catalog.GetTopic(initiator,5));
        }

        [TestCase(PetId.Angel, SocialResponseType.Warm)]
        [TestCase(PetId.Angel, SocialResponseType.Normal)]
        [TestCase(PetId.Angel, SocialResponseType.NeedSpace)]
        [TestCase(PetId.Devil, SocialResponseType.Warm)]
        [TestCase(PetId.Devil, SocialResponseType.Normal)]
        [TestCase(PetId.Devil, SocialResponseType.NeedSpace)]
        public void SpeechAppearsOnCorrectPetInOrder_ThenExpiresWithoutResettling(PetId initiator, SocialResponseType response)
        {
            bool angel = initiator == PetId.Angel;
            if (!angel) _devilInput.TakeControl();
            if (response == SocialResponseType.Warm) _social.ApplySpecialEventFriendship(30);
            if (response == SocialResponseType.NeedSpace) (angel ? _devil : _angel).Mood = 29;
            var speaker = angel ? _angelBubble : _devilBubble;
            var receiver = angel ? _devilBubble : _angelBubble;
            BeginConversation(!angel);
            var topic = (IndoorDoorDialogueCatalog.Topic)typeof(ApartmentDoorInteraction).GetField("_topic", BindingFlags.Instance|BindingFlags.NonPublic).GetValue(_door);
            Assert.IsTrue(speaker.IsVisible); Assert.IsFalse(receiver.IsVisible);
            Assert.AreEqual(topic.Opening,speaker.Message);
            Assert.AreEqual(80, _angel.Energy);
            Advance(Deadline("_replyAt") - .01f); Assert.IsFalse(receiver.IsVisible);
            Advance(Deadline("_replyAt"));
            Assert.IsTrue(speaker.IsVisible); Assert.IsTrue(receiver.IsVisible);
            Assert.AreEqual(topic.Reply(response),receiver.Message);
            Assert.AreEqual(response == SocialResponseType.Warm ? 78 : response == SocialResponseType.Normal ? 79 : 80, _angel.Energy);
            float energy = _angel.Energy, friendship = _social.Friendship;
            Advance(Deadline("_hideAt"));
            Assert.IsFalse(_door.IsDialogueVisible); Assert.IsFalse(speaker.IsVisible); Assert.IsFalse(receiver.IsVisible);
            Assert.AreEqual(energy,_angel.Energy); Assert.AreEqual(friendship,_social.Friendship);
        }

        [TestCase(false)] [TestCase(true)]
        public void CancelBeforeReplyDoesNotLeaveDelayedBubble(bool disable)
        {
            BeginConversation();
            float later = Deadline("_replyAt") + 1;
            // EditMode 不自动发送普通 MonoBehaviour 的 Play 生命周期消息。
            if (disable)
            {
                _door.enabled=false;
                typeof(ApartmentDoorInteraction).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(_door,null);
            }
            else _door.SetOpen(false);
            Advance(later);
            Assert.IsFalse(_door.IsDialogueVisible); Assert.IsFalse(_angelBubble.IsVisible); Assert.IsFalse(_devilBubble.IsVisible);
            Assert.AreEqual(80,_angel.Energy); Assert.AreEqual(30,_social.Friendship);
            Assert.IsFalse(_angelInput.GetComponent<PetController>().IsConversationPaused);
            Assert.IsFalse(_devilInput.GetComponent<PetController>().IsConversationPaused);
        }

        [Test]
        public void ExhaustedInitiatorOnlyShowsOwnBubble_WithoutReplyOrSettlement()
        {
            _angel.Energy=9; BeginConversation();
            Assert.IsTrue(_angelBubble.IsVisible); Assert.IsFalse(_devilBubble.IsVisible);
            Advance(Deadline("_replyAt") + .1f); Assert.IsFalse(_devilBubble.IsVisible);
            Assert.AreEqual(9,_angel.Energy); Assert.AreEqual(80,_devil.Energy); Assert.AreEqual(30,_social.Friendship);
        }

        [Test]
        public void EnteringRoomOffersChoiceWithoutProximity_DeclineWaitsForReentry()
        {
            _door.SetOpen(true);
            _angelInput.transform.position = new Vector3(6,0); RefreshVisit();
            Assert.IsFalse(_door.IsChoiceVisible);
            _angelInput.transform.position = new Vector3(-10,0);
            _devilInput.transform.position = new Vector3(-2,0); RefreshVisit();
            Assert.IsTrue(_door.IsChoiceVisible); Assert.IsFalse(_door.IsDialogueVisible);
            _door.DeclineConversation(); RefreshVisit();
            Assert.IsFalse(_door.IsChoiceVisible); Assert.AreEqual(80,_angel.Energy); Assert.AreEqual(30,_social.Friendship);
            _angelInput.transform.position = new Vector3(6,0); RefreshVisit();
            _angelInput.transform.position = new Vector3(-10,0); RefreshVisit();
            Assert.IsTrue(_door.IsChoiceVisible);
        }

        [Test]
        public void ConversationPausesBothPets_AndPreservesExternalLockOnExit()
        {
            var angel = _angelInput.GetComponent<PetController>(); var devil = _devilInput.GetComponent<PetController>();
            devil.SetExternalMovementLock(true);
            BeginConversation();
            Assert.IsTrue(angel.IsConversationPaused); Assert.IsTrue(devil.IsConversationPaused);
            _door.AcceptConversation(); // 双击按钮不产生第二场对话。
            Advance(Deadline("_replyAt")); Advance(Deadline("_replyAt") + .01f);
            Assert.AreEqual(79,_angel.Energy);
            Advance(Deadline("_hideAt"));
            Assert.IsFalse(angel.IsMovementLocked); Assert.IsFalse(devil.IsConversationPaused); Assert.IsTrue(devil.IsMovementLocked);
        }

        [Test]
        public void SelectionChangeCancelsPendingExchangeAndRestoresBothPets()
        {
            BeginConversation(); _devilInput.TakeControl(); RefreshVisit();
            Advance(Deadline("_replyAt"));
            Assert.IsFalse(_door.IsDialogueVisible); Assert.AreEqual(80,_angel.Energy);
            Assert.IsFalse(_angelInput.GetComponent<PetController>().IsConversationPaused);
            Assert.IsFalse(_devilInput.GetComponent<PetController>().IsConversationPaused);
        }

        [TestCase(PetId.Angel)] [TestCase(PetId.Devil)]
        public void RandomTopicsStayWithInitiatorAndDoNotImmediatelyRepeat(PetId initiator)
        {
            var catalog = Catalog(); var previous = catalog.GetTopic(initiator,0);
            for (int i=0;i<40;i++)
            {
                var topic = catalog.GetRandomTopic(initiator, previous);
                Assert.NotNull(topic); Assert.AreEqual(initiator,topic.Initiator); Assert.AreNotSame(previous,topic);
                previous = topic;
            }
        }

        private void BeginConversation(bool devil = false)
        {
            var input = devil ? _devilInput : _angelInput;
            input.TakeControl(); input.transform.position = new Vector3(devil ? 5 : -5,0);
            Physics2D.SyncTransforms(); _door.SetOpen(true); _door.TryInteract();
            Assert.IsTrue(_door.IsChoiceVisible); Assert.IsFalse(_door.IsDialogueVisible);
            _door.AcceptConversation(); Assert.IsFalse(_door.IsChoiceVisible);
        }
        private void RefreshVisit()
        {
            Physics2D.SyncTransforms();
            typeof(ApartmentDoorInteraction).GetMethod("RefreshVisit",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(_door,null);
        }

        private float Deadline(string name) => (float)typeof(ApartmentDoorInteraction).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(_door);
        private void Advance(float now) => typeof(ApartmentDoorInteraction).GetMethod("AdvanceDialogue",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(_door,new object[]{now});
        private PetDialogueBubble Bubble(string name)
        {
            var bubble=New(name).AddComponent<PetDialogueBubble>();
            var text=New(name+" text").AddComponent<TextMeshProUGUI>();
            var so=new SerializedObject(bubble); Bind(so,"_content",text.gameObject); Bind(so,"_text",text); so.ApplyModifiedPropertiesWithoutUndo();
            bubble.Hide(); return bubble;
        }

        private static IndoorDoorDialogueCatalog Catalog() => AssetDatabase.LoadAssetAtPath<IndoorDoorDialogueCatalog>("Assets/_Project/ScriptableObjects/IndoorDoorDialogues.asset");
        private GameObject New(string name) { var go=new GameObject(name); _objects.Add(go); return go; }
        private static void Bind(SerializedObject target,string field,Object value) => target.FindProperty(field).objectReferenceValue=value;
    }
}
