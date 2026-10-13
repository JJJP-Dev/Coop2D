using System.Collections;
using FishNet.Object;
using UnityEngine;

// Target for testing attacks: restores its health a moment after dying.
[RequireComponent(typeof(Health))]
public class TrainingDummy : NetworkBehaviour
{
    [SerializeField, Min(0f)] private float _restoreDelay = 1f;

    private Health _health;

    private void Awake()
    {
        _health = GetComponent<Health>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        _health.Died += OnDied;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        _health.Died -= OnDied;
    }

    private void OnDied(Health health, NetworkObject attacker)
    {
        StartCoroutine(RestoreAfterDelay());
    }

    private IEnumerator RestoreAfterDelay()
    {
        yield return new WaitForSeconds(_restoreDelay);

        // The session may have ended while waiting.
        if (IsServerInitialized)
            _health.RestoreFull();
    }
}
