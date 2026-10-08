using FishNet.Object;
using UnityEngine;

public class PlayerInputReader : NetworkBehaviour
{
    private GameInputActions _actions;

    public Vector2 MoveInput { get; private set; }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner)
            return;

        _actions = new GameInputActions();
        _actions.Player.Enable();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (_actions == null)
            return;

        _actions.Player.Disable();
        _actions.Dispose();
        _actions = null;
    }

    private void Update()
    {
        if (_actions == null)
            return;

        MoveInput = _actions.Player.Move.ReadValue<Vector2>();
    }
}
