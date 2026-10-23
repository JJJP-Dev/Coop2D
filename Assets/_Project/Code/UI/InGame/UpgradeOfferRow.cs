using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One row of the upgrade offer's strip (#77): the key that picks it, the upgrade in one line and a tag
// (rarity, stacks). Only the picture; UpgradeOfferPanel decides what it shows and what a click does.
public class UpgradeOfferRow : MonoBehaviour
{
    // What a row the player can't use right now looks like: answered, or not banishable.
    private const float DimmedAlpha = 0.35f;

    [Tooltip("The whole row. Its Image must be a raycast target, so clicking it doesn't also cast a skill.")]
    [SerializeField] private Button _button;

    [SerializeField] private TMP_Text _keyLabel;

    [Tooltip("Name and description in one line, with rich text for the name's color.")]
    [SerializeField] private TMP_Text _textLabel;

    [Tooltip("Rarity or stacks, on the right.")]
    [SerializeField] private TMP_Text _tagLabel;

    private int _index;
    private Action<int> _clicked;

    private void Awake()
    {
        if (_button == null || _keyLabel == null || _textLabel == null || _tagLabel == null)
        {
            Debug.LogError("[UpgradeOfferRow] Button or a label is not assigned.", this);
            enabled = false;
            return;
        }

        _button.onClick.AddListener(OnClicked);
    }

    public void Bind(int index, Action<int> clicked)
    {
        _index = index;
        _clicked = clicked;
        _keyLabel.text = (index + 1).ToString();
    }

    public void Show(string text, string tag, bool dimmed)
    {
        gameObject.SetActive(true);
        _textLabel.text = text;
        _tagLabel.text = tag;

        float alpha = dimmed ? DimmedAlpha : 1f;
        _keyLabel.alpha = alpha;
        _textLabel.alpha = alpha;
        _tagLabel.alpha = alpha;
        _button.interactable = !dimmed;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnClicked()
    {
        _clicked?.Invoke(_index);
    }
}
