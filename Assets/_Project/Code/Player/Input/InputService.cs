using UnityEngine;
using UnityEngine.InputSystem;

public class InputService : MonoBehaviour
{
    private GameInputActions _actions;

    public InputAction Move => _actions.Player.Move;
    public InputAction Cancel => _actions.UI.Cancel;
    public InputAction ToggleNetworkStats => _actions.Debug.ToggleNetworkStats;

    private void Awake()
    {
        _actions = new GameInputActions();
        _actions.Enable();
    }

    private void OnDestroy()
    {
        // Disable before Dispose, or the generated class reports a leak.
        _actions.Disable();
        _actions.Dispose();
    }
}
