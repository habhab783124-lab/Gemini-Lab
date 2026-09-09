#nullable enable
using System;
using GeminiLab.Core;
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Handles left-click reactions on the pet, including a debug emotion log and a simple speech bubble.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PetClickReactionController : MonoBehaviour
    {
        [Serializable]
        public sealed class ClickReactionAnimationOption
        {
            [SerializeField] private string _label = string.Empty;
            [SerializeField] private string _animatorStateName = string.Empty;
            [SerializeField] private string _expressionOverride = string.Empty;

            public string Label => _label;
            public string AnimatorStateName => _animatorStateName;
            public string ExpressionOverride => _expressionOverride;
        }

        [SerializeField] private bool _enableClickReaction = true;
        [SerializeField] private bool _socializeOnClick = true;
        [SerializeField] private Collider2D? _selectionArea;
        [SerializeField, Tooltip("气泡显示在此控制标识上方，避免箭头盖住文字。")]
        private SpriteRenderer? _controlIndicator;
        [SerializeField] private float _bubbleDurationSeconds = 3f;
        [SerializeField] private Vector3 _bubbleLocalOffset = new(0f, 2.6f, 0f);
        [SerializeField] private float _bubbleCharacterSize = 0.14f;
        [SerializeField] private int _bubbleFontSize = 44;
        [SerializeField] private float _bubbleMinWidth = 3.6f;
        [SerializeField] private float _bubbleMaxWidth = 8.2f;
        [SerializeField] private float _bubbleMinHeight = 1.1f;
        [SerializeField] private float _bubbleLineHeight = 0.46f;
        [SerializeField] private float _bubbleHorizontalPadding = 0.6f;
        [SerializeField] private float _bubbleVerticalPadding = 0.4f;
        [SerializeField] private float _bubbleOutlineThickness = 0.12f;
        [SerializeField] private int _bubbleMaxCharactersPerLine = 9;
        [SerializeField] private string[] _responseCorpus = PetClickResponseLibrary.CreateDefaultResponses();
        [SerializeField] private string[] _expressionLabels = PetClickResponseLibrary.CreateDefaultExpressions();
        [SerializeField] private ClickReactionAnimationOption[] _clickReactionAnimations = Array.Empty<ClickReactionAnimationOption>();
        [Header("交流反馈（数值规则文档 §15-16）；留空则该情况走随机语料")]
        [SerializeField] private string _needSpaceFeedback = "（想自己待一会儿…）";
        [SerializeField] private string _initiatorExhaustedFeedback = "（它累得不想动了…）";

        private const int BubbleSortingOffset = 250;
        private const float BubbleMinWidth = 3.6f;
        private const float BubbleMaxWidth = 8.2f;
        private const float BubbleMinHeight = 1.1f;
        private const float BubbleLineHeight = 0.46f;
        private const float BubbleHorizontalPadding = 0.6f;
        private const float BubbleVerticalPadding = 0.4f;
        private const float BubbleOutlineThickness = 0.12f;
        private const int BubbleMaxCharactersPerLine = 9;

        private static Sprite? s_whiteSprite;

        private SpriteRenderer? _petRenderer;
        private PetController? _petController;
        private PetPlayerInputController? _playerInputController;
        private Collider2D? _clickCollider;
        private GameObject? _bubbleRoot;
        private SpriteRenderer? _bubbleOutline;
        private SpriteRenderer? _bubbleBackground;
        private SpriteRenderer? _bubbleTailOutline;
        private SpriteRenderer? _bubbleTail;
        private TextMesh? _bubbleText;
        private float _bubbleHideAtTime;

        private void Awake()
        {
            _petRenderer = GetComponent<SpriteRenderer>();
            _petController = GetComponent<PetController>();
            _playerInputController = GetComponent<PetPlayerInputController>();
            EnsureClickCollider();
            EnsureBubbleVisuals();
            HideBubbleImmediate();
        }

        private void Update()
        {
            if (_bubbleRoot != null && _bubbleRoot.activeSelf && Time.time >= _bubbleHideAtTime)
            {
                HideBubbleImmediate();
            }

            if (ClickOcclusionUtility.IsPointerOverUI())
            {
                return;
            }

            if (!isActiveAndEnabled || !Input.GetMouseButtonDown(0))
            {
                return;
            }

            TryHandleLeftClick();
        }

        private void EnsureClickCollider()
        {
            _clickCollider = _selectionArea != null ? _selectionArea : GetComponent<Collider2D>();
            if (_clickCollider != null)
            {
                return;
            }

            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            if (_petRenderer != null && _petRenderer.sprite != null)
            {
                collider.size = _petRenderer.sprite.bounds.size;
                collider.offset = _petRenderer.sprite.bounds.center;
            }
            else
            {
                collider.size = Vector2.one;
            }

            _clickCollider = collider;
        }

        private void TryHandleLeftClick()
        {
            if (_clickCollider == null)
            {
                EnsureClickCollider();
            }

            if (_clickCollider == null || Camera.main == null)
            {
                return;
            }

            Vector3 screenPoint = Input.mousePosition;
            Vector3 worldPoint3 = Camera.main.ScreenToWorldPoint(screenPoint);
            Vector2 worldPoint = new(worldPoint3.x, worldPoint3.y);
            _ = TryHandleWorldPoint(worldPoint);
        }

        public bool TryHandleWorldPoint(Vector2 worldPoint)
        {
            if (_clickCollider == null)
            {
                EnsureClickCollider();
            }

            if (_clickCollider == null ||
                (!ClickOcclusionUtility.IsTopmostColliderAtWorldPoint(worldPoint, _clickCollider) &&
                 !ClickOcclusionUtility.IsTopmostColliderAtWorldPoint(worldPoint, GetComponent<Collider2D>())) ||
                !_clickCollider.OverlapPoint(worldPoint))
            {
                return false;
            }

            _playerInputController ??= GetComponent<PetPlayerInputController>();
            _playerInputController?.TakeControl();

            if (!_enableClickReaction)
            {
                return true;
            }

            // 交流结算（数值规则文档 §15-18）：被点击的宠物是“对方”，另一只是“发起者”。
            // NEED_SPACE / 发起者精力不足时用固定气泡反馈，其余情况走原有随机语料。
            if (_socializeOnClick && TryResolveSocialFeedback(out string socialFeedback))
            {
                ShowBubble(socialFeedback);
                return true;
            }

            string expression = ResolveCurrentExpression();
            string animatorStateName = ResolveReactionAnimatorStateName(ref expression);
            string response = PetClickResponseLibrary.GetRandomResponse(_responseCorpus);
            Debug.Log($"[PetClickReaction] Current expression: {expression}");
            if (!string.IsNullOrWhiteSpace(animatorStateName))
            {
                Debug.Log($"[PetClickReaction] Pending click animation state: {animatorStateName}");
            }
            ShowBubble(response);
            return true;
        }

        private void EnsureBubbleVisuals()
        {
            if (_bubbleRoot != null)
            {
                return;
            }

            Sprite sprite = GetOrCreateWhiteSprite();

            _bubbleRoot = new GameObject("PetClickSpeechBubble");
            _bubbleRoot.transform.SetParent(transform, false);
            UpdateBubbleTransform();

            GameObject outline = new("Outline");
            outline.transform.SetParent(_bubbleRoot.transform, false);
            _bubbleOutline = outline.AddComponent<SpriteRenderer>();
            _bubbleOutline.sprite = sprite;
            _bubbleOutline.color = new Color(0.12f, 0.12f, 0.12f, 0.96f);

            GameObject background = new("Background");
            background.transform.SetParent(_bubbleRoot.transform, false);
            _bubbleBackground = background.AddComponent<SpriteRenderer>();
            _bubbleBackground.sprite = sprite;
            _bubbleBackground.color = new Color(1f, 1f, 1f, 0.92f);

            GameObject tailOutline = new("TailOutline");
            tailOutline.transform.SetParent(_bubbleRoot.transform, false);
            tailOutline.transform.localPosition = new Vector3(-0.46f, -0.58f, 0f);
            tailOutline.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tailOutline.transform.localScale = new Vector3(0.28f, 0.28f, 1f);
            _bubbleTailOutline = tailOutline.AddComponent<SpriteRenderer>();
            _bubbleTailOutline.sprite = sprite;
            _bubbleTailOutline.color = _bubbleOutline.color;

            GameObject tail = new("Tail");
            tail.transform.SetParent(_bubbleRoot.transform, false);
            tail.transform.localPosition = new Vector3(-0.42f, -0.52f, -0.01f);
            tail.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tail.transform.localScale = new Vector3(0.2f, 0.2f, 1f);
            _bubbleTail = tail.AddComponent<SpriteRenderer>();
            _bubbleTail.sprite = sprite;
            _bubbleTail.color = _bubbleBackground.color;

            GameObject text = new("Text");
            text.transform.SetParent(_bubbleRoot.transform, false);
            text.transform.localPosition = new Vector3(0f, 0.02f, -0.02f);
            _bubbleText = text.AddComponent<TextMesh>();
            _bubbleText.anchor = TextAnchor.MiddleCenter;
            _bubbleText.alignment = TextAlignment.Center;
            _bubbleText.characterSize = _bubbleCharacterSize;
            _bubbleText.fontSize = _bubbleFontSize;
            _bubbleText.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            _bubbleText.text = string.Empty;

            ApplyBubbleSorting();
        }

        private bool _dialogueBubbleActive;

        public void SetDialogueBubbleActive(bool active)
        {
            _dialogueBubbleActive = active;
            if (active) HideBubbleImmediate();
        }

        private void ShowBubble(string message)
        {
            if (_dialogueBubbleActive) return;
            EnsureBubbleVisuals();
            if (_bubbleRoot == null || _bubbleBackground == null || _bubbleTail == null || _bubbleText == null)
            {
                return;
            }

            string formattedMessage = FormatBubbleText(message, _bubbleMaxCharactersPerLine, out int lineCount, out int maxLineLength);
            float width = Mathf.Clamp(
                _bubbleHorizontalPadding * 2f + maxLineLength * 0.42f,
                _bubbleMinWidth,
                _bubbleMaxWidth);
            float height = Mathf.Max(
                _bubbleMinHeight,
                _bubbleVerticalPadding * 2f + lineCount * _bubbleLineHeight);

            if (_bubbleOutline != null)
            {
                _bubbleOutline.drawMode = SpriteDrawMode.Sliced;
                _bubbleOutline.size = new Vector2(width + _bubbleOutlineThickness * 2f, height + _bubbleOutlineThickness * 2f);
            }

            _bubbleBackground.drawMode = SpriteDrawMode.Sliced;
            _bubbleBackground.size = new Vector2(width, height);
            _bubbleTail.drawMode = SpriteDrawMode.Simple;
            if (_bubbleText != null)
            {
                _bubbleText.text = formattedMessage;
            }

            UpdateBubbleTransform();
            _bubbleRoot.SetActive(true);
            _bubbleHideAtTime = Time.time + Mathf.Max(0.5f, _bubbleDurationSeconds);
            ApplyBubbleSorting();
        }

        private void HideBubbleImmediate()
        {
            if (_bubbleRoot != null)
            {
                _bubbleRoot.SetActive(false);
            }
        }

        private void ApplyBubbleSorting()
        {
            if (_petRenderer == null)
            {
                _petRenderer = GetComponent<SpriteRenderer>();
            }

            if (_petRenderer == null)
            {
                return;
            }

            int sortingOrder = _petRenderer.sortingOrder + BubbleSortingOffset;
            string sortingLayerName = _petRenderer.sortingLayerName;
            if (_controlIndicator != null)
            {
                int petLayer = SortingLayer.GetLayerValueFromID(_petRenderer.sortingLayerID);
                int indicatorLayer = SortingLayer.GetLayerValueFromID(_controlIndicator.sortingLayerID);
                if (indicatorLayer > petLayer)
                {
                    sortingLayerName = _controlIndicator.sortingLayerName;
                    sortingOrder = _controlIndicator.sortingOrder + 1;
                }
                else if (indicatorLayer == petLayer)
                {
                    sortingOrder = Mathf.Max(sortingOrder, _controlIndicator.sortingOrder + 1);
                }
            }

            if (_bubbleBackground != null)
            {
                _bubbleBackground.sortingLayerName = sortingLayerName;
                _bubbleBackground.sortingOrder = sortingOrder;
            }

            if (_bubbleTail != null)
            {
                _bubbleTail.sortingLayerName = sortingLayerName;
                _bubbleTail.sortingOrder = sortingOrder + 2;
            }

            if (_bubbleOutline != null)
            {
                _bubbleOutline.sortingLayerName = sortingLayerName;
                _bubbleOutline.sortingOrder = sortingOrder;
            }

            if (_bubbleTailOutline != null)
            {
                _bubbleTailOutline.sortingLayerName = sortingLayerName;
                _bubbleTailOutline.sortingOrder = sortingOrder + 1;
            }

            if (_bubbleText != null)
            {
                MeshRenderer? meshRenderer = _bubbleText.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.sortingLayerName = sortingLayerName;
                    meshRenderer.sortingOrder = sortingOrder + 3;
                }
            }
        }

        private void UpdateBubbleTransform()
        {
            if (_bubbleRoot == null)
            {
                return;
            }

            Vector3 lossyScale = transform.lossyScale;
            float inverseX = Mathf.Approximately(lossyScale.x, 0f) ? 1f : 1f / lossyScale.x;
            float inverseY = Mathf.Approximately(lossyScale.y, 0f) ? 1f : 1f / lossyScale.y;

            _bubbleRoot.transform.localPosition = new Vector3(
                _bubbleLocalOffset.x * inverseX,
                _bubbleLocalOffset.y * inverseY,
                _bubbleLocalOffset.z);
            _bubbleRoot.transform.localScale = new Vector3(inverseX, inverseY, 1f);
        }

        /// <summary>
        /// 点击交流结算（数值规则文档 §15-18）。
        /// 返回 true 表示应使用 <paramref name="feedback"/> 作为气泡文本（NEED_SPACE / 发起者精力不足）；
        /// 返回 false 表示走原有随机语料（NORMAL / WARM 已静默结算，或场景里只有一只宠）。
        /// </summary>
        private bool TryResolveSocialFeedback(out string feedback)
        {
            feedback = string.Empty;
            if (_petController == null)
            {
                return false;
            }

            if (!ServiceLocator.TryResolve(out Social.IPetSocialService? social) || social == null ||
                !ServiceLocator.TryResolve(out IPetRoster? roster) || roster == null)
            {
                return false;
            }

            PetId target = _petController.PetId;
            PetId initiator = target == PetId.Angel ? PetId.Devil : PetId.Angel;
            if (roster.TryGet(initiator) == null)
            {
                // 另一只宠不在场（如世界地图单宠场景），不发生交流。
                return false;
            }

            // §16：发起者精力不足，交流不发生，给玩家反馈。
            if (!social.CanInitiate(initiator))
            {
                if (!string.IsNullOrWhiteSpace(_initiatorExhaustedFeedback))
                {
                    feedback = _initiatorExhaustedFeedback;
                    return true;
                }
                return false;
            }

            var outcome = social.TrySocialize(initiator, target);
            if (outcome.Initiated &&
                outcome.ResponseType == Social.SocialResponseType.NeedSpace &&
                !string.IsNullOrWhiteSpace(_needSpaceFeedback))
            {
                feedback = _needSpaceFeedback;
                return true;
            }

            return false;
        }

        private string ResolveCurrentExpression()
        {
            if (_petController == null)
            {
                _petController = GetComponent<PetController>();
            }

            float mood = _petController?.RuntimeData?.Mood ?? 60f;
            if (mood >= 80f)
            {
                return "happy";
            }

            if (mood >= 60f)
            {
                return "expectant";
            }

            if (mood >= 40f)
            {
                return "relaxed";
            }

            if (mood >= 20f)
            {
                return "curious";
            }

            return "shy";
        }

        private static string FormatBubbleText(string source, int maxCharactersPerLine, out int lineCount, out int maxLineLength)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                lineCount = 1;
                maxLineLength = 1;
                return string.Empty;
            }

            System.Text.StringBuilder builder = new();
            int currentLineLength = 0;
            maxLineLength = 0;
            lineCount = 1;

            foreach (char c in source)
            {
                if (currentLineLength >= maxCharactersPerLine)
                {
                    builder.Append('\n');
                    maxLineLength = Mathf.Max(maxLineLength, currentLineLength);
                    currentLineLength = 0;
                    lineCount++;
                }

                builder.Append(c);
                currentLineLength++;
            }

            maxLineLength = Mathf.Max(maxLineLength, currentLineLength);
            return builder.ToString();
        }

        private string ResolveReactionAnimatorStateName(ref string expression)
        {
            for (int i = 0; i < _clickReactionAnimations.Length; i++)
            {
                ClickReactionAnimationOption option = _clickReactionAnimations[i];
                if (string.IsNullOrWhiteSpace(option.AnimatorStateName))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(option.ExpressionOverride))
                {
                    expression = option.ExpressionOverride;
                }

                return option.AnimatorStateName;
            }

            return string.Empty;
        }

        private static Sprite GetOrCreateWhiteSprite()
        {
            if (s_whiteSprite != null)
            {
                return s_whiteSprite;
            }

            Texture2D texture = new(1, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            s_whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            s_whiteSprite.hideFlags = HideFlags.HideAndDontSave;
            return s_whiteSprite;
        }
    }
}
