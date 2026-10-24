using System;

// One piece of what an upgrade does, as the progression catalog writes them: a stat that
// changes, something that happens on an event... An upgrade is a list of these in its asset, so a new
// upgrade made of existing blocks needs no code.
// Only the host runs them. The clients see the results through what already travels: health, stats,
// rerolls. To add a kind of block, write a [Serializable] class that inherits from this one: the
// Inspector's menu finds it by itself.
[Serializable]
public abstract class EffectBlock
{
    /// <summary>
    /// Server only. The player took this upgrade: once for each stack.
    /// </summary>
    public abstract void Acquire(EffectContext context);
}
