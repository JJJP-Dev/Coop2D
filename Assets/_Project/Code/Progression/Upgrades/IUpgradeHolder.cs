using System.Collections.Generic;

// What the offer generator needs to know about the player it draws for. PlayerUpgrades is the
// real one; being an interface keeps the generator plain C#, so it can be checked without a match.
public interface IUpgradeHolder
{
    int GetStacks(string id);

    /// <summary>
    /// By offer key: an upgrade's id, or a skill's key (OfferOption.SkillKey).
    /// </summary>
    bool IsBanished(string key);

    /// <summary>
    /// By offer key, as IsBanished.
    /// </summary>
    bool WasSeen(string key);

    bool MeetsRequirement(string requirement);

    /// <summary>
    /// rarity_luck. 0 until player stats exist.
    /// </summary>
    int Luck { get; }

    /// <summary>
    /// Offers in a row that could have had a duo and didn't.
    /// </summary>
    int OffersWithoutDuo { get; }

    // --- Skills ---

    /// <summary>
    /// The player's main element, for the solo rule of the level 6 guarantee.
    /// </summary>
    ElementType Element { get; }

    /// <summary>
    /// Whether a new skill still fits.
    /// </summary>
    bool HasFreeSkillSlot { get; }

    /// <summary>
    /// The level the player has that skill at, or 0.
    /// </summary>
    int GetSkillLevel(string skillId);

    /// <summary>
    /// Fills skills with the player's, slot by slot.
    /// </summary>
    void CollectOwnedSkills(List<SkillDefinition> skills);

    /// <summary>
    /// Fills own with the character's skills and generic with the ones any character can get, in their
    /// fixed order, the ones the player has included: the generator leaves those out.
    /// </summary>
    void CollectSkillPool(List<SkillDefinition> own, List<SkillDefinition> generic);
}
