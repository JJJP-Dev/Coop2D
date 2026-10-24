// A bonus that only counts on hits that meet its conditions: Cazador de auras is +15 % of skill
// damage against enemies with an aura. Host only: it's always for damage, and damage is the host's.
public readonly struct ConditionalBonus
{
    public TargetConditions Conditions { get; }
    public StatBonus Bonus { get; }

    public ConditionalBonus(TargetConditions conditions, StatBonus bonus)
    {
        Conditions = conditions;
        Bonus = bonus;
    }
}
