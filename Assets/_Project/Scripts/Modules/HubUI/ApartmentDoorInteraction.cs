#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Furniture;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.Pet.Social;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI
{
    /// <summary>小门开关和串门交流。房间范围、选项和气泡均由场景显式绑定。</summary>
    public sealed class ApartmentDoorInteraction : MonoBehaviour
    {
        [SerializeField] private Collider2D? _clickArea;
        [SerializeField] private SpriteRenderer? _closedVisual;
        [SerializeField] private GameObject? _openVisual;
        [SerializeField] private BoxCollider2D? _passageBlocker;
        [SerializeField] private PetController? _angel;
        [SerializeField] private PetController? _devil;
        [SerializeField] private Transform? _angelApproach;
        [SerializeField] private Transform? _devilApproach;
        [SerializeField, Min(0.1f)] private float _talkDistance = 2.2f;
        [SerializeField] private IndoorDoorDialogueCatalog? _dialogues;
        [SerializeField] private PetDialogueBubble? _angelBubble;
        [SerializeField] private PetDialogueBubble? _devilBubble;
        [SerializeField, Min(0.1f)] private float _replyDelay = 2.5f;
        [SerializeField, Min(1f)] private float _readingDuration = 8f;
        [SerializeField] private TMP_Text? _hint;
        [SerializeField] private bool _isOpen;
        [SerializeField] private Collider2D? _angelRoom;
        [SerializeField] private Collider2D? _devilRoom;
        [SerializeField] private GameObject? _angelChoices;
        [SerializeField] private GameObject? _devilChoices;
        [SerializeField] private Button? _angelChat;
        [SerializeField] private Button? _angelDecline;
        [SerializeField] private Button? _devilChat;
        [SerializeField] private Button? _devilDecline;
        private PetController? _visitInitiator;
        private PetController? _speakerPet;
        private bool _visitDismissed;
        private bool _choiceVisible;
        private bool _closeBlocked;
        private PetDialogueBubble? _pendingReceiver;
        private IndoorDoorDialogueCatalog.Topic? _topic;
        private IndoorDoorDialogueCatalog.Topic? _lastAngelTopic;
        private IndoorDoorDialogueCatalog.Topic? _lastDevilTopic;
        private float _replyAt;
        private float _hideAt;
        private bool _dialogueActive;

        public bool IsOpen => _isOpen;
        public bool IsDialogueVisible => _dialogueActive;
        public bool IsChoiceVisible => _choiceVisible;

        private bool IsSpacePageActive => !ServiceLocator.TryResolve(out IUIRouter? router) || router == null || router.Top == PanelId.SpaceSys;

        private void OnValidate() => ApplyDoorState();

        private void OnEnable()
        {
            PetPlayerFurnitureInteractionController.PriorityInteractRequested += TryInteract;
            ApplyDoorState();
            if (_angelChat != null) _angelChat.onClick.AddListener(AcceptConversation);
            if (_devilChat != null) _devilChat.onClick.AddListener(AcceptConversation);
            if (_angelDecline != null) _angelDecline.onClick.AddListener(DeclineConversation);
            if (_devilDecline != null) _devilDecline.onClick.AddListener(DeclineConversation);
            HideChoices();
        }

        private void OnDisable()
        {
            PetPlayerFurnitureInteractionController.PriorityInteractRequested -= TryInteract;
            if (_angelChat != null) _angelChat.onClick.RemoveListener(AcceptConversation);
            if (_devilChat != null) _devilChat.onClick.RemoveListener(AcceptConversation);
            if (_angelDecline != null) _angelDecline.onClick.RemoveListener(DeclineConversation);
            if (_devilDecline != null) _devilDecline.onClick.RemoveListener(DeclineConversation);
            ResetVisit();
        }

        private void Update()
        {
            RefreshVisit();
            if (Input.GetKeyDown(KeyCode.Escape)) DeclineConversation();
            AdvanceDialogue(Time.unscaledTime);
            if (_hint == null) return;
            _hint.text = _dialogueActive ? "Esc 结束交流" : _choiceVisible ? "" : _closeBlocked ? "门口有宠物，走开后再关门" : !_isOpen ? "点击小门开门" : _visitInitiator != null ? "F 再次显示交流选项" : "WASD 进入对方房间，可选择交流";
        }

        private bool CanOfferConversation()
        {
            if (!isActiveAndEnabled || !IsSpacePageActive || !_isOpen) return false;
            foreach (var build in FindObjectsByType<BuildModeController>(FindObjectsSortMode.None))
                if (build.IsBuildModeEnabled) return false;
            return true;
        }

        private void RefreshVisit()
        {
            if (!CanOfferConversation() || !TryGetVisitingInitiator(out var visitor))
            { ResetVisit(); return; }
            if (_visitInitiator != visitor)
            {
                ResetVisit();
                _visitInitiator = visitor;
            }
            if (!_dialogueActive && !_visitDismissed) ShowChoices();
        }

        private void ResetVisit()
        {
            CloseDialogue();
            _visitInitiator = null;
            _visitDismissed = false;
        }

        private void ShowChoices()
        {
            _choiceVisible = true;
            if (_angelChoices != null) _angelChoices.SetActive(_visitInitiator == _devil);
            if (_devilChoices != null) _devilChoices.SetActive(_visitInitiator == _angel);
        }

        private void HideChoices()
        {
            _choiceVisible = false;
            if (_angelChoices != null) _angelChoices.SetActive(false);
            if (_devilChoices != null) _devilChoices.SetActive(false);
        }

        public void DeclineConversation()
        {
            _visitDismissed = true;
            CloseDialogue();
        }

        public bool TryHandleWorldPoint(Vector2 point)
        {
            if (!isActiveAndEnabled || !IsSpacePageActive || _clickArea == null || !_clickArea.OverlapPoint(point)) return false;
            SetOpen(!_isOpen);
            return true;
        }

        public void SetOpen(bool value)
        {
            _closeBlocked = !value && _isOpen && IsPassageOccupied();
            if (_closeBlocked) return;
            _isOpen = value;
            ApplyDoorState();
            if (!value) ResetVisit();
        }

        private void ApplyDoorState()
        {
            if (_closedVisual != null) _closedVisual.enabled = !_isOpen;
            if (_openVisual != null) _openVisual.SetActive(_isOpen);
            if (_passageBlocker != null) _passageBlocker.enabled = !_isOpen;
            if (Application.isPlaying)
            {
                if (_angel != null) _angel.GetComponent<ApartmentPetMovement>()?.RefreshObstacles();
                if (_devil != null) _devil.GetComponent<ApartmentPetMovement>()?.RefreshObstacles();
            }
        }

        private bool IsPassageOccupied()
        {
            if (_passageBlocker == null) return false;
            // disabled Collider.bounds 为空，使用作者化尺寸检查，避免关门把宠物夹进障碍。
            Vector3 size = _passageBlocker.transform.TransformVector(_passageBlocker.size);
            var bounds = new Bounds(_passageBlocker.transform.TransformPoint(_passageBlocker.offset),
                new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), 2f));
            foreach (PetController? pet in new[] { _angel, _devil })
            {
                var foot = pet != null ? pet.GetComponent<CapsuleCollider2D>() : null;
                if (foot != null && foot.enabled && bounds.Intersects(foot.bounds)) return true;
            }
            return false;
        }

        public void CloseDialogue()
        {
            _dialogueActive = false;
            _pendingReceiver = null;
            _topic = null;
            _speakerPet = null;
            HideChoices();
            if (_angel != null) _angel.SetConversationPaused(false);
            if (_devil != null) _devil.SetConversationPaused(false);
            if (_angelBubble != null) _angelBubble.Hide();
            if (_devilBubble != null) _devilBubble.Hide();
            SetClickBubblesSuppressed(false);
        }

        private void SetClickBubblesSuppressed(bool value)
        {
            if (_angel != null) _angel.GetComponent<PetClickReactionController>()?.SetDialogueBubbleActive(value);
            if (_devil != null) _devil.GetComponent<PetClickReactionController>()?.SetDialogueBubbleActive(value);
        }

        private void AdvanceDialogue(float now)
        {
            if (!_dialogueActive) return;
            if (_pendingReceiver != null && _speakerPet != null && _topic != null && now >= _replyAt)
            {
                // 开场播放后才读取接收者状态并结算；先取走待办，避免事件回调重复结算。
                var receiver = _pendingReceiver;
                _pendingReceiver = null;
                if (ServiceLocator.TryResolve(out IPetSocialService? social) && social != null)
                {
                    PetId id = _speakerPet.PetId;
                    var topic = _topic;
                    var outcome = social.TrySocialize(id, id == PetId.Angel ? PetId.Devil : PetId.Angel);
                    if (_dialogueActive)
                        receiver.Show(outcome.Initiated ? topic.Reply(outcome.ResponseType) : "等休息好了再聊吧。");
                }
            }
            if (now >= _hideAt) CloseDialogue();
        }

        private bool TryGetVisitingInitiator(out PetController? initiator)
        {
            initiator = null;
            Transform? active = PetPlayerInputController.ActiveTransform;
            if (active == null) return false;
            if (_angel != null && active == _angel.transform) initiator = _angel;
            else if (_devil != null && active == _devil.transform) initiator = _devil;
            if (initiator == null) return false;
            var room = initiator == _angel ? _devilRoom : _angelRoom;
            var foot = initiator.GetComponent<CapsuleCollider2D>();
            Vector2 position = foot != null && foot.enabled ? foot.bounds.center : active.position;
            return room != null && room.OverlapPoint(position);
        }

        // F 只重开选项，不能绕过玩家的交流确认。
        public bool TryInteract()
        {
            if (!isActiveAndEnabled || !IsSpacePageActive) return false;
            if (_dialogueActive) return true;
            if (!CanOfferConversation() || !TryGetVisitingInitiator(out var visitor)) return false;
            _visitInitiator = visitor;
            _visitDismissed = false;
            ShowChoices();
            return true;
        }

        public void AcceptConversation()
        {
            if (!_choiceVisible || !CanOfferConversation() || !TryGetVisitingInitiator(out var initiator) || initiator != _visitInitiator) return;
            if (_dialogues == null || _angelBubble == null || _devilBubble == null ||
                !ServiceLocator.TryResolve(out IPetSocialService? social) || social == null) return;
            PetId id = initiator!.PetId;
            var topic = _dialogues.GetRandomTopic(id, id == PetId.Angel ? _lastAngelTopic : _lastDevilTopic);
            if (topic == null) return;
            CloseDialogue();
            _visitDismissed = true;
            _speakerPet = initiator;
            _topic = topic;
            SetClickBubblesSuppressed(true);
            bool canInitiate = social.CanInitiate(id);
            var speaker = id == PetId.Angel ? _angelBubble : _devilBubble;
            speaker.Show(canInitiate ? topic.Opening : "现在太累了，休息一会儿再聊吧。");
            _dialogueActive = true;
            _pendingReceiver = canInitiate ? (id == PetId.Angel ? _devilBubble : _angelBubble) : null;
            _replyAt = Time.unscaledTime + Mathf.Max(.1f, _replyDelay);
            _hideAt = (canInitiate ? _replyAt : Time.unscaledTime) + Mathf.Max(1f, _readingDuration);
            if (canInitiate)
            {
                _angel.SetConversationPaused(true);
                _devil.SetConversationPaused(true);
                if (id == PetId.Angel) _lastAngelTopic = topic; else _lastDevilTopic = topic;
            }
        }
    }
}
