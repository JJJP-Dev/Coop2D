using TMPro;
using UnityEngine;

// The group's experience bar and level in the HUD. Only shows what RunExperience received.
// The fill is a child stretched by its anchors, not an Image in Filled mode, so it needs no sprite
// until the HUD art arrives.
public class ExperienceBarView : MonoBehaviour
{
    [Tooltip("Child of the bar's background, pivot and anchors on the left. Its right anchor follows the bar.")]
    [SerializeField] private RectTransform _fill;

    [SerializeField] private TMP_Text _levelLabel;

    private int _shownLevel = -1;
    private float _shownFraction = -1f;

    private void Awake()
    {
        if (_fill == null || _levelLabel == null)
        {
            Debug.LogError("[ExperienceBarView] Fill or level label is not assigned.", this);
            enabled = false;
        }
    }

    // Polled: two values, and it also shows the right bar to a player who arrives late.
    private void Update()
    {
        RunExperience experience = RunExperience.Current;
        int level = experience != null ? experience.Level : 1;
        float fraction = experience != null ? experience.Fraction : 0f;

        if (level != _shownLevel)
        {
            _shownLevel = level;
            _levelLabel.text = $"Nv {level}";
        }

        // Anchors only change when the bar moves, so the canvas isn't rebuilt every frame.
        if (!Mathf.Approximately(fraction, _shownFraction))
        {
            _shownFraction = fraction;
            Vector2 anchorMax = _fill.anchorMax;
            anchorMax.x = fraction;
            _fill.anchorMax = anchorMax;
        }
    }
}
