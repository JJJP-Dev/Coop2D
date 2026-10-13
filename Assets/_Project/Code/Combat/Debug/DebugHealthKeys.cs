using FishNet.Managing;
using UnityEngine;
using UnityEngine.InputSystem;

// Test keys for the host until the cheats panel (#146) replaces them.
public class DebugHealthKeys : MonoBehaviour
{
    [SerializeField] private NetworkManager _networkManager;
    [SerializeField, Min(1)] private int _damage = 10;

    private InputService _inputService;

    private void Start()
    {
        // Editor and development builds only.
        if (!Debug.isDebugBuild)
            return;

        _inputService = GameServices.Instance.InputService;
        _inputService.DamageAll.performed += OnDamageAll;
        _inputService.HealAll.performed += OnHealAll;
    }

    private void OnDestroy()
    {
        if (_inputService == null)
            return;

        _inputService.DamageAll.performed -= OnDamageAll;
        _inputService.HealAll.performed -= OnHealAll;
    }

    private void OnDamageAll(InputAction.CallbackContext context)
    {
        if (!_networkManager.IsServerStarted)
            return;

        foreach (Health health in FindObjectsByType<Health>(FindObjectsSortMode.None))
            health.TryTakeDamage(_damage, null);
    }

    private void OnHealAll(InputAction.CallbackContext context)
    {
        if (!_networkManager.IsServerStarted)
            return;

        foreach (Health health in FindObjectsByType<Health>(FindObjectsSortMode.None))
            health.RestoreFull();
    }
}
