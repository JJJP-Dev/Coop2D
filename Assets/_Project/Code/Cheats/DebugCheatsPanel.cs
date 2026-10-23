using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The cheats window, opened with F1. It only exists in development builds,
// and every button asks DebugCheats, which asks the host.
public class DebugCheatsPanel : MonoBehaviour
{
    [SerializeField] private DebugCheats _cheats;

    [Tooltip("What F1 shows and hides. A child of this object, so this one keeps listening.")]
    [SerializeField] private GameObject _window;

    [Tooltip("Hidden, and copied right after itself once per enemy and amount in the cheats settings.")]
    [SerializeField] private Button _enemyButtonTemplate;

    [SerializeField] private Button _killAllButton;
    [SerializeField] private Button _nextWaveButton;
    [SerializeField] private Button _restartWavesButton;

    [Tooltip("Its text says whether your player is invulnerable.")]
    [SerializeField] private Button _invulnerableButton;

    [SerializeField] private Button _damageAllButton;
    [SerializeField] private Button _healAllButton;

    [Tooltip("Optional. Gives the group experience (#76).")]
    [SerializeField] private Button _giveXpButton;

    [Tooltip("Optional. Gives every player a reroll, within the limit of 2 per match (#78).")]
    [SerializeField] private Button _giveRerollButton;

    [Tooltip("Optional. Shows the match's seed (#75), to write it in RunRandomSettings and play the same run again.")]
    [SerializeField] private TMP_Text _seedLabel;

    [Tooltip("Optional. Shows the group's revives left out of its total (#79).")]
    [SerializeField] private TMP_Text _reviveLabel;

    private InputService _inputService;
    private TMP_Text _invulnerableLabel;
    private bool? _shownInvulnerable;
    private string _shownSeed;
    private int _shownRemaining = -1;
    private int _shownTotal = -1;

    private void Awake()
    {
        // In a release build, nothing of it is left.
        if (!Debug.isDebugBuild)
        {
            Destroy(gameObject);
            return;
        }

        if (_cheats == null || _cheats.Settings == null || _window == null || _enemyButtonTemplate == null ||
            _killAllButton == null || _nextWaveButton == null || _restartWavesButton == null || _invulnerableButton == null ||
            _damageAllButton == null || _healAllButton == null)
        {
            Debug.LogError("[DebugCheatsPanel] Cheats, their settings, the window or a button are not assigned.", this);
            enabled = false;
            return;
        }

        _window.SetActive(false);
        CreateEnemyButtons();

        _killAllButton.onClick.AddListener(_cheats.KillAllEnemies);
        _nextWaveButton.onClick.AddListener(_cheats.SkipToNextWave);
        _restartWavesButton.onClick.AddListener(_cheats.RestartWaves);
        _invulnerableButton.onClick.AddListener(_cheats.ToggleInvulnerable);
        _damageAllButton.onClick.AddListener(_cheats.DamageAll);
        _healAllButton.onClick.AddListener(_cheats.HealAll);

        if (_giveXpButton != null)
            _giveXpButton.onClick.AddListener(_cheats.GiveXp);

        if (_giveRerollButton != null)
            _giveRerollButton.onClick.AddListener(_cheats.GiveReroll);

        _invulnerableLabel = _invulnerableButton.GetComponentInChildren<TMP_Text>();
    }

    private void OnEnable()
    {
        // OnEnable still runs after the Destroy in Awake.
        if (!Debug.isDebugBuild || GameServices.Instance == null)
            return;

        _inputService = GameServices.Instance.InputService;
        _inputService.ToggleCheats.performed += OnToggle;
    }

    private void OnDisable()
    {
        if (_inputService != null)
            _inputService.ToggleCheats.performed -= OnToggle;

        _inputService = null;
    }

    // It shows what the host sent back, so the text changes once the host has done it.
    private void Update()
    {
        if (!_window.activeSelf || _inputService == null)
            return;

        UpdateSeedLabel();
        UpdateReviveLabel();

        PlayerIdentity player = GameServices.Instance.LocalPlayerService.Current;
        bool invulnerable = player != null && player.TryGetComponent(out Health health) && health.IsInvulnerable;

        if (_shownInvulnerable == invulnerable)
            return;

        _shownInvulnerable = invulnerable;
        _invulnerableLabel.text = invulnerable ? "Invulnerable: sí" : "Invulnerable: no";
    }

    // Only when it changes, so the text isn't rebuilt every frame.
    private void UpdateSeedLabel()
    {
        if (_seedLabel == null)
            return;

        string seed = MatchSeed.Current != null ? MatchSeed.Current.SeedText : "-";
        if (seed == _shownSeed)
            return;

        _shownSeed = seed;
        _seedLabel.text = $"Semilla: {seed}";
    }

    private void UpdateReviveLabel()
    {
        if (_reviveLabel == null)
            return;

        ReviveCharges charges = ReviveCharges.Current;
        int remaining = charges != null ? charges.Remaining : -1;
        int total = charges != null ? charges.Total : -1;

        if (remaining == _shownRemaining && total == _shownTotal)
            return;

        _shownRemaining = remaining;
        _shownTotal = total;
        _reviveLabel.text = charges != null ? $"Reanimaciones: {remaining}/{total}" : "Reanimaciones: sin límite";
    }

    private void OnToggle(InputAction.CallbackContext context)
    {
        _window.SetActive(!_window.activeSelf);
    }

    private void CreateEnemyButtons()
    {
        _enemyButtonTemplate.gameObject.SetActive(false);

        Transform template = _enemyButtonTemplate.transform;
        IReadOnlyList<Enemy> enemies = _cheats.Settings.Enemies;
        IReadOnlyList<int> amounts = _cheats.Settings.SpawnAmounts;
        int created = 0;

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] == null)
                continue;

            for (int j = 0; j < amounts.Count; j++)
            {
                Button button = Instantiate(_enemyButtonTemplate, template.parent);
                button.transform.SetSiblingIndex(template.GetSiblingIndex() + 1 + created);
                button.gameObject.SetActive(true);

                string amount = amounts[j] > 1 ? $"{amounts[j]} " : "";
                button.GetComponentInChildren<TMP_Text>().text = $"Generar {amount}{enemies[i].name}";

                // Each button needs its own copies: the lambda would otherwise see i and j after the loops.
                int enemyIndex = i;
                int amountIndex = j;
                button.onClick.AddListener(() => _cheats.SpawnEnemy(enemyIndex, amountIndex));
                created++;
            }
        }
    }
}
