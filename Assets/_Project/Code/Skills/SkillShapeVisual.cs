using System.Collections.Generic;
using UnityEngine;

// Placeholder picture of where a skill hit: its circle, line or cone, filled with the element's
// colour, that flashes and fades out. It builds a small mesh from the cast's SkillArea, so one prefab
// draws every shape at its exact size. Only the picture: hits are decided by SkillHitResolver.
// Three things make it readable without art:
//   - A skill that can detonate gets a bright edge; one that only marks is the fill alone, so players
//     can tell which casts set off reactions.
//   - Your own casts are drawn stronger than your teammates', so yours stand out in a crowded fight.
//   - A wall or a turret stays (ShowLasting): it dims as its time runs out and flashes on each hit.
// The real effects replace it with the skills' art.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SkillShapeVisual : MonoBehaviour
{
    // Segments of a full circle; a cone uses its share of them.
    private const int CircleSegments = 40;

    [Tooltip("Seconds it takes to fade out.")]
    [SerializeField, Min(0.05f)] private float _fadeDuration = 0.5f;

    [Tooltip("Opacity of the fill when it appears, so what is under it stays visible.")]
    [SerializeField, Range(0f, 1f)] private float _startAlpha = 0.5f;

    [Tooltip("Skills that can detonate: opacity of their edge, and its width in units.")]
    [SerializeField, Range(0f, 1f)] private float _edgeAlpha = 0.95f;
    [SerializeField, Min(0.02f)] private float _edgeWidth = 0.1f;

    [Tooltip("How much the edge goes towards white, so it reads on top of its own fill.")]
    [SerializeField, Range(0f, 1f)] private float _edgeWhiten = 0.35f;

    [Tooltip("Teammates' casts are drawn at this share of the opacity of your own.")]
    [SerializeField, Range(0f, 1f)] private float _othersOpacity = 0.55f;

    [Tooltip("Lasting shapes: the share of the opacity left just before they end.")]
    [SerializeField, Range(0f, 1f)] private float _lastingEndFade = 0.4f;

    [Tooltip("Extra opacity of the flash when it appears and on each hit of a lasting shape, and the " +
             "seconds it takes to go.")]
    [SerializeField, Range(0f, 1f)] private float _pulseAlpha = 0.35f;
    [SerializeField, Min(0.01f)] private float _pulseDuration = 0.15f;

    private readonly List<Vector3> _vertices = new();
    private readonly List<int> _triangles = new();

    // Per vertex: 0 for the fill, 1 for the edge.
    private readonly List<float> _edge = new();

    private Mesh _mesh;
    private Color[] _colors;
    private Color _color;
    private float _opacity;
    private float _elapsed;

    // 0 for a cast that just fades.
    private float _lifetime;
    private float _pulseInterval;

    /// <summary>
    /// Draws that area in that colour. The object can be anywhere: the mesh is placed around the area.
    /// edge for a skill that can detonate; mine false for a teammate's cast, drawn weaker.
    /// </summary>
    public void Show(SkillArea area, Color color, bool edge = false, bool mine = true)
    {
        transform.SetPositionAndRotation(area.Origin, Quaternion.identity);
        transform.localScale = Vector3.one;

        _vertices.Clear();
        _triangles.Clear();
        _edge.Clear();
        BuildFill(area);
        if (edge)
            BuildEdge(area);

        _mesh = new Mesh { name = "SkillArea" };
        _mesh.SetVertices(_vertices);
        _mesh.SetTriangles(_triangles, 0);
        _colors = new Color[_vertices.Count];
        GetComponent<MeshFilter>().sharedMesh = _mesh;

        _color = color;
        _opacity = mine ? 1f : _othersOpacity;
        _elapsed = 0f;
        _lifetime = 0f;
        SetAlpha(1f, 1f);
    }

    /// <summary>
    /// Like Show, but it stays for that many seconds, dimming as they pass, flashes every pulseInterval
    /// (0: never) and fades out at the end.
    /// </summary>
    public void ShowLasting(SkillArea area, Color color, float seconds, float pulseInterval, bool edge = false,
                            bool mine = true)
    {
        Show(area, color, edge, mine);
        _lifetime = Mathf.Max(0.01f, seconds);
        _pulseInterval = pulseInterval;
    }

    /// <summary>
    /// Ends it now, for a lasting shape whose skill went away (a restart, another skill in the slot).
    /// </summary>
    public void Stop()
    {
        Destroy(gameObject);
    }

    private void Update()
    {
        if (_mesh == null)
            return;

        _elapsed += Time.deltaTime;

        if (_lifetime > 0f)
        {
            UpdateLasting();
            return;
        }

        float progress = _elapsed / _fadeDuration;
        if (progress >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // A flash when it appears, then a steady fade.
        SetAlpha(1f - progress, 1f - progress + Flash(_elapsed));
    }

    // Steady opacity that drops towards _lastingEndFade, a flash at each hit and the usual fade after it.
    private void UpdateLasting()
    {
        if (_elapsed >= _lifetime + _fadeDuration)
        {
            Destroy(gameObject);
            return;
        }

        if (_elapsed >= _lifetime)
        {
            float last = _lastingEndFade * (1f - (_elapsed - _lifetime) / _fadeDuration);
            SetAlpha(last, last);
            return;
        }

        float fade = Mathf.Lerp(1f, _lastingEndFade, _elapsed / _lifetime);

        // The first hit is at once, then one every interval, the same rhythm as the host's.
        float flash = _pulseInterval > 0f ? Flash(_elapsed % _pulseInterval) : Flash(_elapsed);
        SetAlpha(fade, fade + flash);
    }

    // Extra fill opacity, in shares of the start opacity, that far after a flash began.
    private float Flash(float sinceFlash)
    {
        if (sinceFlash >= _pulseDuration)
            return 0f;

        return _pulseAlpha / Mathf.Max(0.01f, _startAlpha) * (1f - sinceFlash / _pulseDuration);
    }

    // The mesh was made here, so it's destroyed here: Unity doesn't free meshes made in code.
    private void OnDestroy()
    {
        if (_mesh != null)
            Destroy(_mesh);
    }

    // edgeFade follows the plain fade; fillFade also carries the flash.
    private void SetAlpha(float edgeFade, float fillFade)
    {
        Color fill = _color;
        fill.a = Mathf.Clamp01(_startAlpha * _color.a * fillFade * _opacity);

        Color edge = Color.Lerp(_color, Color.white, _edgeWhiten);
        edge.a = Mathf.Clamp01(_edgeAlpha * _color.a * edgeFade * _opacity);

        for (int i = 0; i < _colors.Length; i++)
            _colors[i] = _edge[i] > 0f ? edge : fill;

        // Sprite shaders multiply by the vertex colour, so this tints and fades the whole shape.
        _mesh.colors = _colors;
    }

    // Vertices are relative to the object, which sits at area.Origin.
    private void BuildFill(SkillArea area)
    {
        switch (area.Shape)
        {
            case SkillShape.Line:
            case SkillShape.Projectile:
            {
                Vector2 side = Side(area) * area.HalfWidth;
                Vector2 along = area.Direction * area.Length;
                AddQuad(-side, side, along + side, along - side, 0f);
                break;
            }

            case SkillShape.Cone:
                AddFan(Vector2.zero, area.Direction, area.Length, area.HalfAngle);
                break;

            default:
                AddFan(area.Point - area.Origin, Vector2.right, area.Radius, Mathf.PI);
                break;
        }
    }

    // The edge stays inside the shape, so it never shows more ground than the skill covers.
    private void BuildEdge(SkillArea area)
    {
        float width = _edgeWidth;

        switch (area.Shape)
        {
            case SkillShape.Line:
            case SkillShape.Projectile:
            {
                Vector2 side = Side(area);
                Vector2 along = area.Direction * area.Length;
                float half = area.HalfWidth;
                float w = Mathf.Min(width, half);
                Vector2 a = -side * half, b = side * half, c = along + side * half, d = along - side * half;

                AddBand(a, b, area.Direction, w);
                AddBand(d, c, -area.Direction, w);
                AddBand(a, d, side, w);
                AddBand(b, c, -side, w);
                break;
            }

            case SkillShape.Cone:
            {
                float w = Mathf.Min(width, area.Length);
                AddArcBand(Vector2.zero, area.Direction, area.Length, area.HalfAngle, w);

                Vector2 left = Rotate(area.Direction, area.HalfAngle) * area.Length;
                Vector2 right = Rotate(area.Direction, -area.HalfAngle) * area.Length;
                AddBand(Vector2.zero, left, Rotate(area.Direction, area.HalfAngle - Mathf.PI * 0.5f), w);
                AddBand(Vector2.zero, right, Rotate(area.Direction, -area.HalfAngle + Mathf.PI * 0.5f), w);
                break;
            }

            default:
                AddArcBand(area.Point - area.Origin, Vector2.right, area.Radius, Mathf.PI, Mathf.Min(width, area.Radius));
                break;
        }
    }

    // A fan: its centre and points on the arc between -halfAngle and +halfAngle around the direction.
    // A circle is a fan of half-angle π.
    private void AddFan(Vector2 center, Vector2 direction, float radius, float halfAngle)
    {
        int segments = Segments(halfAngle);
        int first = _vertices.Count;
        AddVertex(center, 0f);

        float start = Mathf.Atan2(direction.y, direction.x) - halfAngle;
        for (int i = 0; i <= segments; i++)
        {
            float angle = start + 2f * halfAngle * i / segments;
            AddVertex(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, 0f);
        }

        for (int i = 0; i < segments; i++)
        {
            _triangles.Add(first);
            _triangles.Add(first + i + 2);
            _triangles.Add(first + i + 1);
        }
    }

    // The outer band of a fan, width units wide, along its arc.
    private void AddArcBand(Vector2 center, Vector2 direction, float radius, float halfAngle, float width)
    {
        int segments = Segments(halfAngle);
        float start = Mathf.Atan2(direction.y, direction.x) - halfAngle;
        float inner = Mathf.Max(0f, radius - width);

        for (int i = 0; i < segments; i++)
        {
            Vector2 from = new(Mathf.Cos(start + 2f * halfAngle * i / segments), Mathf.Sin(start + 2f * halfAngle * i / segments));
            Vector2 to = new(Mathf.Cos(start + 2f * halfAngle * (i + 1) / segments), Mathf.Sin(start + 2f * halfAngle * (i + 1) / segments));
            AddQuad(center + from * inner, center + from * radius, center + to * radius, center + to * inner, 1f);
        }
    }

    // A strip from a to b, width units wide towards inward.
    private void AddBand(Vector2 a, Vector2 b, Vector2 inward, float width)
    {
        Vector2 offset = inward.normalized * width;
        AddQuad(a, a + offset, b + offset, b, 1f);
    }

    // Two triangles; the winding doesn't matter for these sprite shaders, which draw both faces.
    private void AddQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float edge)
    {
        int first = _vertices.Count;
        AddVertex(a, edge);
        AddVertex(b, edge);
        AddVertex(c, edge);
        AddVertex(d, edge);

        _triangles.Add(first);
        _triangles.Add(first + 1);
        _triangles.Add(first + 2);
        _triangles.Add(first);
        _triangles.Add(first + 2);
        _triangles.Add(first + 3);
    }

    private void AddVertex(Vector2 position, float edge)
    {
        _vertices.Add(position);
        _edge.Add(edge);
    }

    private static int Segments(float halfAngle)
    {
        return Mathf.Max(2, Mathf.CeilToInt(CircleSegments * halfAngle / Mathf.PI));
    }

    private static Vector2 Side(SkillArea area)
    {
        return new Vector2(-area.Direction.y, area.Direction.x);
    }

    private static Vector2 Rotate(Vector2 vector, float radians)
    {
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
    }
}
