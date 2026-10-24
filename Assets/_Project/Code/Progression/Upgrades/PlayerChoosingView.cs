using TMPro;
using UnityEngine;

// Above a player while they have the upgrade panel open (#77), so the group knows they're looking at
// the strip and not at the fight. A label until the icon's art exists. Every client shows it from what
// PlayerOffers received.
public class PlayerChoosingView : MonoBehaviour
{
    [Tooltip("Optional: empty, it's looked up on the player this view is part of.")]
    [SerializeField] private PlayerOffers _offers;

    [Tooltip("Above the player, higher than the downed label: a downed player can choose too.")]
    [SerializeField] private TMP_Text _label;

    private bool? _shownChoosing;

    private void Awake()
    {
        if (_offers == null)
            _offers = GetComponentInParent<PlayerOffers>();

        if (_offers == null || _label == null)
        {
            Debug.LogError("[PlayerChoosingView] No PlayerOffers on the player, or the label is not assigned.", this);
            enabled = false;
            return;
        }

        _label.text = "";
    }

    // One value per player, and it also catches the one a late joiner receives with the spawn.
    private void Update()
    {
        bool choosing = _offers.IsChoosing;
        if (_shownChoosing == choosing)
            return;

        _shownChoosing = choosing;
        _label.text = choosing ? "Eligiendo" : "";
    }
}
