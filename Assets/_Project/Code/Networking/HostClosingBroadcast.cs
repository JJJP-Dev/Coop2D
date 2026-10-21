using FishNet.Broadcast;

// Sent by the host to every client right before it closes the match on purpose, so they can tell
// "the host left" apart from "the connection dropped". It carries nothing: arriving is the message.
public struct HostClosingBroadcast : IBroadcast
{
}
