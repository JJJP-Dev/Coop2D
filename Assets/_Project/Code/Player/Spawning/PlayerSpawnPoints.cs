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

    public Vector3 GetSpawnPosition(int slot)
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.LogError("[PlayerSpawnPoints] No spawn points assigned. Using this object's position.", this);
            return transform.position;
        }

        // With fewer points than players, they wrap around and share.
        int index = slot % _spawnPoints.Length;

        if (_spawnPoints[index] == null)
        {
            Debug.LogError($"[PlayerSpawnPoints] Spawn point {index} is empty. Using this object's position.", this);
            return transform.position;
        }

        return _spawnPoints[index].position;
    }
}
