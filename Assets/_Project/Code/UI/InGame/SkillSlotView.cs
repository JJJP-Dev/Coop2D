using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// One skill of the local player in the HUD, with its key and how long until it's ready (#73).
// It reads the owner's own cooldown, the same one that lets the cast through, so what it shows
// matches what happens when you press the key, on the host and on a client alike.
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

    private LocalPlayerService _localPlayer;
    private PlayerSkills _skills;
    private SkillDefinition _shownSkill;
    private int _shownSeconds = -1;
    private bool _shownRecharging;

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
    }

    private void Update()
    {
        SkillDefinition skill = _skills != null ? _skills.GetSkill(_slot) : null;

        if (skill != _shownSkill)
            ShowSkill(skill);

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

    private void ShowSkill(SkillDefinition skill)
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
