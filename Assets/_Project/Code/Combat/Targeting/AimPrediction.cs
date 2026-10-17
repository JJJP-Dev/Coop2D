using UnityEngine;

public static class AimPrediction
{
    /// <summary>
    /// Where a straight shot meets a target that keeps its current velocity.
    /// Returns the target's position when the shot can't reach it within maxTime.
    /// </summary>
    public static Vector2 GetInterceptPoint(Vector2 origin, Vector2 targetPosition, Vector2 targetVelocity,
                                            float projectileSpeed, float maxTime)
    {
        if (!TryGetInterceptTime(targetPosition - origin, targetVelocity, projectileSpeed, out float time) || time > maxTime)
            return targetPosition;

        return targetPosition + targetVelocity * time;
    }

    // Solves |toTarget + targetVelocity * t| = projectileSpeed * t for the first t > 0.
    private static bool TryGetInterceptTime(Vector2 toTarget, Vector2 targetVelocity, float projectileSpeed, out float time)
    {
        float a = Vector2.Dot(targetVelocity, targetVelocity) - projectileSpeed * projectileSpeed;
        float b = 2f * Vector2.Dot(toTarget, targetVelocity);
        float c = Vector2.Dot(toTarget, toTarget);

        // Target exactly as fast as the shot: the equation is no longer quadratic.
        if (Mathf.Abs(a) < 0.0001f)
        {
            time = Mathf.Abs(b) > 0.0001f ? -c / b : -1f;
            return time > 0f;
        }

        float discriminant = b * b - 4f * a * c;
        if (discriminant < 0f)
        {
            time = -1f;
            return false;
        }

        float root = Mathf.Sqrt(discriminant);
        float first = (-b - root) / (2f * a);
        float second = (-b + root) / (2f * a);

        if (first > 0f && second > 0f)
            time = Mathf.Min(first, second);
        else
            time = Mathf.Max(first, second);

        return time > 0f;
    }
}
