using UnityEngine;

// How one kind of floating text looks and moves: a basic hit, a skill hit, a reaction's damage or its headline.
[CreateAssetMenu(fileName = "FloatingTextStyle", menuName = "Coop2D/UI/Floating Text Style")]
public class FloatingTextStyle : ScriptableObject
{
    [Header("Look")]
    [Tooltip("In world units, like the health labels.")]
    [SerializeField, Min(0.1f)] private float _fontSize = 4.5f;

    [Tooltip("Second line under a headline, with its participants.")]
    [SerializeField, Min(0.1f)] private float _subtextFontSize = 4f;

    [Tooltip("Used when the text has no color of its own, such as an element's.")]
    [SerializeField] private Color _color = Color.white;

    [Tooltip("Where it appears, from the enemy's position.")]
    [SerializeField] private Vector2 _offset = new(0f, 1.2f);

    [Tooltip("Random sideways shift, in units, so several hits on one enemy don't cover each other.")]
    [SerializeField, Min(0f)] private float _horizontalJitter;

    [Header("Motion")]
    [Tooltip("Seconds on screen.")]
    [SerializeField, Min(0.1f)] private float _lifetime = 0.6f;

    [Tooltip("Units it rises while it settles.")]
    [SerializeField] private float _rise = 0.45f;

    [Tooltip("Seconds the rise takes. It slows down at the end.")]
    [SerializeField, Min(0f)] private float _riseDuration = 0.3f;

    [Tooltip("Scale it appears at before shrinking to its size: the pop.")]
    [SerializeField, Min(0.1f)] private float _popScale = 1.25f;

    [Tooltip("Seconds the pop lasts.")]
    [SerializeField, Min(0f)] private float _popDuration = 0.08f;

    [Tooltip("Seconds it fades out at the end of its lifetime.")]
    [SerializeField, Min(0f)] private float _fadeDuration = 0.15f;

    [Header("Crowds")]
    [Tooltip("When the screen is full, lower priorities make room for higher ones. Your own hits get +1.")]
    [SerializeField] private int _priority = 1;

    [Tooltip("Seconds in which a new one joins the last one about the same thing instead of appearing: " +
             "basic hits on the same enemy add up, and the same reaction nearby counts ×2. 0 never joins.")]
    [SerializeField, Min(0f)] private float _mergeWindow;

    [Tooltip("For headlines: how close, in units, the same reaction has to be to join. 0 joins by enemy instead.")]
    [SerializeField, Min(0f)] private float _mergeDistance;

    [Tooltip("For headlines: how far up one moves when it would cover another. 0 never moves.")]
    [SerializeField, Min(0f)] private float _stackSpacing;

    [Tooltip("For headlines: how close sideways, in units, two count as covering each other.")]
    [SerializeField, Min(0f)] private float _stackWidth = 5f;

    [Tooltip("For headlines: most times one moves up before it gives up and covers another.")]
    [SerializeField, Min(0)] private int _maxStackShifts = 3;

    public float FontSize => _fontSize;
    public float SubtextFontSize => _subtextFontSize;
    public Color Color => _color;
    public Vector2 Offset => _offset;
    public float HorizontalJitter => _horizontalJitter;
    public float Lifetime => _lifetime;
    public float Rise => _rise;
    public float RiseDuration => _riseDuration;
    public float PopScale => _popScale;
    public float PopDuration => _popDuration;
    public float FadeDuration => _fadeDuration;
    public int Priority => _priority;
    public float MergeWindow => _mergeWindow;
    public float MergeDistance => _mergeDistance;
    public float StackSpacing => _stackSpacing;
    public float StackWidth => _stackWidth;
    public int MaxStackShifts => _maxStackShifts;
}
