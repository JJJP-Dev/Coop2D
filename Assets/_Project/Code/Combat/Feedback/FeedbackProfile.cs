using UnityEngine;

// How strongly each event answers on screen. Presentation only: none of it changes the game.
// For now only the hit flash (#58); hitstop (#54) and the rest of the design document's table come later.
[CreateAssetMenu(fileName = "FeedbackProfile", menuName = "Coop2D/Combat/Feedback Profile")]
public class FeedbackProfile : ScriptableObject
{
    [Tooltip("Seconds a sprite stays white after a hit. The design document asks for about 0.05.")]
    [SerializeField, Min(0f)] private float _hitFlashDuration = 0.05f;

    public float HitFlashDuration => _hitFlashDuration;
}
