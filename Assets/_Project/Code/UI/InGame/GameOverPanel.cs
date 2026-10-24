using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The window that says the match is over, on every client: lost or survived. It only shows what
// MatchFlow received from the host: how long it lasted, how many waves started and the group's level.
// The host also gets the button that restarts the match for everyone; the others are told they're
// waiting for it.
public class GameOverPanel : MonoBehaviour
{
    [Tooltip("What shows and hides. A child of this object, so this one keeps checking.")]
    [SerializeField] private GameObject _window;

    [Tooltip("Optional. Says whether the group lost or survived.")]
    [SerializeField] private TMP_Text _titleLabel;

    [SerializeField] private string _lostTitle = "Habéis caído";
    [SerializeField] private string _wonTitle = "¡Habéis sobrevivido!";

    [Tooltip("Waves, time and level of the match that just ended.")]
    [SerializeField] private TMP_Text _statsLabel;

    [Tooltip("Only the host sees it.")]
    [SerializeField] private Button _restartButton;

    [Tooltip("What the other players see instead of the button.")]
    [SerializeField] private GameObject _waitingLabel;

    private MatchPhase? _shownPhase;
    private bool _shownWon;

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
        bool won = flow != null && flow.Won;

        if (_shownPhase == phase && _shownWon == won)
            return;

        _shownPhase = phase;
        _shownWon = won;
        bool over = phase == MatchPhase.Over;
        _window.SetActive(over);

        if (!over)
            return;

        if (_titleLabel != null)
            _titleLabel.text = won ? _wonTitle : _lostTitle;

        // The group's level when the panel opens. Orbs still on the ground could add one later; it isn't redrawn.
        int level = RunExperience.Current != null ? RunExperience.Current.Level : 0;
        string levelText = level > 0 ? $" · Nivel {level}" : string.Empty;
        _statsLabel.text = $"Oleadas: {flow.EndWaves} · {FormatTime(flow.EndSeconds)}{levelText}";

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
