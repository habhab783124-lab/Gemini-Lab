#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Furniture;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.Pet.Social;
using TMPro;
using UnityEngine;

namespace GeminiLab.Modules.HubUI
{
    /// <summary>小门开关和交流入口。视觉、对话和接近点均在场景/资产中绑定。</summary>
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
        private int _angelTopic;
        private int _devilTopic;
        private float _nextTalkTime;
        private bool _closeBlocked;
        private PetDialogueBubble? _pendingReceiver;
        private string _pendingReply = string.Empty;
        private float _replyAt;
        private float _hideAt;
        private bool _dialogueActive;

        public bool IsOpen => _isOpen;
        public bool IsDialogueVisible => _dialogueActive;

        private bool IsSpacePageActive => !ServiceLocator.TryResolve(out IUIRouter? router) || router == null || router.Top == PanelId.SpaceSys;

        private void OnValidate() => ApplyDoorState();

        private void OnEnable()
        {
            PetPlayerFurnitureInteractionController.PriorityInteractRequested += TryInteract;
            ApplyDoorState();
        }

        private void OnDisable()
        {
            PetPlayerFurnitureInteractionController.PriorityInteractRequested -= TryInteract;
            CloseDialogue();
        }

        private void Update()
        {
            if (!IsSpacePageActive) { CloseDialogue(); return; }
            if (Input.GetKeyDown(KeyCode.Escape)) CloseDialogue();
            AdvanceDialogue(Time.unscaledTime);
            if (_hint == null) return;
            bool near = TryGetNearbyInitiator(out _);
            _hint.text = _dialogueActive ? "F / Esc 收起气泡" : _closeBlocked ? "门口有宠物，走开后再关门" : !_isOpen ? "点击小门开门" : near ? "WASD 穿过小门  ·  F 交流  ·  点击关门" : "小门已打开  ·  WASD 可串门，门边按 F 交流";
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
            if (!value) CloseDialogue();
        }

        private void ApplyDoorState()
        {
            if (_closedVisual != null) _closedVisual.enabled = !_isOpen;
            if (_openVisual != null) _openVisual.SetActive(_isOpen);
            if (_passageBlocker != null) _passageBlocker.enabled = !_isOpen;
            if (Application.isPlaying)
            {
                _angel?.GetComponent<ApartmentPetMovement>()?.RefreshObstacles();
                _devil?.GetComponent<ApartmentPetMovement>()?.RefreshObstacles();
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
            _pendingReply = string.Empty;
            _angelBubble?.Hide();
            _devilBubble?.Hide();
            SetClickBubblesSuppressed(false);
        }

        private void SetClickBubblesSuppressed(bool value)
        {
            _angel?.GetComponent<PetClickReactionController>()?.SetDialogueBubbleActive(value);
            _devil?.GetComponent<PetClickReactionController>()?.SetDialogueBubbleActive(value);
        }

        private void AdvanceDialogue(float now)
        {
            if (!_dialogueActive) return;
            if (now >= _hideAt) { CloseDialogue(); return; }
            if (_pendingReceiver != null && now >= _replyAt)
            {
                _pendingReceiver.Show(_pendingReply);
                _pendingReceiver = null;
                _pendingReply = string.Empty;
            }
        }

        private bool TryGetNearbyInitiator(out PetController? initiator)
        {
            initiator = null;
            Transform? active = PetPlayerInputController.ActiveTransform;
            if (active == null) return false;
            if (_angel != null && active == _angel.transform) initiator = _angel;
            else if (_devil != null && active == _devil.transform) initiator = _devil;
            if (initiator == null) return false;
            // 串门后也可从另一侧发起，不把交互位置绑定到宠物的出生房间。
            return (_angelApproach != null && Vector2.Distance(active.position, _angelApproach.position) <= _talkDistance) ||
                   (_devilApproach != null && Vector2.Distance(active.position, _devilApproach.position) <= _talkDistance);
        }

        public bool TryInteract()
        {
            if (!isActiveAndEnabled || !IsSpacePageActive) return false;
            foreach (var build in FindObjectsByType<BuildModeController>(FindObjectsSortMode.None))
                if (build.IsBuildModeEnabled) return false;
            if (IsDialogueVisible) { CloseDialogue(); return true; }
            if (!TryGetNearbyInitiator(out PetController? initiator)) return false;
            // 关闭的门也消费 F，避免落入旧的“门边家具”动画入口。
            if (!_isOpen || Time.unscaledTime < _nextTalkTime) return true;
            if (_dialogues == null || _angelBubble == null || _devilBubble == null ||
                !ServiceLocator.TryResolve(out IPetSocialService? social) || social == null) return true;
            PetId id = initiator!.PetId;
            PetId target = id == PetId.Angel ? PetId.Devil : PetId.Angel;
            var topic = _dialogues.GetTopic(id, id == PetId.Angel ? _angelTopic : _devilTopic);
            if (topic == null) return true;
            PetSocialOutcome outcome = social.TrySocialize(id, target);
            var speaker = id == PetId.Angel ? _angelBubble : _devilBubble;
            var receiver = id == PetId.Angel ? _devilBubble : _angelBubble;
            CloseDialogue();
            SetClickBubblesSuppressed(true);
            speaker.Show(outcome.Initiated ? topic.Opening : "现在太累了，休息一会儿再聊吧。");
            _dialogueActive = true;
            _pendingReceiver = outcome.Initiated ? receiver : null;
            _pendingReply = outcome.Initiated ? topic.Reply(outcome.ResponseType) : string.Empty;
            _replyAt = Time.unscaledTime + Mathf.Max(.1f, _replyDelay);
            _hideAt = (outcome.Initiated ? _replyAt : Time.unscaledTime) + Mathf.Max(1f, _readingDuration);
            if (outcome.Initiated) { if (id == PetId.Angel) _angelTopic++; else _devilTopic++; }
            _nextTalkTime = Time.unscaledTime + 2f;
            return true;
        }
    }
}
