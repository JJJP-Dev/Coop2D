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

    public float CooldownGrace => _cooldownGrace;
    public float PositionTolerance => _positionTolerance;
    public float TargetTolerance => _targetTolerance;
    public int MaxTargets => _maxTargets;
    public LayerMask TargetLayers => _targetLayers;
}
