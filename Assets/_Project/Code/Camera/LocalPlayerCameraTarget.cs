using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineCamera))]
public class LocalPlayerCameraTarget : MonoBehaviour
{
    private CinemachineCamera _camera;
    private LocalPlayerService _localPlayerService;

    private void Awake()
    {
        _camera = GetComponent<CinemachineCamera>();
    }

    private void OnEnable()
    {
        if (GameServices.Instance == null)
        {
            Debug.LogWarning("[LocalPlayerCameraTarget] GameServices not found. Start Play mode from Bootstrap.", this);
            return;
        }

        _localPlayerService = GameServices.Instance.LocalPlayerService;
        _localPlayerService.Changed += Follow;

        // The local player may already exist before this camera is enabled.
        Follow(_localPlayerService.Current);
    }

    private void OnDisable()
    {
        if (_localPlayerService != null)
            _localPlayerService.Changed -= Follow;

        _localPlayerService = null;
    }

    private void Follow(PlayerIdentity player)
    {
        _camera.Follow = player != null ? player.transform : null;

        // Cut straight to the new target instead of gliding from the old position.
        _camera.PreviousStateIsValid = false;
    }
}
