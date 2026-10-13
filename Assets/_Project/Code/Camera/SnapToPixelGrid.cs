using UnityEngine;
using UnityEngine.Rendering.Universal;

// Keeps a child that is not a sprite, like a text, on the Pixel Perfect Camera's
// grid every frame, so it doesn't shimmer while its parent moves.
public class SnapToPixelGrid : MonoBehaviour
{
    private Vector3 _localOffset;
    private PixelPerfectCamera _pixelPerfectCamera;

    private void Awake()
    {
        _localOffset = transform.localPosition;
    }

    private void LateUpdate()
    {
        if (transform.parent == null || !TryFindPixelPerfectCamera())
            return;

        // Parent and offset are rounded separately, so the offset is the same every
        // frame, like a sprite child: Unity snaps sprites to this same grid when drawing.
        Vector3 parentPosition = _pixelPerfectCamera.RoundToPixel(transform.parent.position);
        Vector3 offset = _pixelPerfectCamera.RoundToPixel(transform.parent.TransformVector(_localOffset));
        transform.position = parentPosition + offset;
    }

    private bool TryFindPixelPerfectCamera()
    {
        if (_pixelPerfectCamera != null)
            return true;

        Camera mainCamera = Camera.main;
        return mainCamera != null && mainCamera.TryGetComponent(out _pixelPerfectCamera);
    }
}
