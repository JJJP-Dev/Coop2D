using UnityEngine;

// Placeholder picture of a circle: a reaction's burst or a Vapor cloud. Skills use SkillShapeVisual.
// It appears at full size, can stay a while, and fades out.
[RequireComponent(typeof(SpriteRenderer))]
public class SkillCircleVisual : MonoBehaviour
{
    [Tooltip("Seconds it takes to fade out.")]
    [SerializeField, Min(0.05f)] private float _fadeDuration = 0.3f;

    [Tooltip("Opacity when it appears, so what is under it stays visible.")]
    [SerializeField, Range(0f, 1f)] private float _startAlpha = 0.5f;

    private SpriteRenderer _renderer;
    private Color _color;
    private float _holdDuration;
    private float _elapsed;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// The color's alpha scales the start opacity. holdDuration keeps it at full opacity
    /// for that many seconds before it fades, as a cloud does.
    /// </summary>
    public void Show(float radius, Color color, float holdDuration = 0f)
    {
        // Scaled from the sprite's own size, whatever its pixels per unit.
        float diameter = _renderer.sprite != null ? _renderer.sprite.bounds.size.x : 0f;
        if (diameter <= 0f)
            diameter = 1f;

        transform.localScale = Vector3.one * (radius * 2f / diameter);
        _color = color;
        _holdDuration = Mathf.Max(0f, holdDuration);
        _elapsed = 0f;
        SetAlpha(1f);
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        float progress = (_elapsed - _holdDuration) / _fadeDuration;
        if (progress >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        SetAlpha(1f - Mathf.Clamp01(progress));
    }

    private void SetAlpha(float fade)
    {
        Color color = _color;
        color.a = _startAlpha * _color.a * fade;
        _renderer.color = color;
    }
}
