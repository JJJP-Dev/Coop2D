using UnityEngine;

public class PlayerSpawnPoints : MonoBehaviour
{
    [SerializeField] private Transform[] _spawnPoints;

    private PlayerSpawnService _spawnService;

    private void OnEnable()
    {
        if (GameServices.Instance == null)
        {
            Debug.LogWarning("[PlayerSpawnPoints] GameServices not found. Start Play mode from Bootstrap.", this);
            return;
        }

        _spawnService = GameServices.Instance.PlayerSpawnService;
        _spawnService.RegisterSpawnPoints(this);
    }

    private void OnDisable()
    {
        if (_spawnService != null)
            _spawnService.UnregisterSpawnPoints(this);

        _spawnService = null;
    }

    public Vector3 GetSpawnPosition()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0 || _spawnPoints[0] == null)
        {
            Debug.LogError("[PlayerSpawnPoints] No spawn points assigned. Using this object's position.", this);
            return transform.position;
        }

        // One point per player is #14.
        return _spawnPoints[0].position;
    }
}
