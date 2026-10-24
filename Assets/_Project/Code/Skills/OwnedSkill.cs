// A skill one player has in one slot (#162), as it travels over the network: its id and its level.
// Each PC turns it into a SkillInstance with the SkillLibrary.
public struct OwnedSkill
{
    public string Id;
    public byte Level;

    public OwnedSkill(string id, byte level)
    {
        Id = id;
        Level = level;
    }
}
