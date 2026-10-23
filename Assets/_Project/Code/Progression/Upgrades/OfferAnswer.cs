// What a player answers to their offer (#78, catalog C.5 and C.7). It travels in the answer's ServerRpc.
public enum OfferAnswer : byte
{
    Pick,
    Reroll,
    Banish,
    Skip,
}
