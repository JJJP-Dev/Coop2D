// How a bonus changes a stat, as the catalog writes it: flat adds to the base value, and percent
// multiplies the result. Stored in assets: never renumber.
public enum StatUnit : byte
{
    Flat = 0,
    Percent = 1,
}
