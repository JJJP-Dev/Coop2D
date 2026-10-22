using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The window that says the match is over (#52), on every client. It only shows what MatchFlow
// received from the host: how long it lasted and how many waves started. The host also gets the
// button that restarts the match for everyone; the others are told they're waiting for it.
public class GameOverPanel : MonoBehaviour
{
    [Tooltip("What shows and hides. A child of this object, so this one keeps checking.")]
    [SerializeField] private GameObject _window;

    [Tooltip("Waves and time of the match that just ended.")]
    [SerializeField] private TMP_Text _statsLabel;

    [Tooltip("Only the host sees it.")]
    [SerializeField] private Button _restartButton;

    [Tooltip("What the other players see instead of the button.")]
    [SerializeField] private GameObject _waitingLabel;

    private MatchPhase? _shownPhase;

    private void Awake()
    {
        if (_window == null || _statsLabel == null || _restartButton == null || _waitingLabel == null)
        {
            Debug.LogError("[GameOverPanel] Window, stats label, restart button or waiting label is not assigned.", this);
            enabled = false;
            return;
        }

        _window.SetActive(false);
        _restartButton.onClick.AddListener(OnRestartClicked);
    }

    // Polled: it's one value, and it also catches a match that was already over when this client arrived.
    private void Update()
    {
        MatchFlow flow = MatchFlow.Current;
        MatchPhase phase = flow != null ? flow.Phase : MatchPhase.Playing;

        if (_shownPhase == phase)
            return;

        _shownPhase = phase;
        bool over = phase == MatchPhase.Over;
        _window.SetActive(over);

        if (!over)
            return;

        _statsLabel.text = $"Oleadas: {flow.EndWaves} · {FormatTime(flow.EndSeconds)}";

        // The host runs the server in this same window, so it can restart without asking anyone.
        bool isHost = flow.IsServerInitialized;
        _restartButton.gameObject.SetActive(isHost);
        _waitingLabel.SetActive(!isHost);
    }

    private void OnRestartClicked()
    {
        if (MatchFlow.Current != null)
            MatchFlow.Current.RestartMatch();
    }

    private static string FormatTime(int seconds)
    {
        return $"{seconds / 60}:{seconds % 60:00}";
    }
}
