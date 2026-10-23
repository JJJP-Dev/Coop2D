using UnityEngine;

// How strictly the host checks the skills that clients report. Shared by every skill.
[CreateAssetMenu(fileName = "SkillCastSettings", menuName = "Coop2D/Skills/Skill Cast Settings")]
public class SkillCastSettings : ScriptableObject
{
    [Tooltip("Seconds a cast may arrive before its cooldown ends on the host. " +
             "Two casts can arrive closer together than they were sent, because of network jitter.")]
    [SerializeField, Min(0f)] private float _cooldownGrace = 0.1f;

    [Tooltip("Extra units of range the host allows, because it sees the caster slightly in the past. " +
             "2 covers about 400 ms at full speed.")]
    [SerializeField, Min(0f)] private float _positionTolerance = 2f;

    [Tooltip("How far, in units, a target may be outside the shape as the host sees it. Enemies keep " +
             "moving while the cast travels to the host; 1 covers a chaser at about 200 ms of ping.")]
    [SerializeField, Min(0f)] private float _targetTolerance = 1f;

    [Tooltip("Most enemies a single cast can hit.")]
    [SerializeField, Min(1)] private int _maxTargets = 12;

    [Tooltip("Only colliders on these layers can be hit, so a skill never hurts a teammate.")]
    [SerializeField] private LayerMask _targetLayers;

    [Tooltip("Cooldown given back (Relevo, Asistencia) never ends a skill's cooldown earlier than this " +
             "part of its full time after the cast. 0.6 is the catalog's floor (engine rule 1).")]
    [SerializeField, Range(0f, 1f)] private float _minCooldownFraction = 0.6f;

    [Tooltip("Seconds in the past a client's cast tick may be. Older ones count from this far back, so a " +
             "made-up tick can't stretch the timed effects of the cast (Tempo).")]
    [SerializeField, Min(0f)] private float _maxCastTickAge = 0.5f;

    public float CooldownGrace => _cooldownGrace;
    public float PositionTolerance => _positionTolerance;
    public float TargetTolerance => _targetTolerance;
    public int MaxTargets => _maxTargets;
    public LayerMask TargetLayers => _targetLayers;
    public float MinCooldownFraction => _minCooldownFraction;
    public float MaxCastTickAge => _maxCastTickAge;
}
