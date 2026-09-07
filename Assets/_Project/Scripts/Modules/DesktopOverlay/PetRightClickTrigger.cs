using UnityEngine;

namespace GeminiLab.Modules.DesktopOverlay
{
    public class PetRightClickTrigger : MonoBehaviour
    {
        private void OnMouseOver()
        {
            if (Input.GetMouseButtonDown(1))
            {
                if (DesktopPetContextMenu.Instance != null)
                {
                    DesktopPetContextMenu.Instance.Show(
                        Input.mousePosition
                    );
                }
            }
        }
    }
}
