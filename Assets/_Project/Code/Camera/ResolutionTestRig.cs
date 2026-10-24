using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

// Test bench for choosing the base resolution. In a scene of its own, without network: it fills
// the screen with 4 player stand-ins and 30 wandering enemies, and switches the Pixel Perfect Camera
// between 480×270 and 640×360 with keys, so both can be compared at 1080p and 1440p. Another key
// draws the stand-ins bigger on the same 16 px grid, to judge larger characters before drawing them.
// Only for that decision: no game scene uses it, and the randomness is UnityEngine.Random on purpose.
public class ResolutionTestRig : MonoBehaviour
{
    private static readonly Vector2Int SmallResolution = new(480, 270);
    private static readonly Vector2Int LargeResolution = new(640, 360);

    // ×1.5 is only a preview: scaled pixel art shows uneven pixels, which drawing it at that size won't have.
    private static readonly float[] SpriteScales = { 1f, 1.5f, 2f };

    [SerializeField] private PixelPerfectCamera _camera;

    [Tooltip("A player's sprite, as it will look (16×24 in a 32×32 canvas).")]
    [SerializeField] private Sprite _playerSprite;

    [SerializeField] private Sprite _enemySprite;

    [SerializeField, Min(1)] private int _players = 4;
    [SerializeField, Min(0)] private int _enemies = 30;

    [Tooltip("Radius, in units, of the area the enemies wander around the center.")]
    [SerializeField, Min(1f)] private float _areaRadius = 9f;

    [Tooltip("Units per second, like a chaser.")]
    [SerializeField, Min(0f)] private float _enemySpeed = 2.5f;

    private readonly List<Transform> _standIns = new();
    private readonly List<Transform> _wanderers = new();
    private readonly List<Vector2> _targets = new();
    private bool _paused;
    private int _scaleIndex;

    private void Start()
    {
        if (_camera == null || _playerSprite == null || _enemySprite == null)
        {
            Debug.LogError("[ResolutionTestRig] Camera, player sprite or enemy sprite is not assigned.", this);
            enabled = false;
            return;
        }

        // The players stand together in the middle, as a group does in a fight.
        for (int i = 0; i < _players; i++)
        {
            Vector2 position = new((i - (_players - 1) * 0.5f) * 1.5f, 0f);
            Create($"Player {i + 1}", _playerSprite, position, 1);
        }

        for (int i = 0; i < _enemies; i++)
        {
            Transform enemy = Create($"Enemy {i + 1}", _enemySprite, RandomPoint(), 0);
            _wanderers.Add(enemy);
            _targets.Add(RandomPoint());
        }
    }

    private void Update()
    {
        ReadKeys();

        if (_paused)
            return;

        for (int i = 0; i < _wanderers.Count; i++)
        {
            Vector2 position = _wanderers[i].position;
            Vector2 next = Vector2.MoveTowards(position, _targets[i], _enemySpeed * Time.deltaTime);
            _wanderers[i].position = next;

            if ((next - _targets[i]).sqrMagnitude < 0.01f)
                _targets[i] = RandomPoint();
        }
    }

    private void ReadKeys()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.digit1Key.wasPressedThisFrame)
            SetResolution(SmallResolution);

        if (keyboard.digit2Key.wasPressedThisFrame)
            SetResolution(LargeResolution);

        // None shows more of the arena on screens that aren't an exact multiple; Windowbox shows
        // exactly the reference everywhere, with black bands where it doesn't fit.
        if (keyboard.cKey.wasPressedThisFrame)
            _camera.cropFrame = _camera.cropFrame == PixelPerfectCamera.CropFrame.None
                ? PixelPerfectCamera.CropFrame.Windowbox
                : PixelPerfectCamera.CropFrame.None;

        if (keyboard.sKey.wasPressedThisFrame)
            SetSpriteScale((_scaleIndex + 1) % SpriteScales.Length);

        if (keyboard.spaceKey.wasPressedThisFrame)
            _paused = !_paused;
    }

    private void SetSpriteScale(int index)
    {
        _scaleIndex = index;
        Vector3 scale = Vector3.one * SpriteScales[index];

        foreach (Transform standIn in _standIns)
            standIn.localScale = scale;
    }

    private void SetResolution(Vector2Int resolution)
    {
        _camera.refResolutionX = resolution.x;
        _camera.refResolutionY = resolution.y;
    }

    // A quick text in the corner: a test tool doesn't need a canvas.
    private void OnGUI()
    {
        if (_camera == null)
            return;

        int zoom = Mathf.Max(1, _camera.pixelRatio);
        float visibleX = Screen.width / (float)zoom;
        float visibleY = Screen.height / (float)zoom;
        bool cropped = _camera.cropFrame != PixelPerfectCamera.CropFrame.None;

        if (cropped)
        {
            visibleX = Mathf.Min(visibleX, _camera.refResolutionX);
            visibleY = Mathf.Min(visibleY, _camera.refResolutionY);
        }

        string text =
            $"Base {_camera.refResolutionX}×{_camera.refResolutionY} · pantalla {Screen.width}×{Screen.height} · ×{zoom}\n" +
            $"Se ven {visibleX:0}×{visibleY:0} px ({visibleX / 16f:0.0}×{visibleY / 16f:0.0} casillas) · " +
            $"recorte {(cropped ? "sí" : "no")} · sprites ×{SpriteScales[_scaleIndex]}\n" +
            "1: 480×270 · 2: 640×360 · C: recorte · S: tamaño de sprites · Espacio: pausa";

        GUI.Label(new Rect(8, 8, 1000, 60), text);
    }

    private Transform Create(string objectName, Sprite sprite, Vector2 position, int sortingOrder)
    {
        GameObject created = new(objectName);
        created.transform.SetParent(transform, false);
        created.transform.position = position;

        SpriteRenderer renderer = created.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        _standIns.Add(created.transform);
        return created.transform;
    }

    private Vector2 RandomPoint()
    {
        return Random.insideUnitCircle * _areaRadius;
    }
}
