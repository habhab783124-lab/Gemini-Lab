using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace GeminiLab.Modules.DesktopOverlay
{
    [RequireComponent(typeof(Button))]
    public class DesktopOverlayButtonListener : MonoBehaviour
    {
        public enum ActionType
    {
        EnterOverlay,
        ExitOverlay
    }

    [SerializeField] private ActionType actionType;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        button.onClick.AddListener(HandleClick);
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick()
    {
        switch (actionType)
        {
            case ActionType.EnterOverlay:
                DesktopOverlaySceneController.EnterOverlay();
                break;

            case ActionType.ExitOverlay:
                DesktopOverlaySceneController.ExitOverlay();
                break;
        }
    }
    }
}
