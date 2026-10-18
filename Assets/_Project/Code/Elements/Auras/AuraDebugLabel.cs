using FishNet.Object;
using TMPro;
using UnityEngine;

// Temporary view of an enemy's aura and who put it there, until the aura icons exist (#68).
public class AuraDebugLabel : MonoBehaviour
{
    [SerializeField] private AuraHolder _aura;
    [SerializeField] private TMP_Text _text;
    [SerializeField] private ElementLibrary _elements;
    [SerializeField] private PlayerColors _playerColors;

    private void OnEnable()
    {
        if (_aura == null || _text == null || _elements == null || _playerColors == null)
        {
            Debug.LogError("[AuraDebugLabel] Aura, text, element library or player colors are not assigned.", this);
            return;
        }

        _aura.Changed += Refresh;
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
        if (!aura.HasAura)
        {
            _text.text = string.Empty;
            return;
        }

        ElementDefinition element = _elements.Get(aura.Element);
        string name = element != null ? element.DisplayName : aura.Element.ToString();
        Color color = element != null ? element.Color : Color.white;
        string strength = aura.Strength == AuraStrength.Strong ? "fuerte" : "débil";

        _text.text = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{name}</color> ({strength}){DescribeOwner(aura.Owner)}";
    }

    private string DescribeOwner(NetworkObject owner)
    {
        if (owner == null || !owner.TryGetComponent(out PlayerIdentity identity) || identity.Slot < 0)
            return string.Empty;

        Color color = _playerColors.GetColor(identity.Slot);
        return $" · <color=#{ColorUtility.ToHtmlStringRGB(color)}>J{identity.Slot + 1}</color>";
    }
}
