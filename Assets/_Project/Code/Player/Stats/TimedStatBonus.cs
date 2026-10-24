// A bonus that only lasts a while: Tempo, Viento a favor. Between two ticks of the host's clock,
// which the owner also follows, so both stop counting it on the same tick. Public fields, so FishNet
// sends it without a serializer of our own.
public struct TimedStatBonus
{
    public StatBonus Bonus;

    // From this tick, included...
    public uint StartTick;

    // ...to this one, not included.
    public uint EndTick;

    public TimedStatBonus(StatBonus bonus, uint startTick, uint endTick)
    {
        Bonus = bonus;
        StartTick = startTick;
        EndTick = endTick;
    }

    public bool IsActive(uint tick) => tick >= StartTick && tick < EndTick;
}
