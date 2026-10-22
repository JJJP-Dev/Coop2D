using TMPro;
using UnityEngine;

// Shows that a player is downed (#51), with placeholders until there is art: the body fades and a
// label above it says so. Only the picture: every client works it out from the health it receives.
public class PlayerDownedView : MonoBehaviour
{
    // Placeholder look until the downed sprite exists.
    private const float DownedAlpha = 0.4f;

    [SerializeField] private PlayerDowned _downed;

    [Tooltip("The body's sprite, which fades while the player is downed. Not the slot marker, " +
             "so everyone still sees whose body it is.")]
    [SerializeField] private SpriteRenderer _visual;

    [Tooltip("Above the player. Empty while it's up.")]
    [SerializeField] private TMP_Text _label;

    private Color _baseColor;
    private bool? _shownDowned;

    private void Awake()
    {
        if (_downed == null || _visual == null || _label == null)
        {
            Debug.LogError("[PlayerDownedView] Downed, visual or label is not assigned.", this);
            enabled = false;
            return;
        }

        // The color is the vertex tint, which the hit flash's material swap leaves alone.
        _baseColor = _visual.color;
        _label.text = "";
    }

    // Checked every frame instead of waiting for Health.Changed: it's one value per player, and it
    // also catches the health a late joiner receives with the spawn.
    private void Update()
    {
        bool downed = _downed.IsDowned;
        if (_shownDowned == downed)
            return;

        _shownDowned = downed;

        Color color = _baseColor;
        if (downed)
            color.a *= DownedAlpha;

        _visual.color = color;
        _label.text = downed ? "Derribado" : "";
    }
}
