using FishNet.Object;
using UnityEngine;

public class PlayerInputReader : NetworkBehaviour
{
    private InputService _inputService;

    public Vector2 MoveInput { get; private set; }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Only the copy this machine owns reads the input.
        if (IsOwner && GameServices.Instance != null)
            _inputService = GameServices.Instance.InputService;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        _inputService = null;
        MoveInput = Vector2.zero;
    }

    private void Update()
    {
        if (_inputService == null)
            return;

        MoveInput = _inputService.Move.ReadValue<Vector2>();
    }
}
