using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Scales a canvas by the same whole number as the game's pixels, so one unit of UI is one pixel
// of the game: a 16x16 icon drawn by us looks exactly like a 16x16 sprite in the arena, at any
// window size. It takes the reference resolution from the scene's Pixel Perfect Camera, so the
// UI follows it when the base resolution changes (#42).
// It also runs outside Play mode, so the UI is edited at the size it will have in the Game view.
[ExecuteAlways]
[RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
public class PixelCanvasScaler : MonoBehaviour
{
    [Tooltip("Only for scenes without a Pixel Perfect Camera on the main camera. Keep it equal to the camera's.")]
    [SerializeField] private Vector2Int _fallbackReferenceResolution = new(480, 270);

    private Canvas _canvas;
    private CanvasScaler _scaler;
    private PixelPerfectCamera _pixelPerfectCamera;
    private Vector2 _lastScreenSize;

    private void OnEnable()
    {
        _canvas = GetComponent<Canvas>();
        _scaler = GetComponent<CanvasScaler>();

        // The scale is set by hand below, which only Constant Pixel Size allows.
        _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        // Forces the next check to apply it.
        _lastScreenSize = Vector2.zero;
        Apply();
    }

    // In Play mode the window only changes with Alt+Enter. Outside it, Update runs when the scene
    // changes, and resizing the Game view resizes the canvas, which calls the method below.
    private void Update()
    {
        Apply();
    }

    private void OnRectTransformDimensionsChange()
    {
        Apply();
    }

    private void Apply()
    {
        if (_canvas == null || _scaler == null)
            return;

        // The canvas's size in real pixels: the window in a build, the Game view in the editor.
        // Screen.width isn't reliable outside Play mode, where it can be the Scene view's.
        Vector2 screenSize = _canvas.pixelRect.size;
        if (screenSize == _lastScreenSize || screenSize.x <= 0f || screenSize.y <= 0f)
            return;

        _lastScreenSize = screenSize;
        _scaler.scaleFactor = GetZoom(screenSize);
    }

    // The same whole number the Pixel Perfect Camera uses: as many times the reference
    // as fit in the window, at least once.
    private int GetZoom(Vector2 screenSize)
    {
        Vector2Int reference = GetReferenceResolution();

        if (reference.x <= 0 || reference.y <= 0)
            return 1;

        int byWidth = Mathf.FloorToInt(screenSize.x / reference.x);
        int byHeight = Mathf.FloorToInt(screenSize.y / reference.y);
        return Mathf.Max(1, Mathf.Min(byWidth, byHeight));
    }

    private Vector2Int GetReferenceResolution()
    {
        if (_pixelPerfectCamera == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                mainCamera.TryGetComponent(out _pixelPerfectCamera);
        }

        return _pixelPerfectCamera != null
            ? new Vector2Int(_pixelPerfectCamera.refResolutionX, _pixelPerfectCamera.refResolutionY)
            : _fallbackReferenceResolution;
    }
}
