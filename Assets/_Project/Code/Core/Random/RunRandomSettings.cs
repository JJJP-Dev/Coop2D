using System.Globalization;
using UnityEngine;

// How RunRandomService picks the seed of each run. Only for testing: in a release build
// every run gets a new seed whatever this says, so a forgotten value can't ship.
[CreateAssetMenu(fileName = "RunRandomSettings", menuName = "Coop2D/Core/Run Random Settings")]
public class RunRandomSettings : ScriptableObject
{
    [Tooltip("Plays every run with this seed: 8 hexadecimal characters, as the log and the cheats panel show it " +
             "(3A9F01C2). Empty: a new seed every run. Only the editor and development builds use it.")]
    [SerializeField] private string _fixedSeed = "";

    [Tooltip("Writes every gameplay roll to the console, to compare two runs with the same seed. " +
             "Only the editor and development builds write them.")]
    [SerializeField] private bool _logRolls;

    /// <summary>
    /// True and the seed when a valid fixed seed is set. A seed of 0 counts as none.
    /// </summary>
    public bool TryGetFixedSeed(out uint seed)
    {
        seed = 0;
        string text = _fixedSeed != null ? _fixedSeed.Trim() : "";

        return text.Length > 0
               && uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out seed)
               && seed != 0;
    }

    /// <summary>
    /// True when something is written in the fixed seed, valid or not, so the service can say it's wrong.
    /// </summary>
    public bool HasFixedSeedText => !string.IsNullOrWhiteSpace(_fixedSeed);

    public bool LogRolls => _logRolls;
}
