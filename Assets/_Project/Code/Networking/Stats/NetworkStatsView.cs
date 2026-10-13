using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkStatsView : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private TMP_Text _text;
    [SerializeField, Min(0.1f)] private float _refreshInterval = 0.5f;

    private ConnectionService _connectionService;
    private LocalPlayerService _localPlayerService;
    private InputService _inputService;

    private bool _visible = true;
    private float _elapsed;
    private int _frames;

    private void Start()
    {
        GameServices services = GameServices.Instance;

        _connectionService = services.ConnectionService;
        _localPlayerService = services.LocalPlayerService;
        _inputService = services.InputService;

        _inputService.ToggleNetworkStats.performed += OnToggle;
        _panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_inputService != null)
            _inputService.ToggleNetworkStats.performed -= OnToggle;
    }

    private void Update()
    {
        // FPS is averaged over the whole interval, so it counts every frame.
        _frames++;
        _elapsed += Time.unscaledDeltaTime;

        if (_elapsed < _refreshInterval)
            return;

        float fps = _frames / _elapsed;
        _frames = 0;
        _elapsed = 0f;

        Refresh(fps);
    }

    private void OnToggle(InputAction.CallbackContext context)
    {
        _visible = !_visible;
    }

    private void Refresh(float fps)
    {
        bool show = _visible && _connectionService.IsConnected;
        _panel.SetActive(show);

        if (!show)
            return;

        NetworkStats stats = _connectionService.GetStats();
        string role = stats.IsHost ? "Host" : "Cliente";

        PlayerIdentity localPlayer = _localPlayerService.Current;
        string player = localPlayer != null ? $" · Jugador {localPlayer.Slot + 1}" : "";

        _text.text =
            $"{role}{player}\n" +
            $"Ping: {stats.PingMs} ms\n" +
            $"Jugadores: {stats.PlayerCount}/{PlayerSlots.MaxPlayers}\n" +
            $"FPS: {Mathf.RoundToInt(fps)}";
    }
}
