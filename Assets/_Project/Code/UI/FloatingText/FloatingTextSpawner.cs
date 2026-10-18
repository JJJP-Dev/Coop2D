using System.Collections.Generic;
using FishNet.Object;
using TMPro;
using UnityEngine;

// Floating numbers for hits and a headline for each reaction, on this client, from the host's events.
// It keeps a pool of at most Max Texts; when it is full, lower priorities make room for higher ones.
public class FloatingTextSpawner : MonoBehaviour
{
    [SerializeField] private FloatingText _prefab;

    [Tooltip("Most texts on screen at once.")]
    [SerializeField, Min(1)] private int _maxTexts = 24;

    [Header("Styles")]
    [SerializeField] private FloatingTextStyle _basicDamage;
    [SerializeField] private FloatingTextStyle _skillDamage;
    [SerializeField] private FloatingTextStyle _reactionDamage;
    [SerializeField] private FloatingTextStyle _reactionHeadline;

    [Header("Colors")]
    [SerializeField] private ElementLibrary _elements;
    [SerializeField] private PlayerColors _playerColors;

    [SerializeField] private Color _duoColor = Color.white;

    [Tooltip("Quieter than «¡DÚO!», so a reaction between two players stands out.")]
    [SerializeField] private Color _soloColor = new(0.757f, 0.694f, 0.608f);

    private readonly List<FloatingText> _active = new();
    private readonly Stack<FloatingText> _pool = new();
    private CombatEventService _events;

    private void OnEnable()
    {
        if (_prefab == null || _basicDamage == null || _skillDamage == null || _reactionDamage == null ||
            _reactionHeadline == null || _elements == null || _playerColors == null)
        {
            Debug.LogError("[FloatingTextSpawner] A prefab, style or color asset is not assigned.", this);
            return;
        }

        _events = GameServices.Instance.CombatEventService;
        _events.HitConfirmed += OnHitConfirmed;
        _events.ReactionTriggered += OnReactionTriggered;
    }

    private void OnDisable()
    {
        if (_events == null)
            return;

        _events.HitConfirmed -= OnHitConfirmed;
        _events.ReactionTriggered -= OnReactionTriggered;
        _events = null;
    }

    private void OnHitConfirmed(HitInfo hit)
    {
        FloatingTextStyle style = GetStyle(hit.Kind);
        Vector3 origin = GetAnchor(hit.Target, hit.Position) + (Vector3)style.Offset;

        // Basic hits on the same enemy close together add up in one number.
        if (TryFindMergeable(style, hit.Target, origin, out FloatingText existing))
        {
            existing.MergeCount += hit.Amount;
            existing.Restart(existing.MergeCount.ToString(), origin);
            return;
        }

        int priority = style.Priority + (hit.ByLocalPlayer ? 1 : 0);
        FloatingText text = Spawn(priority);
        if (text == null)
            return;

        origin.x += Random.Range(-style.HorizontalJitter, style.HorizontalJitter);
        text.MergeKey = hit.Target;
        text.MergeCount = hit.Amount;
        text.Play(style, origin, hit.Amount.ToString(), GetHitColor(hit, style), priority);
    }

    private void OnReactionTriggered(ReactionInfo reaction)
    {
        FloatingTextStyle style = _reactionHeadline;
        ReactionDefinition definition = reaction.Reaction;
        Vector3 origin = GetAnchor(reaction.Target, reaction.Position) + (Vector3)style.Offset;

        // The same reaction nearby a moment later: one headline that counts them.
        if (TryFindMergeable(style, definition, origin, out FloatingText existing))
        {
            existing.MergeCount++;
            existing.Restart(FormatHeadline(definition, existing.MergeCount));
            return;
        }

        FloatingText text = Spawn(style.Priority);
        if (text == null)
            return;

        VertexGradient gradient = new(definition.HeadlineTopColor, definition.HeadlineTopColor,
                                      definition.HeadlineBottomColor, definition.HeadlineBottomColor);

        text.MergeKey = definition;
        text.MergeCount = 1;
        text.Play(style, MakeRoom(style, origin), FormatHeadline(definition, 1), Color.white, style.Priority,
                  gradient, FormatParticipants(reaction));
    }

    private FloatingTextStyle GetStyle(DamageKind kind)
    {
        return kind switch
        {
            DamageKind.Skill => _skillDamage,
            DamageKind.Reaction => _reactionDamage,
            _ => _basicDamage
        };
    }

    // Skills show their element. Basic hits stay white so they don't compete with them.
    private Color GetHitColor(HitInfo hit, FloatingTextStyle style)
    {
        if (hit.Kind != DamageKind.Skill)
            return style.Color;

        ElementDefinition element = _elements.Get(hit.Element);
        return element != null ? element.Color : style.Color;
    }

    // Where this client sees the target now, or where the host saw it if it's gone.
    private static Vector3 GetAnchor(Component target, Vector2 fallback)
    {
        return target != null ? target.transform.position : (Vector3)fallback;
    }

    private bool TryFindMergeable(FloatingTextStyle style, object key, Vector3 origin, out FloatingText found)
    {
        found = null;
        if (style.MergeWindow <= 0f || key == null)
            return false;

        foreach (FloatingText text in _active)
        {
            if (text.Style != style || !Equals(text.MergeKey, key) || text.Age >= style.MergeWindow)
                continue;

            if (style.MergeDistance > 0f && Vector2.Distance(text.Origin, origin) > style.MergeDistance)
                continue;

            found = text;
            return true;
        }

        return false;
    }

    // A headline that would cover another moves up a little, a few times at most.
    private Vector3 MakeRoom(FloatingTextStyle style, Vector3 origin)
    {
        if (style.StackSpacing <= 0f)
            return origin;

        for (int shift = 0; shift < style.MaxStackShifts && CoversAnother(style, origin); shift++)
            origin.y += style.StackSpacing;

        return origin;
    }

    private bool CoversAnother(FloatingTextStyle style, Vector3 origin)
    {
        foreach (FloatingText text in _active)
        {
            if (text.Style == style && Mathf.Abs(text.Origin.x - origin.x) < style.StackWidth &&
                Mathf.Abs(text.Origin.y - origin.y) < style.StackSpacing)
                return true;
        }

        return false;
    }

    private static string FormatHeadline(ReactionDefinition definition, int count)
    {
        string name = definition.DisplayName.ToUpperInvariant();
        return count > 1 ? $"{name}! ×{count}" : $"{name}!";
    }

    // «J1 + J2  ¡DÚO!» in each player's color, or «J1  EN SOLITARIO».
    private string FormatParticipants(ReactionInfo reaction)
    {
        string auraOwner = FormatPlayer(reaction.AuraOwner);
        string detonator = FormatPlayer(reaction.Detonator);

        if (reaction.IsDuo)
            return $"{auraOwner} + {detonator}  <color=#{ColorUtility.ToHtmlStringRGB(_duoColor)}>¡DÚO!</color>";

        string player = detonator.Length > 0 ? detonator : auraOwner;
        return $"{player}  <color=#{ColorUtility.ToHtmlStringRGB(_soloColor)}>EN SOLITARIO</color>";
    }

    private string FormatPlayer(NetworkObject player)
    {
        if (player == null || !player.TryGetComponent(out PlayerIdentity identity) || identity.Slot < 0)
            return string.Empty;

        string color = ColorUtility.ToHtmlStringRGB(_playerColors.GetColor(identity.Slot));
        return $"<color=#{color}>J{identity.Slot + 1}</color>";
    }

    // When it's full, the oldest of the lowest priority makes room, unless the new one matters less.
    private FloatingText Spawn(int priority)
    {
        if (_active.Count >= _maxTexts)
        {
            FloatingText lowest = null;
            foreach (FloatingText text in _active)
            {
                if (lowest == null || text.Priority < lowest.Priority)
                    lowest = text;
            }

            if (lowest == null || lowest.Priority > priority)
                return null;

            Release(lowest);
        }

        FloatingText next = _pool.Count > 0 ? _pool.Pop() : CreateText();
        _active.Add(next);
        return next;
    }

    private FloatingText CreateText()
    {
        FloatingText text = Instantiate(_prefab, transform);
        text.Init(Release);
        return text;
    }

    private void Release(FloatingText text)
    {
        if (!_active.Remove(text))
            return;

        text.gameObject.SetActive(false);
        text.MergeKey = null;
        _pool.Push(text);
    }
}
