using UnityEngine;

// How much the camera shakes when a reaction triggers on screen.
[CreateAssetMenu(fileName = "ReactionShakeSettings", menuName = "Coop2D/Camera/Reaction Shake Settings")]
public class ReactionShakeSettings : ScriptableObject
{
    [Tooltip("Roughly how far the camera moves, in units, for a reaction by one player. " +
             "A pixel is 0.0625. 0 turns it off.")]
    [SerializeField, Min(0f)] private float _soloStrength = 0.1f;

    [Tooltip("The same for a reaction between two players, which should feel bigger.")]
    [SerializeField, Min(0f)] private float _duoStrength = 0.2f;

    [Tooltip("Reactions farther than this from the center of the screen, in units, don't shake it.")]
    [SerializeField, Min(0f)] private float _maxDistance = 12f;

    [Tooltip("Seconds after a shake during which new reactions don't shake again, " +
             "so a crowd doesn't keep the screen trembling.")]
    [SerializeField, Min(0f)] private float _minInterval = 0.15f;

    public float SoloStrength => _soloStrength;
    public float DuoStrength => _duoStrength;
    public float MaxDistance => _maxDistance;
    public float MinInterval => _minInterval;
}
