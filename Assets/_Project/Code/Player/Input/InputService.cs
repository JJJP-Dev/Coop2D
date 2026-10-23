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
    public InputAction CycleElement => _actions.Debug.CycleElement;
    public InputAction ToggleCheats => _actions.Debug.ToggleCheats;

    // The upgrade offer's panel (#77). The reroll is F and not R, which #77 keeps for a skill.
    public InputAction OfferChoose1 => _actions.Offer.Choose1;
    public InputAction OfferChoose2 => _actions.Offer.Choose2;
    public InputAction OfferChoose3 => _actions.Offer.Choose3;
    public InputAction OfferReroll => _actions.Offer.Reroll;
    public InputAction OfferBanish => _actions.Offer.Banish;
    public InputAction OfferSkip => _actions.Offer.Skip;
    public InputAction OfferTogglePanel => _actions.Offer.TogglePanel;

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
