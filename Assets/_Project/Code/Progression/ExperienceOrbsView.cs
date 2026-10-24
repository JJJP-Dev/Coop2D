using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

// Draws the experience orbs this client received and makes the picked ones fly to whoever took
// them. Only the picture: the host already gave the experience to the group when the orb was picked.
// The sprites are pooled, since orbs come and go all match long.
public class ExperienceOrbsView : MonoBehaviour
{
    // Orbs worth this much or more look bigger, so merged ones stand out. Placeholder until the orb art.
    private const int BigOrbValue = 10;
    private const float BigOrbScale = 1.5f;

    [SerializeField] private ExperienceOrbs _orbs;

    [Tooltip("One orb's look. Copied for each orb; its scale is the size of a normal one.")]
    [SerializeField] private SpriteRenderer _orbPrefab;

    private readonly Dictionary<ushort, SpriteRenderer> _onGround = new();
    private readonly List<Flight> _flights = new();
    private readonly Stack<SpriteRenderer> _pool = new();

    // Orbs already flying that the list hasn't removed yet, so it doesn't draw them again on the ground.
    private readonly HashSet<ushort> _picked = new();

    // Reused on every change, so comparing the list allocates nothing.
    private readonly HashSet<ushort> _inList = new();
    private readonly List<ushort> _gone = new();

    private Vector3 _baseScale;

    private struct Flight
    {
        public SpriteRenderer Renderer;
        public Vector2 From;
        public Transform Target;
        public float StartTime;
    }

    private void Awake()
    {
        if (_orbs == null || _orbPrefab == null)
        {
            Debug.LogError("[ExperienceOrbsView] Orbs or orb prefab is not assigned.", this);
            enabled = false;
            return;
        }

        _baseScale = _orbPrefab.transform.localScale;
    }

    private void OnEnable()
    {
        if (_orbs == null)
            return;

        _orbs.OrbsChanged += OnOrbsChanged;
        _orbs.OrbPicked += OnOrbPicked;
    }

    private void OnDisable()
    {
        if (_orbs == null)
            return;

        _orbs.OrbsChanged -= OnOrbsChanged;
        _orbs.OrbPicked -= OnOrbPicked;
    }

    private void Update()
    {
        float duration = _orbs.Settings != null ? _orbs.Settings.FlightDuration : 0f;

        for (int i = _flights.Count - 1; i >= 0; i--)
        {
            Flight flight = _flights[i];
            float t = duration > 0f ? (Time.time - flight.StartTime) / duration : 1f;

            // The player may have left mid-flight.
            if (t >= 1f || flight.Target == null)
            {
                Release(flight.Renderer);
                _flights.RemoveAt(i);
                continue;
            }

            // Slow at first and fast at the end, like being pulled in.
            flight.Renderer.transform.position = Vector2.Lerp(flight.From, flight.Target.position, t * t);
        }
    }

    // Makes the orbs on screen match the list: draws the new ones, resizes the merged ones and takes
    // away those that are gone. At most 40 orbs, only when the list changes, so it costs nothing.
    private void OnOrbsChanged()
    {
        _inList.Clear();

        foreach (ExperienceOrb orb in _orbs.Orbs)
        {
            _inList.Add(orb.Id);

            // Already flying to whoever picked it; the list just hasn't let it go yet.
            if (_picked.Contains(orb.Id))
                continue;

            if (!_onGround.TryGetValue(orb.Id, out SpriteRenderer renderer))
            {
                renderer = Take();
                renderer.transform.position = orb.Position;
                _onGround.Add(orb.Id, renderer);
            }

            SetSize(renderer, orb.Value);
        }

        _gone.Clear();
        foreach (ushort id in _onGround.Keys)
        {
            if (!_inList.Contains(id))
                _gone.Add(id);
        }

        foreach (ushort id in _gone)
        {
            Release(_onGround[id]);
            _onGround.Remove(id);
        }

        // Flying orbs the list has let go no longer need to be remembered.
        _picked.IntersectWith(_inList);
    }

    // The orb on the ground becomes the one that flies. If the list removed it first, a new one flies
    // from where it was. The host sends this before removing it, so it usually comes first.
    private void OnOrbPicked(ushort id, Vector2 position, NetworkObject picker)
    {
        if (!_onGround.Remove(id, out SpriteRenderer renderer))
            renderer = Take();

        // Until a list without it arrives, it must not be drawn again on the ground.
        _picked.Add(id);

        if (picker == null)
        {
            Release(renderer);
            return;
        }

        renderer.transform.position = position;
        _flights.Add(new Flight { Renderer = renderer, From = position, Target = picker.transform, StartTime = Time.time });
    }

    private void SetSize(SpriteRenderer renderer, int value)
    {
        renderer.transform.localScale = value >= BigOrbValue ? _baseScale * BigOrbScale : _baseScale;
    }

    private SpriteRenderer Take()
    {
        SpriteRenderer renderer = _pool.Count > 0 ? _pool.Pop() : Instantiate(_orbPrefab, transform);
        renderer.gameObject.SetActive(true);
        return renderer;
    }

    private void Release(SpriteRenderer renderer)
    {
        renderer.gameObject.SetActive(false);
        _pool.Push(renderer);
    }
}
