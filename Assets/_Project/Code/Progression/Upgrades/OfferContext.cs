using System;

// Everything one offer depends on besides the random stream: the level of its level up, how
// many players there are and the player it is for. With the same context and the same stream, the
// generator always draws the same offer.
public class OfferContext
{
    public OfferContext(int level, int players, IUpgradeHolder holder, Func<UpgradeDefinition, bool> isTakenByOther)
    {
        Level = level;
        Players = players;
        Holder = holder;
        IsTakenByOther = isTakenByOther;
    }

    public int Level { get; }
    public int Players { get; }
    public IUpgradeHolder Holder { get; }

    /// <summary>
    /// Whether another player of the group already has it, for the upgrades only one can have.
    /// Null: nobody else has anything.
    /// </summary>
    public Func<UpgradeDefinition, bool> IsTakenByOther { get; }
}
