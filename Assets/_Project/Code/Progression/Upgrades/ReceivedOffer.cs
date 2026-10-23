// An offer as its owner's client received it (#78): what the panel shows. The host keeps the real one;
// this copy only has what the player needs to see and to answer with.
public class ReceivedOffer
{
    private readonly bool[] _banishable;

    public ReceivedOffer(int id, int level, UpgradeDefinition[] options, bool[] banishable)
    {
        Id = id;
        Level = level;
        Options = options;
        _banishable = banishable;
    }

    /// <summary>
    /// The host's number for this version of the offer. A reroll or a banish sends a new one, so an
    /// answer to an older version is ignored.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Level of the level up it came from.
    /// </summary>
    public int Level { get; }

    public UpgradeDefinition[] Options { get; }

    /// <summary>
    /// True once the player answered, until the host's reply arrives. The panel ignores more input meanwhile.
    /// </summary>
    public bool IsAnswered { get; set; }

    public bool CanBanish(int index)
    {
        return index >= 0 && index < _banishable.Length && _banishable[index];
    }
}
