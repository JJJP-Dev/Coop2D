using System.Text;
using TMPro;
using UnityEngine;

// The strip at the bottom of the HUD where the local player chooses their upgrades (#77), without
// pausing anything: they keep moving and the basic attack keeps firing.
//   1, 2, 3 or a click pick; F rerolls; X and then a number banishes; C skips; Tab hides and shows it.
// It only shows the offer the host sent to this player and sends back what they chose (PlayerOffers).
// When it's hidden and offers are waiting, a line of the HUD says how many.
public class UpgradeOfferPanel : MonoBehaviour
{
    [Tooltip("What shows and hides: the strip with the rows. A child of this object, so this one keeps reading keys.")]
    [SerializeField] private GameObject _strip;

    [Tooltip("The three rows, top to bottom. More offer options than rows are not shown.")]
    [SerializeField] private UpgradeOfferRow[] _rows = new UpgradeOfferRow[3];

    [Tooltip("Top line of the strip: level and offers waiting.")]
    [SerializeField] private TMP_Text _headerLabel;

    [Tooltip("Bottom line of the strip: the keys, with rerolls and banishes left.")]
    [SerializeField] private TMP_Text _hintLabel;

    [Tooltip("Outside the strip. Shown while it's hidden and offers are waiting.")]
    [SerializeField] private TMP_Text _queueLabel;

    [Header("Colors of the names, until the HUD art (#43)")]
    [SerializeField] private Color _commonColor = new(0.91f, 0.91f, 0.91f);
    [SerializeField] private Color _rareColor = new(0.35f, 0.66f, 1f);
    [SerializeField] private Color _epicColor = new(0.78f, 0.49f, 1f);
    [SerializeField] private Color _specialColor = new(1f, 0.84f, 0f);
    [SerializeField] private Color _fallbackColor = new(0.63f, 0.63f, 0.63f);

    [Tooltip("Name of a new skill or a skill level in the offer (#162).")]
    [SerializeField] private Color _skillColor = new(0.4f, 0.9f, 0.75f);
    [SerializeField] private Color _descriptionColor = new(0.75f, 0.75f, 0.75f);

    private readonly StringBuilder _text = new();

    private InputService _input;
    private PlayerIdentity _boundIdentity;
    private PlayerOffers _offers;
    private PlayerUpgrades _player;

    // What is on screen, so it's only redrawn when something changes.
    private ReceivedOffer _shownOffer;
    private int _shownPending = -1;
    private int _shownRerollThirds = -1;
    private int _shownBanishes = -1;
    private bool _shownBanishMode;
    private bool _shownAnswered;
    private bool? _shownVisible;
    private bool _dirty = true;

    private bool _hiddenByPlayer;
    private bool _banishMode;

    private void Awake()
    {
        if (_strip == null || _headerLabel == null || _hintLabel == null || _queueLabel == null || _rows == null)
        {
            Debug.LogError("[UpgradeOfferPanel] Strip, a label or the rows are not assigned.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < _rows.Length; i++)
        {
            if (_rows[i] != null)
                _rows[i].Bind(i, OnRowClicked);
        }

        _strip.SetActive(false);
        _queueLabel.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        Bind(null);
        _boundIdentity = null;
    }

    private void Update()
    {
        if (_input == null && GameServices.Instance != null)
            _input = GameServices.Instance.InputService;

        BindLocalPlayer();

        ReceivedOffer offer = _offers != null ? _offers.CurrentOffer : null;
        bool over = MatchFlow.Current != null && MatchFlow.Current.Phase == MatchPhase.Over;

        // A new offer after none: it opens by itself, even if the player had hidden the last one.
        if (offer != null && _shownOffer == null)
            _hiddenByPlayer = false;

        bool visible = offer != null && !_hiddenByPlayer && !over;

        // Polled like PlayerSkills: these are the same frames the player is fighting in.
        if (_input != null && offer != null && !over)
            ReadKeys(offer, visible);

        visible = offer != null && !_hiddenByPlayer && !over;

        if (_offers != null)
            _offers.SetChoosing(visible);

        Refresh(offer, visible);
    }

    private void ReadKeys(ReceivedOffer offer, bool visible)
    {
        if (_input.OfferTogglePanel.WasPressedThisFrame())
        {
            _hiddenByPlayer = !_hiddenByPlayer;
            _banishMode = false;
            _dirty = true;
            return;
        }

        if (!visible || offer.IsAnswered)
            return;

        if (_input.OfferChoose1.WasPressedThisFrame())
            Choose(offer, 0);
        else if (_input.OfferChoose2.WasPressedThisFrame())
            Choose(offer, 1);
        else if (_input.OfferChoose3.WasPressedThisFrame())
            Choose(offer, 2);
        else if (_input.OfferReroll.WasPressedThisFrame() && _player.Rerolls > 0)
            Send(OfferAnswer.Reroll, 0);
        else if (_input.OfferBanish.WasPressedThisFrame() && (_banishMode || _player.BanishesLeft > 0))
        {
            _banishMode = !_banishMode;
            _dirty = true;
        }
        else if (_input.OfferSkip.WasPressedThisFrame())
            Send(OfferAnswer.Skip, 0);
    }

    private void OnRowClicked(int index)
    {
        ReceivedOffer offer = _offers != null ? _offers.CurrentOffer : null;
        if (offer != null && !offer.IsAnswered)
            Choose(offer, index);
    }

    private void Choose(ReceivedOffer offer, int index)
    {
        if (index >= offer.Options.Length || offer.Options[index] == null)
            return;

        if (!_banishMode)
        {
            Send(OfferAnswer.Pick, index);
            return;
        }

        if (offer.CanBanish(index))
            Send(OfferAnswer.Banish, index);
    }

    private void Send(OfferAnswer answer, int index)
    {
        _banishMode = false;
        _offers.Answer(answer, index);
        _dirty = true;
    }

    // The local player can change (joining, a restart), so it's looked up again when it does.
    private void BindLocalPlayer()
    {
        PlayerIdentity identity = GameServices.Instance != null ? GameServices.Instance.LocalPlayerService.Current : null;
        if (identity == _boundIdentity)
            return;

        _boundIdentity = identity;
        Bind(identity != null ? identity.GetComponent<PlayerOffers>() : null);
    }

    private void Bind(PlayerOffers offers)
    {
        if (_offers != null)
        {
            _offers.OfferChanged -= OnOfferChanged;
            _player.UpgradesChanged -= OnOfferChanged;
        }

        _offers = offers;
        _player = offers != null ? offers.Upgrades : null;
        _banishMode = false;
        _dirty = true;

        if (_offers != null)
        {
            _offers.OfferChanged += OnOfferChanged;
            _player.UpgradesChanged += OnOfferChanged;
        }
    }

    private void OnOfferChanged()
    {
        _banishMode = false;
        _dirty = true;
    }

    // Only when something it shows changed, so the canvas isn't rebuilt every frame.
    private void Refresh(ReceivedOffer offer, bool visible)
    {
        int pending = _offers != null ? _offers.PendingOffers : 0;
        int thirds = _player != null ? _player.Rerolls * 3 + _player.RerollThirdsOver : 0;
        int banishes = _player != null ? _player.BanishesLeft : 0;
        bool answered = offer != null && offer.IsAnswered;

        if (!_dirty && offer == _shownOffer && visible == _shownVisible && pending == _shownPending &&
            thirds == _shownRerollThirds && banishes == _shownBanishes && _banishMode == _shownBanishMode &&
            answered == _shownAnswered)
            return;

        _dirty = false;
        _shownOffer = offer;
        _shownVisible = visible;
        _shownPending = pending;
        _shownRerollThirds = thirds;
        _shownBanishes = banishes;
        _shownBanishMode = _banishMode;
        _shownAnswered = answered;

        _strip.SetActive(visible);

        bool showQueue = !visible && pending > 0 && (MatchFlow.Current == null || MatchFlow.Current.Phase != MatchPhase.Over);
        _queueLabel.gameObject.SetActive(showQueue);
        if (showQueue)
            _queueLabel.text = $"Mejoras: {pending} · Tab";

        if (!visible)
            return;

        _headerLabel.text = pending > 1 ? $"Nivel {offer.Level} · +{pending - 1} en cola" : $"Nivel {offer.Level}";
        _hintLabel.text = _banishMode
            ? "Descartar: 1, 2 o 3 · X cancela"
            : $"F volver a tirar ({FormatRerolls(thirds)}) · X descartar ({banishes}) · C saltar · Tab ocultar";

        for (int i = 0; i < _rows.Length; i++)
        {
            if (_rows[i] == null)
                continue;

            OfferOption? option = i < offer.Options.Length ? offer.Options[i] : null;
            if (option == null)
            {
                _rows[i].Hide();
                continue;
            }

            bool dimmed = answered || (_banishMode && !offer.CanBanish(i));
            _rows[i].Show(FormatText(option.Value), FormatTag(option.Value), dimmed);
        }
    }

    // A skill shows its own description when new, and what the level changes otherwise (#162).
    private string FormatText(OfferOption option)
    {
        string name;
        string description;
        Color color;

        if (option.IsSkill)
        {
            name = option.Skill.DisplayName;
            SkillLevelModifier level = option.Skill.GetLevelModifier(option.SkillLevel);
            description = level != null ? level.Description : option.Skill.Description;
            color = _skillColor;
        }
        else
        {
            name = option.Upgrade.DisplayName;
            description = option.Upgrade.Description;
            color = GetColor(option.Upgrade);
        }

        _text.Clear();
        _text.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(color)).Append('>')
             .Append(name).Append("</color> <color=#")
             .Append(ColorUtility.ToHtmlStringRGB(_descriptionColor)).Append('>')
             .Append(description).Append("</color>");
        return _text.ToString();
    }

    private string FormatTag(OfferOption option)
    {
        if (!option.IsSkill)
            return FormatTag(option.Upgrade);

        return option.SkillLevel <= 1 ? "habilidad" : $"nivel {option.SkillLevel}";
    }

    // The next stack when it has more than one, and otherwise its rarity.
    private string FormatTag(UpgradeDefinition upgrade)
    {
        if (IsFallback(upgrade))
            return "";

        if (upgrade.MaxStacks > 1)
            return $"{_player.GetStacks(upgrade.Id) + 1}/{upgrade.MaxStacks}";

        return upgrade.Rarity switch
        {
            UpgradeRarity.Rare => "rara",
            UpgradeRarity.Epic => "épica",
            UpgradeRarity.Legendary => "legendaria",
            UpgradeRarity.Duo => "dúo",
            UpgradeRarity.Evolution => "evolución",
            _ => "",
        };
    }

    private Color GetColor(UpgradeDefinition upgrade)
    {
        if (IsFallback(upgrade))
            return _fallbackColor;

        return upgrade.Rarity switch
        {
            UpgradeRarity.Common => _commonColor,
            UpgradeRarity.Rare => _rareColor,
            UpgradeRarity.Epic => _epicColor,
            _ => _specialColor,
        };
    }

    private static bool IsFallback(UpgradeDefinition upgrade)
    {
        UpgradeOffers offers = UpgradeOffers.Current;
        return offers != null && offers.Pool != null && upgrade == offers.Pool.Fallback;
    }

    // Whole rerolls, and the third on its way: «1», «0 +1/3», «1 +2/3».
    private static string FormatRerolls(int thirds)
    {
        int whole = thirds / 3;
        int over = thirds % 3;
        return over > 0 ? $"{whole} +{over}/3" : whole.ToString();
    }
}
