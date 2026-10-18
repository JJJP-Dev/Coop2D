using FishNet.Broadcast;
using UnityEngine;

// The path of an Electrocution chain, from the target through every jump, as the host saw it.
// A broadcast rather than an RPC: no enemy on the chain is sure to live long enough to send it.
// FishNet serializes public fields, so this struct can't be read-only like HitInfo.
public struct ReactionChainBroadcast : IBroadcast
{
    public Vector2[] Points;
    public Color Color;
}
