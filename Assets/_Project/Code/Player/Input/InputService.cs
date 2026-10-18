using UnityEngine;
using UnityEngine.InputSystem;

public class InputService : MonoBehaviour
{
    private GameInputActions _actions;

    public InputAction Move => _actions.Player.Move;
    public InputAction Aim => _actions.Player.Aim;
    public InputAction Skill1 => _actions.Player.Skill1;
    public InputAction Skill2 => _actions.Player.Skill2;
    public InputAction Skill3 => _actions.Player.Skill3;
    public InputAction Cancel => _actions.UI.Cancel;
    public InputAction ToggleNetworkStats => _actions.Debug.ToggleNetworkStats;
    public InputAction DamageAll => _actions.Debug.DamageAll;
    public InputAction HealAll => _actions.Debug.HealAll;
    public InputAction CycleElement => _actions.Debug.CycleElement;
    public InputAction ToggleCheats => _actions.Debug.ToggleCheats;

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
