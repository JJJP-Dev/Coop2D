using System.Collections.Generic;
using UnityEngine;

// Placeholder picture of a chain: a zigzag line that reaches one more enemy each frame, then fades.
[RequireComponent(typeof(LineRenderer))]
public class ChainVisual : MonoBehaviour
{
    [Tooltip("Seconds it takes to fade out after the last jump appears.")]
    [SerializeField, Min(0.05f)] private float _fadeDuration = 0.25f;

    [Tooltip("Zigzag points in each jump, so it reads as lightning.")]
    [SerializeField, Min(0)] private int _kinksPerJump = 3;

    [Tooltip("How far, in units, the zigzag strays from the straight line.")]
    [SerializeField, Min(0f)] private float _kinkOffset = 0.15f;

    private readonly List<Vector3> _points = new();
    private readonly List<int> _jumpEnds = new();
    private LineRenderer _line;
    private Color _color;
    private int _shownJumps;
    private float _fadeElapsed;

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _line.useWorldSpace = true;
        _line.positionCount = 0;
    }

    public void Show(IReadOnlyList<Vector2> chain, Color color)
    {
        _points.Clear();
        _jumpEnds.Clear();

        for (int i = 0; i < chain.Count; i++)
        {
            if (i > 0)
                AddKinks(chain[i - 1], chain[i]);

            _points.Add(chain[i]);

            if (i > 0)
                _jumpEnds.Add(_points.Count);
        }

        _color = color;
        _shownJumps = 0;
        _fadeElapsed = 0f;
        SetAlpha(1f);
    }

    private void AddKinks(Vector2 from, Vector2 to)
    {
        Vector2 side = Vector2.Perpendicular(to - from).normalized;

        for (int k = 1; k <= _kinksPerJump; k++)
        {
            float along = k / (float)(_kinksPerJump + 1);
            _points.Add(Vector2.Lerp(from, to, along) + side * Random.Range(-_kinkOffset, _kinkOffset));
        }
    }

    private void Update()
    {
        // One more jump per frame, as the design document asks.
        if (_shownJumps < _jumpEnds.Count)
        {
            _shownJumps++;
            int count = _jumpEnds[_shownJumps - 1];
            _line.positionCount = count;

            for (int i = 0; i < count; i++)
                _line.SetPosition(i, _points[i]);

            return;
        }

        _fadeElapsed += Time.deltaTime;
        float progress = _fadeElapsed / _fadeDuration;
        if (progress >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        SetAlpha(1f - progress);
    }

    private void SetAlpha(float alpha)
    {
        Color color = _color;
        color.a = alpha;
        _line.startColor = color;
        _line.endColor = color;
    }
}
