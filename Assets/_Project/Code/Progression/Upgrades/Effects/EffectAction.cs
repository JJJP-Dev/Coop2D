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

    /// <summary>
    /// Owner only, for events it can see coming (its own skill cast): what it shows at once, before the
    /// host's answer arrives (#157). Most actions show nothing ahead and leave it empty.
    /// </summary>
    public virtual void Predict(EffectContext context)
    {
    }
}
