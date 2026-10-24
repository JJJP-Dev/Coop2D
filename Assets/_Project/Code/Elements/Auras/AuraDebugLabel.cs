using FishNet.Object;
using TMPro;
using UnityEngine;

// Temporary view of an enemy's aura and who put it there, until the aura icons exist. It also says
// «Aturdido» while the enemy can't move or attack (EnemyStatus), so a trap or a stun is seen.
public class AuraDebugLabel : MonoBehaviour
{
    [SerializeField] private AuraHolder _aura;
    [SerializeField] private TMP_Text _text;
    [SerializeField] private ElementLibrary _elements;
    [SerializeField] private PlayerColors _playerColors;

    [Tooltip("Optional: empty, it's looked up on the enemy. Dummies have none and are never stunned.")]
    [SerializeField] private EnemyStatus _status;

    private bool _shownStunned;

    private void OnEnable()
    {
        if (_aura == null || _text == null || _elements == null || _playerColors == null)
        {
            Debug.LogError("[AuraDebugLabel] Aura, text, element library or player colors are not assigned.", this);
            return;
        }

        if (_status == null)
            _status = GetComponentInParent<EnemyStatus>();

        _aura.Changed += Refresh;
        Refresh(_aura);
    }

    // Polled: the stun ends by itself on a tick, with no event to listen to.
    private void Update()
    {
        bool stunned = _status != null && _status.LooksStunned;
        if (stunned != _shownStunned && _aura != null && _text != null && _elements != null && _playerColors != null)
            Refresh(_aura);
    }

    private void Start()
    {
        // OnEnable can run before AuraHolder's Awake on a new instance. Start runs after every Awake.
        if (_aura != null && _text != null && _elements != null && _playerColors != null)
            Refresh(_aura);
    }

    private void OnDisable()
    {
        if (_aura != null)
            _aura.Changed -= Refresh;
    }

    private void Refresh(AuraHolder aura)
    {
        _shownStunned = _status != null && _status.LooksStunned;
        string stunned = _shownStunned ? "<color=#FFFFFF>Aturdido</color>" : string.Empty;

        if (!aura.HasAura)
        {
            _text.text = stunned;
            return;
        }

        ElementDefinition element = _elements.Get(aura.Element);
        string name = element != null ? element.DisplayName : aura.Element.ToString();
        Color color = element != null ? element.Color : Color.white;
        string strength = aura.Strength == AuraStrength.Strong ? "fuerte" : "débil";

        string line = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{name}</color> ({strength}){DescribeOwner(aura.Owner)}";
        _text.text = _shownStunned ? $"{stunned}\n{line}" : line;
    }

    private string DescribeOwner(NetworkObject owner)
    {
        if (owner == null || !owner.TryGetComponent(out PlayerIdentity identity) || identity.Slot < 0)
            return string.Empty;

        Color color = _playerColors.GetColor(identity.Slot);
        return $" · <color=#{ColorUtility.ToHtmlStringRGB(color)}>J{identity.Slot + 1}</color>";
    }
}
