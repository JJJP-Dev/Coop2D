using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public GameObject mainMenuParentButtons;
    public GameObject mainMenuMultiplayerParent;
    public Button multiplayerBtn;

    public void TransitionToMultiplayerUI()
    {
        mainMenuParentButtons.SetActive(false);
        mainMenuMultiplayerParent.SetActive(true);
    }
}
