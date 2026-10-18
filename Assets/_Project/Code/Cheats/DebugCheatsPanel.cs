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

    [Tooltip("Hidden, and copied once per enemy in the cheats settings, right after itself.")]
    [SerializeField] private Button _enemyButtonTemplate;

    [SerializeField] private Button _killAllButton;
    [SerializeField] private Button _nextWaveButton;

    private InputService _inputService;

    private void Awake()
    {
        // In a release build, nothing of it is left.
        if (!Debug.isDebugBuild)
        {
            Destroy(gameObject);
            return;
        }

        if (_cheats == null || _cheats.Settings == null || _window == null || _enemyButtonTemplate == null ||
            _killAllButton == null || _nextWaveButton == null)
        {
            Debug.LogError("[DebugCheatsPanel] Cheats, their settings, the window or a button are not assigned.", this);
            enabled = false;
            return;
        }

        _window.SetActive(false);
        CreateEnemyButtons();

        _killAllButton.onClick.AddListener(_cheats.KillAllEnemies);
        _nextWaveButton.onClick.AddListener(_cheats.SkipToNextWave);
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

    private void OnToggle(InputAction.CallbackContext context)
    {
        _window.SetActive(!_window.activeSelf);
    }

    private void CreateEnemyButtons()
    {
        _enemyButtonTemplate.gameObject.SetActive(false);

        Transform template = _enemyButtonTemplate.transform;
        IReadOnlyList<Enemy> enemies = _cheats.Settings.Enemies;
        int created = 0;

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] == null)
                continue;

            Button button = Instantiate(_enemyButtonTemplate, template.parent);
            button.transform.SetSiblingIndex(template.GetSiblingIndex() + 1 + created);
            button.gameObject.SetActive(true);
            button.GetComponentInChildren<TMP_Text>().text = $"Generar {enemies[i].name}";

            // Each button needs its own copy: the lambda would otherwise see i after the loop.
            int index = i;
            button.onClick.AddListener(() => _cheats.SpawnEnemy(index));
            created++;
        }
    }
}
