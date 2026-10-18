using System;
using TMPro;
using UnityEngine;

// One pooled text: it pops in, rises a little and fades out. FloatingTextSpawner reuses it.
public class FloatingText : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;

    [Tooltip("Optional second line, without the text's gradient: a reaction's participants.")]
    [SerializeField] private TMP_Text _subtext;

    [Tooltip("Dark outline so it reads on any background, as a fraction of the font size.")]
    [SerializeField, Range(0f, 0.5f)] private float _outlineWidth = 0.25f;

    [SerializeField] private Color _outlineColor = new(0.05f, 0.05f, 0.1f, 1f);

    private FloatingTextStyle _style;
    private Action<FloatingText> _finished;
    private Vector3 _origin;
    private float _elapsed;

    public FloatingTextStyle Style => _style;
    public int Priority { get; private set; }
    public Vector3 Origin => _origin;

    // Seconds since it last started playing.
    public float Age => _elapsed;

    // For the spawner: what this text is about (an enemy, a reaction) and its running total.
    public object MergeKey { get; set; }
    public int MergeCount { get; set; }

    private void Awake()
    {
        if (_text == null)
            Debug.LogError("[FloatingText] Text is not assigned.", this);

        SetOutline(_text);
        SetOutline(_subtext);
    }

    public void Init(Action<FloatingText> finished)
    {
        _finished = finished;
    }

    /// <summary>
    /// With a gradient, the text is drawn in it instead of in color.
    /// </summary>
    public void Play(FloatingTextStyle style, Vector3 origin, string text, Color color, int priority,
                     VertexGradient? gradient = null, string subtext = null)
    {
        _style = style;
        Priority = priority;

        _text.fontSize = style.FontSize;
        _text.color = gradient.HasValue ? Color.white : color;
        _text.enableVertexGradient = gradient.HasValue;
        if (gradient.HasValue)
            _text.colorGradient = gradient.Value;

        if (_subtext != null)
        {
            bool hasSubtext = !string.IsNullOrEmpty(subtext);
            _subtext.gameObject.SetActive(hasSubtext);

            if (hasSubtext)
            {
                _subtext.fontSize = style.SubtextFontSize;
                _subtext.text = subtext;
            }
        }

        gameObject.SetActive(true);
        Restart(text, origin);
    }

    /// <summary>
    /// Plays it again from the start, as when a number adds up another hit.
    /// Null keeps the current text or position.
    /// </summary>
    public void Restart(string text = null, Vector3? origin = null)
    {
        if (text != null)
            _text.text = text;

        if (origin.HasValue)
            _origin = origin.Value;

        _elapsed = 0f;
        Apply();
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        if (_elapsed >= _style.Lifetime)
        {
            gameObject.SetActive(false);
            _finished?.Invoke(this);
            return;
        }

        Apply();
    }

    private void Apply()
    {
        // Rises fast and settles.
        float rise = _style.RiseDuration > 0f ? Mathf.Clamp01(_elapsed / _style.RiseDuration) : 1f;
        rise = 1f - (1f - rise) * (1f - rise);
        transform.position = _origin + Vector3.up * (_style.Rise * rise);

        float pop = _style.PopDuration > 0f ? Mathf.Clamp01(_elapsed / _style.PopDuration) : 1f;
        transform.localScale = Vector3.one * Mathf.Lerp(_style.PopScale, 1f, pop);

        float fadeStart = _style.Lifetime - _style.FadeDuration;
        float alpha = _style.FadeDuration > 0f && _elapsed > fadeStart
            ? 1f - (_elapsed - fadeStart) / _style.FadeDuration
            : 1f;

        _text.alpha = alpha;
        if (_subtext != null)
            _subtext.alpha = alpha;
    }

    // Each pooled text gets its own copy of the font material, once.
    private void SetOutline(TMP_Text text)
    {
        if (text == null)
            return;

        text.outlineWidth = _outlineWidth;
        text.outlineColor = _outlineColor;
    }
}
