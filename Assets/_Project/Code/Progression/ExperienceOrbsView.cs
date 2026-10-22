using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

// Draws the experience orbs this client received (#84) and makes the picked ones fly to whoever took
// them. Only the picture: the host already gave the experience to the group when the orb was picked.
// The sprites are pooled, since orbs come and go all match long.
public class ExperienceOrbsView : MonoBehaviour
{
    // Orbs worth this much or more look bigger, so merged ones stand out. Placeholder until the orb art (#83).
    private const int BigOrbValue = 10;
    private const float BigOrbScale = 1.5f;

    [SerializeField] private ExperienceOrbs _orbs;

    [Tooltip("One orb's look. Copied for each orb; its scale is the size of a normal one.")]
    [SerializeField] private SpriteRenderer _orbPrefab;

    private readonly Dictionary<ushort, SpriteRenderer> _onGround = new();
    private readonly List<Flight> _flights = new();
    private readonly Stack<SpriteRenderer> _pool = new();

    // Orbs already flying that the list hasn't removed yet, so their removal doesn't hide the flight.
    private readonly HashSet<ushort> _picked = new();

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

        _orbs.OrbAdded += OnOrbAdded;
        _orbs.OrbChanged += OnOrbChanged;
        _orbs.OrbRemoved += OnOrbRemoved;
        _orbs.OrbsCleared += OnOrbsCleared;
        _orbs.OrbPicked += OnOrbPicked;
    }

    private void OnDisable()
    {
        if (_orbs == null)
            return;

        _orbs.OrbAdded -= OnOrbAdded;
        _orbs.OrbChanged -= OnOrbChanged;
        _orbs.OrbRemoved -= OnOrbRemoved;
        _orbs.OrbsCleared -= OnOrbsCleared;
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

    // Also for the orbs already there when this client arrived, which may come twice: once from the
    // list and once from FishNet's first changes. The id keeps them as one.
    private void OnOrbAdded(ExperienceOrb orb)
    {
        if (!_onGround.TryGetValue(orb.Id, out SpriteRenderer renderer))
        {
            renderer = Take();
            _onGround.Add(orb.Id, renderer);
        }

        renderer.transform.position = orb.Position;
        SetSize(renderer, orb.Value);
    }

    // A merged orb is worth more.
    private void OnOrbChanged(ExperienceOrb orb)
    {
        if (_onGround.TryGetValue(orb.Id, out SpriteRenderer renderer))
            SetSize(renderer, orb.Value);
    }

    private void OnOrbRemoved(ushort id)
    {
        // Already flying: the flight ends by itself.
        if (_picked.Remove(id))
            return;

        if (_onGround.Remove(id, out SpriteRenderer renderer))
            Release(renderer);
    }

    private void OnOrbsCleared()
    {
        foreach (SpriteRenderer renderer in _onGround.Values)
            Release(renderer);

        _onGround.Clear();
        _picked.Clear();
    }

    // The orb on the ground becomes the one that flies. If the list removed it first, a new one flies
    // from where it was, and there is nothing left to wait for.
    private void OnOrbPicked(ushort id, Vector2 position, NetworkObject picker)
    {
        if (_onGround.Remove(id, out SpriteRenderer renderer))
            _picked.Add(id);
        else
            renderer = Take();

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
