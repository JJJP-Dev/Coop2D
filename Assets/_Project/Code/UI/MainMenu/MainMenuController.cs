using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class MainMenuController : MonoBehaviour
{
    [FormerlySerializedAs("mainMenuParentButtons")]
    [SerializeField] private GameObject _mainButtons;

    [FormerlySerializedAs("mainMenuMultiplayerParent")]
    [SerializeField] private GameObject _multiplayerPanel;

    private InputService _inputService;

    private void OnEnable()
    {
        if (GameServices.Instance == null)
        {
            Debug.LogWarning("[MainMenuController] GameServices not found. Start Play mode from Bootstrap.", this);
            return;
        }

        _inputService = GameServices.Instance.InputService;
        _inputService.Cancel.performed += OnCancel;
    }

    private void OnDisable()
    {
        if (_inputService != null)
            _inputService.Cancel.performed -= OnCancel;

        _inputService = null;
    }

    public void TransitionToMultiplayerUI()
    {
        _mainButtons.SetActive(false);
        _multiplayerPanel.SetActive(true);
    }

    public void TransitionToMainUI()
    {
        _multiplayerPanel.SetActive(false);
        _mainButtons.SetActive(true);
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        if (_multiplayerPanel.activeSelf)
            TransitionToMainUI();
    }
}
