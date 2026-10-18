using TMPro;
using UnityEngine;

// Temporary view of the synced health, until the real health bars exist (#59).
public class HealthDebugLabel : MonoBehaviour
{
    [SerializeField] private Health _health;
    [SerializeField] private TMP_Text _text;

    private void OnEnable()
    {
        if (_health == null || _text == null)
        {
            Debug.LogError("[HealthDebugLabel] Health or text is not assigned.", this);
            return;
        }

        _health.Changed += Refresh;
        Refresh(_health);
    }

    private void Start()
    {
        // OnEnable can run before Health's Awake on a new instance. Start runs after every Awake.
        if (_health != null && _text != null)
            Refresh(_health);
    }

    private void OnDisable()
    {
        if (_health != null)
            _health.Changed -= Refresh;
    }

    private void Refresh(Health health)
    {
        string text = $"{health.Current}/{health.Max}";
        _text.text = health.IsInvulnerable ? $"{text} · invulnerable" : text;
    }
}
