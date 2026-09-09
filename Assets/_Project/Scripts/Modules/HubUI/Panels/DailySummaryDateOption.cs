#nullable enable
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// AI 每日小结日期列表中的一个 Scene 作者化选项。
    /// 选中态和未选中态都由预先保存的子节点提供，运行时只切换显隐并填充日期。
    /// </summary>
    public sealed class DailySummaryDateOption : MonoBehaviour
    {
        [SerializeField] private Button? _button;
        [SerializeField] private TMP_Text? _dateText;
        [SerializeField] private GameObject? _selectedVisual;
        [SerializeField] private GameObject? _unselectedVisual;

        public Button? Button => _button;
        public string DateIso { get; private set; } = string.Empty;

        public void SetDate(string dateIso, bool selected, bool visible)
        {
            DateIso = dateIso ?? string.Empty;
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }

            if (_dateText != null)
            {
                _dateText.text = DateIso;
            }

            SetSelected(selected);
        }

        public void SetSelected(bool selected)
        {
            if (_selectedVisual != null)
            {
                if (_selectedVisual.activeSelf != selected)
                {
                    _selectedVisual.SetActive(selected);
                }
            }

            if (_unselectedVisual != null)
            {
                if (_unselectedVisual.activeSelf != !selected)
                {
                    _unselectedVisual.SetActive(!selected);
                }
            }
        }

        public void Clear()
        {
            DateIso = string.Empty;
            if (_dateText != null)
            {
                _dateText.text = string.Empty;
            }

            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
