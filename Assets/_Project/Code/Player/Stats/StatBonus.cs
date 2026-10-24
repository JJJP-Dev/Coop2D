// One bonus on one stat of a player: +3 of basic range, +9 % of basic interval... Each stack of
// an upgrade adds its own. A plain struct with public fields, so FishNet sends it without a serializer
// of our own.
public struct StatBonus
{
    public StatId Stat;
    public StatUnit Unit;
    public float Value;

    // The upgrade it comes from, to read in a log and to take bonuses away later by source.
    public string Source;

    public StatBonus(StatId stat, StatUnit unit, float value, string source)
    {
        Stat = stat;
        Unit = unit;
        Value = value;
        Source = source;
    }
}
