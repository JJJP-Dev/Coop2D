using System.Collections.Generic;
using FishNet.Object;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The buttons of the reaction test scene and what came out of them. Everything it does goes
// through ReactionLab, on the host. It lists every reaction this client sees, with who put the aura
// and who detonated it, so the nine combinations of aura and detonator can be checked against the table.
public class ReactionLabPanel : MonoBehaviour
{
    [SerializeField] private ReactionLab _lab;
    [SerializeField] private ElementLibrary _elements;

    [Header("Target")]
    [SerializeField] private Button _previousDummyButton;
    [SerializeField] private Button _nextDummyButton;
    [SerializeField] private TMP_Text _dummyLabel;

    [Header("Element")]
    [Tooltip("The chosen one stays pressed (not interactable).")]
    [SerializeField] private Button _fireButton;
    [SerializeField] private Button _waterButton;
    [SerializeField] private Button _electricButton;

    [Header("Actions")]
    [SerializeField] private Button _weakAuraButton;
    [SerializeField] private Button _strongAuraButton;
    [SerializeField] private Button _detonateButton;
    [SerializeField] private Button _resetButton;

    [Tooltip("Switches every player's basic attack on and off. Its label says which one it is.")]
    [SerializeField] private Button _basicAttackButton;
    [SerializeField] private TMP_Text _basicAttackLabel;

    [Header("Results")]
    [Tooltip("What the last button you pressed did.")]
    [SerializeField] private TMP_Text _lastActionLabel;

    [Tooltip("The latest reactions, newest first.")]
    [SerializeField] private TMP_Text _reactionsLabel;

    [SerializeField, Min(1)] private int _maxReactionLines = 8;

    private readonly List<string> _reactionLines = new();
    private CombatEventService _events;
    private int _dummyIndex;
    private ElementType _element = ElementType.Fire;

    private void Awake()
    {
        if (_lab == null || _elements == null)
        {
            Debug.LogError("[ReactionLabPanel] Lab or element library is not assigned.", this);
            enabled = false;
            return;
        }

        _previousDummyButton.onClick.AddListener(() => SelectDummy(_dummyIndex - 1));
        _nextDummyButton.onClick.AddListener(() => SelectDummy(_dummyIndex + 1));

        _fireButton.onClick.AddListener(() => SelectElement(ElementType.Fire));
        _waterButton.onClick.AddListener(() => SelectElement(ElementType.Water));
        _electricButton.onClick.AddListener(() => SelectElement(ElementType.Electric));

        _weakAuraButton.onClick.AddListener(() => _lab.ApplyAura(_dummyIndex, _element, AuraStrength.Weak));
        _strongAuraButton.onClick.AddListener(() => _lab.ApplyAura(_dummyIndex, _element, AuraStrength.Strong));
        _detonateButton.onClick.AddListener(() => _lab.Detonate(_dummyIndex, _element));
        _resetButton.onClick.AddListener(_lab.ResetDummies);
        _basicAttackButton.onClick.AddListener(_lab.ToggleBasicAttack);

        SelectDummy(0);
        SelectElement(_element);
        _lastActionLabel.text = string.Empty;
        _reactionsLabel.text = string.Empty;
    }

    private void OnEnable()
    {
        if (_lab != null)
        {
            _lab.Reported += OnReported;
            _lab.BasicAttackChanged += ShowBasicAttack;
            ShowBasicAttack(_lab.BasicAttackEnabled);
        }

        if (GameServices.Instance != null)
        {
            _events = GameServices.Instance.CombatEventService;
            _events.ReactionTriggered += OnReactionTriggered;
        }
    }

    private void OnDisable()
    {
        if (_lab != null)
        {
            _lab.Reported -= OnReported;
            _lab.BasicAttackChanged -= ShowBasicAttack;
        }

        if (_events != null)
            _events.ReactionTriggered -= OnReactionTriggered;

        _events = null;
    }

    // Wraps around at both ends.
    private void SelectDummy(int index)
    {
        int count = _lab.DummyCount;
        _dummyIndex = count > 0 ? (index % count + count) % count : 0;
        _dummyLabel.text = _lab.GetDummyName(_dummyIndex);
    }

    private void SelectElement(ElementType element)
    {
        _element = element;
        _fireButton.interactable = element != ElementType.Fire;
        _waterButton.interactable = element != ElementType.Water;
        _electricButton.interactable = element != ElementType.Electric;
    }

    private void ShowBasicAttack(bool enabled)
    {
        if (_basicAttackLabel != null)
            _basicAttackLabel.text = enabled ? "Ataque básico: encendido" : "Ataque básico: apagado";
    }

    private void OnReported(ReactionLabReport report)
    {
        _lastActionLabel.text = Describe(report);
    }

    private void OnReactionTriggered(ReactionInfo reaction)
    {
        string duo = reaction.IsDuo ? " · ¡DÚO!" : "";
        string line = $"{reaction.Reaction.DisplayName} ({reaction.Reaction.VariantId}) en {reaction.Target.name}: " +
                      $"aura de {Player(reaction.AuraOwner)}, detona {Player(reaction.Detonator)}{duo}";

        _reactionLines.Insert(0, line);
        if (_reactionLines.Count > _maxReactionLines)
            _reactionLines.RemoveAt(_reactionLines.Count - 1);

        _reactionsLabel.text = string.Join("\n", _reactionLines);
    }

    private string Describe(ReactionLabReport report)
    {
        if (report.Action == ReactionLabAction.Reset)
            return "Muñecos reiniciados: vida llena, sin aura y sin esperas.";

        string dummy = _lab.GetDummyName(report.DummyIndex);
        string element = ElementName(report.Element);

        if (report.Action == ReactionLabAction.Detonate)
        {
            return report.Result switch
            {
                AuraApplyResult.Reacted => $"{element} detona el aura de {dummy}.",
                AuraApplyResult.Refreshed => $"{dummy} ya tenía {element}: el aura se refresca y no hay reacción.",
                AuraApplyResult.Applied => $"{dummy} no tenía aura: ahora tiene {element} fuerte.",
                _ => $"Ignorado en {dummy}: acaba de reaccionar (bloqueo) o hay que esperar el enfriamiento del aura.",
            };
        }

        string strength = report.Strength == AuraStrength.Weak ? "débil" : "fuerte";

        return report.Result switch
        {
            AuraApplyResult.Applied => $"Aura {strength} de {element} en {dummy}.",
            AuraApplyResult.Refreshed => $"Aura de {element} refrescada en {dummy}.",
            _ => $"Ignorado en {dummy}: ya tiene otra aura y esto no detona, o hay que esperar el enfriamiento del aura.",
        };
    }

    private string ElementName(ElementType type)
    {
        ElementDefinition element = _elements.Get(type);
        return element != null ? element.DisplayName : type.ToString();
    }

    // The player's number, as the HUD and the colors count them: J1 to J4.
    private static string Player(NetworkObject player)
    {
        if (player == null)
            return "nadie";

        return player.TryGetComponent(out PlayerIdentity identity) ? $"J{identity.Slot + 1}" : player.name;
    }
}
