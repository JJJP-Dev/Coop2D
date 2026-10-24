using UnityEngine;

// Placeholder picture of where a skill hit: its circle, line or cone, filled with the element's
// colour, that fades out. It builds a small mesh from the cast's SkillArea, so one prefab draws every
// shape at its exact size. Only the picture: hits are decided by SkillHitResolver.
// The real effects replace it with the skills' art.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SkillShapeVisual : MonoBehaviour
{
    // Segments of a full circle; a cone uses its share of them.
    private const int CircleSegments = 40;

    [Tooltip("Seconds it takes to fade out.")]
    [SerializeField, Min(0.05f)] private float _fadeDuration = 0.3f;

    [Tooltip("Opacity when it appears, so what is under it stays visible.")]
    [SerializeField, Range(0f, 1f)] private float _startAlpha = 0.5f;

    private Mesh _mesh;
    private Color[] _colors;
    private Color _color;
    private float _elapsed;

    /// <summary>
    /// Draws that area in that colour. The object can be anywhere: the mesh is placed around the area.
    /// </summary>
    public void Show(SkillArea area, Color color)
    {
        transform.SetPositionAndRotation(area.Origin, Quaternion.identity);
        transform.localScale = Vector3.one;

        _mesh = new Mesh { name = "SkillArea" };
        Vector3[] vertices = area.Shape switch
        {
            SkillShape.Line => BuildLine(area),
            SkillShape.Cone => BuildFan(area.Origin, area.Direction, area.Length, area.HalfAngle, area.Origin),
            _ => BuildFan(area.Point, Vector2.right, area.Radius, Mathf.PI, area.Origin),
        };

        _mesh.vertices = vertices;
        _mesh.triangles = area.Shape == SkillShape.Line ? new[] { 0, 1, 2, 0, 2, 3 } : FanTriangles(vertices.Length);
        _colors = new Color[vertices.Length];
        GetComponent<MeshFilter>().sharedMesh = _mesh;

        _color = color;
        _elapsed = 0f;
        SetAlpha(1f);
    }

    private void Update()
    {
        if (_mesh == null)
            return;

        _elapsed += Time.deltaTime;
        float progress = _elapsed / _fadeDuration;
        if (progress >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        SetAlpha(1f - progress);
    }

    // The mesh was made here, so it's destroyed here: Unity doesn't free meshes made in code.
    private void OnDestroy()
    {
        if (_mesh != null)
            Destroy(_mesh);
    }

    private void SetAlpha(float fade)
    {
        Color color = _color;
        color.a = _startAlpha * _color.a * fade;

        for (int i = 0; i < _colors.Length; i++)
            _colors[i] = color;

        // Sprite shaders multiply by the vertex colour, so this tints and fades the whole shape.
        _mesh.colors = _colors;
    }

    // A rectangle from the origin along the direction. Vertices relative to the object, at the origin.
    private static Vector3[] BuildLine(SkillArea area)
    {
        Vector2 side = new Vector2(-area.Direction.y, area.Direction.x) * area.HalfWidth;
        Vector2 along = area.Direction * area.Length;
        return new Vector3[] { -side, side, along + side, along - side };
    }

    // A fan: its centre and points on the arc between -halfAngle and +halfAngle around the direction.
    // A circle is a fan of half-angle π. Vertices relative to the object, which sits at objectOrigin.
    private static Vector3[] BuildFan(Vector2 center, Vector2 direction, float radius, float halfAngle,
                                      Vector2 objectOrigin)
    {
        int segments = Mathf.Max(2, Mathf.CeilToInt(CircleSegments * halfAngle / Mathf.PI));
        var vertices = new Vector3[segments + 2];
        Vector2 local = center - objectOrigin;
        vertices[0] = local;

        float start = Mathf.Atan2(direction.y, direction.x) - halfAngle;
        for (int i = 0; i <= segments; i++)
        {
            float angle = start + 2f * halfAngle * i / segments;
            vertices[i + 1] = local + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        return vertices;
    }

    private static int[] FanTriangles(int vertexCount)
    {
        int segments = vertexCount - 2;
        var triangles = new int[segments * 3];
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 2;
            triangles[i * 3 + 2] = i + 1;
        }

        return triangles;
    }
}
