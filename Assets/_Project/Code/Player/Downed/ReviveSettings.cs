using UnityEngine;

// How a downed player gets back up (#51). The limits of #79 (shared charges, +1 s per revive used,
// a hit stops it) will be added here.
[CreateAssetMenu(fileName = "ReviveSettings", menuName = "Coop2D/Player/Revive Settings")]
public class ReviveSettings : ScriptableObject
{
    [Tooltip("A living teammate this close to the downed player, in units (1 unit = 1 tile), revives it.")]
    [SerializeField, Min(0.1f)] private float _radius = 1.5f;

    [Tooltip("Seconds a teammate has to stay close. If nobody is close, the progress is lost.")]
    [SerializeField, Min(0.1f)] private float _duration = 3f;

    [Tooltip("Share of its maximum health the player gets up with. 0.5 is 50 with 100.")]
    [SerializeField, Range(0.01f, 1f)] private float _healthFraction = 0.5f;

    [Tooltip("Seconds after getting up in which hits are ignored, so the crowd around it can't drop it again at once.")]
    [SerializeField, Min(0f)] private float _invulnerableDuration = 1f;

    public float Radius => _radius;
    public float Duration => _duration;
    public float HealthFraction => _healthFraction;
    public float InvulnerableDuration => _invulnerableDuration;
}
