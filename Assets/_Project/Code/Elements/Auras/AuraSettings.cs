using UnityEngine;

[CreateAssetMenu(fileName = "AuraSettings", menuName = "Coop2D/Elements/Aura Settings")]
public class AuraSettings : ScriptableObject
{
    [Tooltip("Seconds a weak aura lasts. The basic attack leaves weak auras.")]
    [SerializeField, Min(0.1f)] private float _weakDuration = 2f;

    [Tooltip("Seconds a strong aura lasts. Skills leave strong auras.")]
    [SerializeField, Min(0.1f)] private float _strongDuration = 4f;

    [Tooltip("Seconds before the same attack of the same player can apply or refresh an aura " +
             "on the same enemy again. Its hits still deal damage meanwhile.")]
    [SerializeField, Min(0f)] private float _applyCooldown = 1f;

    public float ApplyCooldown => _applyCooldown;

    public float GetDuration(AuraStrength strength)
    {
        return strength == AuraStrength.Strong ? _strongDuration : _weakDuration;
    }
}
