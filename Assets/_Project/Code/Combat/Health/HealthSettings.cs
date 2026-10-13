using UnityEngine;

[CreateAssetMenu(fileName = "HealthSettings", menuName = "Coop2D/Combat/Health Settings")]
public class HealthSettings : ScriptableObject
{
    [SerializeField, Min(1)] private int _maxHealth = 100;

    [Tooltip("Seconds after a hit in which more damage is ignored. Keep it at 0 for enemies, " +
             "or two players hitting at the same time would lose damage.")]
    [SerializeField, Min(0f)] private float _invulnerabilityDuration = 0.5f;

    public int MaxHealth => _maxHealth;
    public float InvulnerabilityDuration => _invulnerabilityDuration;
}
