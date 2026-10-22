using UnityEngine;

// How a downed player gets back up (#51) and how often the group can do it (#79).
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

    [Tooltip("Revives the whole group can use in a match, by players: the first number for 1 player, the second " +
             "for 2... More players than numbers use the last one. It counts the most players the match has had, " +
             "so someone leaving never takes charges away (#79).")]
    [SerializeField] private int[] _chargesByPlayers = { 1, 2, 3, 3 };

    public float Radius => _radius;
    public float Duration => _duration;
    public float HealthFraction => _healthFraction;
    public float InvulnerableDuration => _invulnerableDuration;

    /// <summary>
    /// The group's revives for a match that has had this many players at most. 0 if the list is empty.
    /// </summary>
    public int GetCharges(int players)
    {
        if (_chargesByPlayers == null || _chargesByPlayers.Length == 0)
            return 0;

        int index = Mathf.Clamp(players, 1, _chargesByPlayers.Length) - 1;
        return Mathf.Max(0, _chargesByPlayers[index]);
    }
}
