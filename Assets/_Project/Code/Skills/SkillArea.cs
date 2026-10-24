using UnityEngine;

// The ground one cast of a skill covers: a circle, a line or a cone, already placed.
// The owner builds it to find its targets and draw the cast; the host builds the same one from the
// origin and aim point the owner sent, to check those targets. Both use the same maths, so a target the
// owner saw inside is inside for the host too, give or take how far it moved on the way (TargetTolerance).
public readonly struct SkillArea
{
    public readonly SkillShape Shape;

    // Where the caster stood. Lines and cones start here.
    public readonly Vector2 Origin;

    // The circle's centre, or the far end of a line or cone.
    public readonly Vector2 Point;

    // From the origin towards the aim point, normalised.
    public readonly Vector2 Direction;

    public readonly float Radius;
    public readonly float Length;
    public readonly float HalfWidth;

    // In radians, from the centre line to each edge of a cone.
    public readonly float HalfAngle;

    private SkillArea(SkillShape shape, Vector2 origin, Vector2 point, Vector2 direction, float radius, float length,
                      float halfWidth, float halfAngle)
    {
        Shape = shape;
        Origin = origin;
        Point = point;
        Direction = direction;
        Radius = radius;
        Length = length;
        HalfWidth = halfWidth;
        HalfAngle = halfAngle;
    }

    /// <summary>
    /// The area of that skill cast from origin towards aim. A circle lands on aim (the caller clamps it to
    /// the cast range); a line or a cone only takes the direction of aim and always has its full length.
    /// With the skill's level, so a longer Chispa is checked as long as it is drawn.
    /// </summary>
    public static SkillArea For(SkillInstance skill, Vector2 origin, Vector2 aim)
    {
        Vector2 toAim = aim - origin;
        Vector2 direction = toAim.sqrMagnitude > 0.0001f ? toAim.normalized : Vector2.right;

        if (skill.Shape == SkillShape.Circle)
            return new SkillArea(SkillShape.Circle, origin, aim, direction, skill.Radius, 0f, 0f, 0f);

        // A projectile's area is its path: a band as wide as the projectile, as long as it flies.
        float halfWidth = skill.Shape == SkillShape.Projectile ? skill.ProjectileRadius : skill.Width * 0.5f;
        Vector2 end = origin + direction * skill.Length;
        return new SkillArea(skill.Shape, origin, end, direction, 0f, skill.Length, halfWidth,
                             skill.Angle * 0.5f * Mathf.Deg2Rad);
    }

    /// <summary>
    /// A circle of that radius around center, for where a projectile bursts.
    /// </summary>
    public static SkillArea Circle(Vector2 center, float radius)
    {
        return new SkillArea(SkillShape.Circle, center, center, Vector2.right, radius, 0f, 0f, 0f);
    }

    /// <summary>
    /// A circle that holds the whole area, for a first cheap physics search.
    /// </summary>
    public Vector2 BoundsCenter => Shape switch
    {
        SkillShape.Line or SkillShape.Projectile => Origin + Direction * (Length * 0.5f),
        SkillShape.Cone => Origin,
        _ => Point,
    };

    public float BoundsRadius => Shape switch
    {
        SkillShape.Line or SkillShape.Projectile => Mathf.Sqrt(Length * Length * 0.25f + HalfWidth * HalfWidth),
        SkillShape.Cone => Length,
        _ => Radius,
    };

    /// <summary>
    /// The point of the area closest to p: p itself when it's inside.
    /// </summary>
    public Vector2 ClosestPoint(Vector2 p)
    {
        switch (Shape)
        {
            case SkillShape.Line:
            case SkillShape.Projectile:
            {
                Vector2 side = new Vector2(-Direction.y, Direction.x);
                Vector2 local = p - Origin;
                float along = Mathf.Clamp(Vector2.Dot(local, Direction), 0f, Length);
                float across = Mathf.Clamp(Vector2.Dot(local, side), -HalfWidth, HalfWidth);
                return Origin + Direction * along + side * across;
            }

            case SkillShape.Cone:
            {
                Vector2 local = p - Origin;
                float distance = local.magnitude;
                if (distance < 0.0001f)
                    return Origin;

                float angle = Mathf.Acos(Mathf.Clamp(Vector2.Dot(local / distance, Direction), -1f, 1f));
                if (angle <= HalfAngle)
                    return Origin + local / distance * Mathf.Min(distance, Length);

                // Outside the wedge's angle: the nearest point is on the nearer edge. True for any cone of
                // 180° or less, which is every cone a skill can have.
                float sign = Direction.x * local.y - Direction.y * local.x >= 0f ? 1f : -1f;
                Vector2 edge = Rotate(Direction, sign * HalfAngle);
                return Origin + edge * Mathf.Clamp(Vector2.Dot(local, edge), 0f, Length);
            }

            default:
                return Point + Vector2.ClampMagnitude(p - Point, Radius);
        }
    }

    /// <summary>
    /// How far p is outside the area, in units. 0 when it's inside.
    /// </summary>
    public float DistanceOutside(Vector2 p)
    {
        return Vector2.Distance(p, ClosestPoint(p));
    }

    /// <summary>
    /// How far the edge of that body is from the area: 0 when they touch, so a big enemy counts as soon as
    /// any part of it is inside, as with the circle before.
    /// </summary>
    public float DistanceOutside(Collider2D body)
    {
        // The area's point nearest the body's centre, then the body's point nearest that one.
        Vector2 nearArea = ClosestPoint(body.bounds.center);
        return DistanceOutside(body.ClosestPoint(nearArea));
    }

    private static Vector2 Rotate(Vector2 vector, float radians)
    {
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
    }
}
