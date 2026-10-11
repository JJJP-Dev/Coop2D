using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(PlayerIdentity))]
public class PlayerColorMarker : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer _marker;
    [SerializeField] private PlayerColors _colors;

    private PlayerIdentity _identity;

    private void Awake()
    {
        _identity = GetComponent<PlayerIdentity>();

        if (_marker == null || _colors == null)
            Debug.LogError("[PlayerColorMarker] Marker or colors are not assigned.", this);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // SyncVars from the spawn message are already applied here,
        // and the slot never changes while the player exists.
        if (_marker != null && _colors != null)
            _marker.color = _colors.GetColor(_identity.Slot);
    }
}
