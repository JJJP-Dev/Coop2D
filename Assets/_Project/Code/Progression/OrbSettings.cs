using UnityEngine;

// How experience orbs are picked up and how many there can be (#84).
[CreateAssetMenu(fileName = "OrbSettings", menuName = "Coop2D/Progression/Orb Settings")]
public class OrbSettings : ScriptableObject
{
    [Tooltip("A standing player this close to an orb, in units (1 unit = 1 tile), picks it up and it flies " +
             "to them. The Magnet upgrade will grow it (pickup_radius).")]
    [SerializeField, Min(0.1f)] private float _pickupRadius = 1.5f;

    [Tooltip("Orbs on the ground at most. A new one beyond this adds its value to the nearest, " +
             "so the host never syncs hundreds of them.")]
    [SerializeField, Min(1)] private int _maxOrbs = 40;

    [Tooltip("Seconds a picked orb takes to fly to the player. Only the picture: the experience is already the group's.")]
    [SerializeField, Min(0f)] private float _flightDuration = 0.25f;

    public float PickupRadius => _pickupRadius;
    public int MaxOrbs => _maxOrbs;
    public float FlightDuration => _flightDuration;
}
