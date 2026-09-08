#nullable enable
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// AI 每日小结的二次详情弹窗。
    /// 五个放大资源由 Scene 预先作者化，运行时只切换对应节点和填充文本。
    /// </summary>
    public sealed class DailySummaryDetailPopup : MonoBehaviour
    {
        public enum DetailKind
        {
            AngelNote,
            Summary,
            DevilNote,
            AngelCard,
            DevilCard,
            Popup
        }

        [SerializeField] private GameObject? _popupRoot;
        [SerializeField] private GameObject? _angelNoteView;
        [SerializeField] private GameObject? _summaryView;
        [SerializeField] private GameObject? _devilNoteView;
        [SerializeField] private GameObject? _angelCardView;
        [SerializeField] private GameObject? _devilCardView;
        [SerializeField] private GameObject? _popupView;
        [SerializeField] private Button? _closeButton;
        [SerializeField] private Button? _backdropButton;
        [SerializeField] private TMP_Text? _titleText;
        [SerializeField] private TMP_Text? _bodyText;

        private void Awake()
        {
            _closeButton?.onClick.AddListener(Hide);
            _backdropButton?.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            _closeButton?.onClick.RemoveListener(Hide);
            _backdropButton?.onClick.RemoveListener(Hide);
        }

        public void Show(DetailKind kind, string title, string body)
        {
            GameObject root = _popupRoot != null ? _popupRoot : gameObject;
            root.SetActive(true);

            SetActive(_angelNoteView, kind == DetailKind.AngelNote);
            SetActive(_summaryView, kind == DetailKind.Summary);
            SetActive(_devilNoteView, kind == DetailKind.DevilNote);
            SetActive(_angelCardView, kind == DetailKind.AngelCard);
            SetActive(_devilCardView, kind == DetailKind.DevilCard);
            SetActive(_popupView, kind == DetailKind.Popup);
            SetActive(_titleText?.gameObject, kind != DetailKind.Popup);
            SetActive(_bodyText?.gameObject, kind != DetailKind.Popup);

            if (_titleText != null)
            {
                _titleText.text = string.IsNullOrWhiteSpace(title) ? "详情" : title;
            }

            if (_bodyText != null)
            {
                _bodyText.text = string.IsNullOrWhiteSpace(body) ? string.Empty : body;
            }
        }

        public void Hide()
        {
            GameObject root = _popupRoot != null ? _popupRoot : gameObject;
            root.SetActive(false);
        }

        private static void SetActive(GameObject? target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
