// Which of a player's attacks something comes from. Each one is a separate source
// for the aura cooldown, so a skill can still mark an enemy right after your basic attack.
public enum AttackSlot : byte
{
    Basic,
    Skill1,
    Skill2,
    Skill3
}
