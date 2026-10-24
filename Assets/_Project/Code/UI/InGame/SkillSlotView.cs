using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// One skill of the local player in the HUD, with its key and how long until it's ready.
// It reads the owner's own cooldown, the same one that lets the cast through, so what it shows
// matches what happens when you press the key, on the host and on a client alike.
// When its slot gets a new skill or a level, it says so in a toast with the key, so the player knows
// what they just got and where. A skill that goes away (a restart) or a lower level says nothing.
public class SkillSlotView : MonoBehaviour
{
    [Tooltip("Skill1, Skill2 or Skill3. Basic has no key, so it shows nothing.")]
    [SerializeField] private AttackSlot _slot = AttackSlot.Skill1;

    [Tooltip("The skill's icon. Hidden while the skill has none.")]
    [SerializeField] private Image _icon;

    [Tooltip("Shown instead of the icon while there is none: the first letter of the skill's name.")]
    [SerializeField] private TMP_Text _initialLabel;

    [Tooltip("An Image of type Filled, Radial 360, on top of the icon. Full right after a cast, empty when ready.")]
    [SerializeField] private Image _cooldownFill;

    [Tooltip("Seconds left, rounded up. Empty when ready.")]
    [SerializeField] private TMP_Text _secondsLabel;

    [Tooltip("The key that casts it, as the Input System names it.")]
    [SerializeField] private TMP_Text _keyLabel;

    [Tooltip("Dims the whole slot while it has no skill.")]
    [SerializeField] private CanvasGroup _group;

    [SerializeField, Range(0f, 1f)] private float _emptyAlpha = 0.35f;

    [Tooltip("Opacity of the icon or the initial while the skill recharges, so the seconds on top read clearly.")]
    [SerializeField, Range(0f, 1f)] private float _rechargingContentAlpha = 0.25f;

    [Tooltip("Seconds the toast of a new skill or level stays, longer than a normal toast: it has a name, " +
             "what changed and a key to read.")]
    [SerializeField, Min(0.5f)] private float _toastSeconds = 4.5f;

    private LocalPlayerService _localPlayer;
    private PlayerSkills _skills;
    private SkillInstance _shownSkill;
    private int _shownSeconds = -1;
    private bool _shownRecharging;

    // What the slot had last frame, to tell a new skill or level from the slot being bound or reset.
    private string _lastId;
    private int _lastLevel;
    private bool _baselineTaken;

    private void OnEnable()
    {
        if (GameServices.Instance == null)
            return;

        _localPlayer = GameServices.Instance.LocalPlayerService;
        _localPlayer.Changed += OnLocalPlayerChanged;
        OnLocalPlayerChanged(_localPlayer.Current);

        ShowKey(GameServices.Instance.InputService);
    }

    private void OnDisable()
    {
        if (_localPlayer != null)
            _localPlayer.Changed -= OnLocalPlayerChanged;

        _localPlayer = null;
        _skills = null;
    }

    private void OnLocalPlayerChanged(PlayerIdentity player)
    {
        _skills = player != null ? player.GetComponent<PlayerSkills>() : null;

        // Forces the next Update to draw everything again.
        _shownSkill = null;
        _shownSeconds = -1;
        ShowSkill(null);

        // What the new player already has isn't news: the first frame only takes note of it.
        _baselineTaken = false;
    }

    private void Update()
    {
        SkillInstance skill = _skills != null ? _skills.GetSkill(_slot) : null;

        if (skill != _shownSkill)
        {
            AnnounceIfNew(skill);
            ShowSkill(skill);
        }
        else if (!_baselineTaken)
        {
            AnnounceIfNew(skill);
        }

        if (skill == null)
            return;

        float remaining = _skills.GetCooldownRemaining(_slot);

        if (_cooldownFill != null)
            _cooldownFill.fillAmount = remaining / skill.Cooldown;

        bool recharging = remaining > 0f;
        if (recharging != _shownRecharging)
            ShowRecharging(recharging);

        // The text only changes once a second, so it's only rebuilt then.
        int seconds = Mathf.CeilToInt(remaining);
        if (seconds != _shownSeconds && _secondsLabel != null)
        {
            _shownSeconds = seconds;
            _secondsLabel.text = seconds > 0 ? seconds.ToString() : string.Empty;
        }
    }

    private void ShowSkill(SkillInstance skill)
    {
        _shownSkill = skill;
        _shownSeconds = -1;

        bool hasIcon = skill != null && skill.Icon != null;

        if (_icon != null)
        {
            _icon.enabled = hasIcon;
            _icon.sprite = hasIcon ? skill.Icon : null;
        }

        if (_initialLabel != null)
        {
            string name = skill != null ? skill.DisplayName : null;
            _initialLabel.text = !hasIcon && !string.IsNullOrEmpty(name) ? name.Substring(0, 1).ToUpperInvariant() : string.Empty;
        }

        if (_cooldownFill != null)
            _cooldownFill.fillAmount = 0f;

        if (_secondsLabel != null)
            _secondsLabel.text = string.Empty;

        if (_group != null)
            _group.alpha = skill != null ? 1f : _emptyAlpha;

        ShowRecharging(false);
    }

    private void AnnounceIfNew(SkillInstance skill)
    {
        string id = skill != null ? skill.Id : null;
        int level = skill != null ? skill.Level : 0;
        bool announce = _baselineTaken && skill != null && (id != _lastId || level > _lastLevel);

        _baselineTaken = true;
        _lastId = id;
        _lastLevel = level;

        if (!announce || GameServices.Instance == null)
            return;

        string key = _keyLabel != null && !string.IsNullOrEmpty(_keyLabel.text) ? $" · {_keyLabel.text}" : string.Empty;
        string text;
        if (level <= 1)
        {
            text = $"Nueva habilidad: {skill.DisplayName}{key}";
        }
        else
        {
            SkillLevelModifier modifier = skill.Definition.GetLevelModifier(level);
            string change = modifier != null && !string.IsNullOrEmpty(modifier.Description) ? $": {modifier.Description}" : string.Empty;
            text = $"{skill.DisplayName} · nivel {level}{change}{key}";
        }

        GameServices.Instance.ToastService.Show(text, ToastType.Success, _toastSeconds);
    }

    // Fades what's under the seconds while it recharges, and brings it back when it's ready.
    private void ShowRecharging(bool recharging)
    {
        _shownRecharging = recharging;
        float alpha = recharging ? _rechargingContentAlpha : 1f;

        if (_icon != null)
        {
            Color color = _icon.color;
            color.a = alpha;
            _icon.color = color;
        }

        if (_initialLabel != null)
            _initialLabel.alpha = alpha;
    }

    private void ShowKey(InputService input)
    {
        if (_keyLabel == null || input == null)
            return;

        InputAction action = _slot switch
        {
            AttackSlot.Skill1 => input.Skill1,
            AttackSlot.Skill2 => input.Skill2,
            AttackSlot.Skill3 => input.Skill3,
            _ => null,
        };

        _keyLabel.text = action != null ? action.GetBindingDisplayString() : string.Empty;
    }
}
