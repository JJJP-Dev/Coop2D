using System;

// Something that happens once (#161): heal, give a third of a reroll... Actions go inside an
// OnEventBlock, which decides when they run. Only the host runs them.
// To add one, write a [Serializable] class that inherits from this one.
[Serializable]
public abstract class EffectAction
{
    /// <summary>
    /// Server only.
    /// </summary>
    public abstract void Run(EffectContext context);
}
