using UnityEngine;

// Placeholder picture of a circle skill: it appears at full size and fades out.
[RequireComponent(typeof(SpriteRenderer))]
public class SkillCircleVisual : MonoBehaviour
{
    [Tooltip("Seconds it takes to fade out.")]
    [SerializeField, Min(0.05f)] private float _fadeDuration = 0.3f;

    [Tooltip("Opacity when it appears, so what is under it stays visible.")]
    [SerializeField, Range(0f, 1f)] private float _startAlpha = 0.5f;

    private SpriteRenderer _renderer;
    private Color _color;
    private float _elapsed;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    public void Show(float radius, Color color)
    {
        // Scaled from the sprite's own size, whatever its pixels per unit.
        float diameter = _renderer.sprite != null ? _renderer.sprite.bounds.size.x : 0f;
        if (diameter <= 0f)
            diameter = 1f;

        transform.localScale = Vector3.one * (radius * 2f / diameter);
        _color = color;
        _elapsed = 0f;
        SetAlpha(_startAlpha);
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        float progress = _elapsed / _fadeDuration;
        if (progress >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        SetAlpha(_startAlpha * (1f - progress));
    }

    private void SetAlpha(float alpha)
    {
        Color color = _color;
        color.a = alpha;
        _renderer.color = color;
    }
}
