// The catalog's element of an upgrade (#78). Primary is «the element of whoever has it» and Any is
// «whatever the aura is». Not ElementType, which only has real elements and travels over the network.
// Assets store the number, so never renumber one.
public enum UpgradeElement
{
    None = 0,
    Primary = 1,
    Any = 2,
    Fire = 3,
    Water = 4,
    Electric = 5,
}
