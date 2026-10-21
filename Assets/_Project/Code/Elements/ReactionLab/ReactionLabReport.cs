// What one button of the reaction test scene did, as the host saw it. The reaction itself, if any,
// arrives on its own through CombatEventService, to every player.
// FishNet serializes public fields, so this struct can't be read-only.
public struct ReactionLabReport
{
    public ReactionLabAction Action;

    // -1 for Reset, which touches every dummy.
    public int DummyIndex;

    public ElementType Element;
    public AuraStrength Strength;
    public AuraApplyResult Result;

    public ReactionLabReport(ReactionLabAction action, int dummyIndex, ElementType element,
                             AuraStrength strength, AuraApplyResult result)
    {
        Action = action;
        DummyIndex = dummyIndex;
        Element = element;
        Strength = strength;
        Result = result;
    }
}
