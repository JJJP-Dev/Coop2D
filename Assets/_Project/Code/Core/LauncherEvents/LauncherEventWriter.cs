using System;
using System.Globalization;
using System.Text;
using UnityEngine;

// Writes one [PalitoEvent] line in Player.log, the format Palito Launcher reads
// (docs/launcher-events.md, contract v1). It knows the format, not the events: those are in LauncherEvents.
// In every build the testers get, development or not, and only from the main thread.
public static class LauncherEventWriter
{
    private const string Prefix = "[PalitoEvent] ";
    private const int FormatVersion = 1;
    private const int MaxLineLength = 512;

    private static readonly StringBuilder Line = new(MaxLineLength);
    private static int _sequence;

    // Before any scene, so session_start is always seq=1. With Enter Play Mode Options and no
    // domain reload, static fields keep their values between Plays, so they're reset here.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void StartSession()
    {
        _sequence = 0;

        Write("session_start",
            ("game_version", Application.version),
            ("dev_build", Debug.isDebugBuild),
            ("launcher_session", ReadArgument("-palitoSession") ?? "none"),
            ("utc", DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)));

        // Removed first, so a Play without domain reload doesn't subscribe twice.
        Application.quitting -= EndSession;
        Application.quitting += EndSession;
    }

    // Only on a normal exit. If the game crashes or is killed, there's no session_end,
    // and that's how the launcher knows it didn't end well.
    private static void EndSession()
    {
        Write("session_end", ("duration", (int)Time.realtimeSinceStartupAsDouble));
    }

    public static void Write(string eventName, params (string Key, object Value)[] fields)
    {
        Line.Clear();
        Line.Append(Prefix);
        Line.Append("v=").Append(FormatVersion.ToString(CultureInfo.InvariantCulture));
        Line.Append(" seq=").Append((++_sequence).ToString(CultureInfo.InvariantCulture));
        Line.Append(" t=").Append(Time.realtimeSinceStartupAsDouble.ToString("0.000", CultureInfo.InvariantCulture));
        Line.Append(" ev=").Append(eventName);

        foreach ((string key, object value) in fields)
        {
            string text = Format(value);
            WarnIfInvalid(eventName, key, text);
            Line.Append(' ').Append(key).Append('=').Append(text);
        }

        // Log, never a warning: the console of the test checklist must stay clean.
        // No call stack, so each event is a single line in the log.
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", Line.ToString());
    }

    // Invariant culture always: a Spanish Windows would write 318,412 and the launcher would drop the line.
    private static string Format(object value)
    {
        return value switch
        {
            null => "none",
            bool b => b ? "true" : "false",
            float f => f.ToString("0.###", CultureInfo.InvariantCulture),
            double d => d.ToString("0.###", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString(),
        };
    }

    // A bad key or value makes the launcher drop the whole line, so the editor says so right away.
    // In builds it costs nothing.
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private static void WarnIfInvalid(string eventName, string key, string value)
    {
        if (!IsValidKey(key) || !IsValidValue(value))
            Debug.LogWarning($"[LauncherEventWriter] {eventName}: '{key}={value}' doesn't follow the contract. The launcher will drop this line.");
    }

    // ^[a-z][a-z0-9_]{0,31}$
    private static bool IsValidKey(string key)
    {
        if (string.IsNullOrEmpty(key) || key.Length > 32 || key[0] < 'a' || key[0] > 'z')
            return false;

        foreach (char c in key)
        {
            if (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9') && c != '_')
                return false;
        }

        return true;
    }

    // ^[A-Za-z0-9_.:+-]{1,64}$
    private static bool IsValidValue(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 64)
            return false;

        foreach (char c in value)
        {
            bool allowed = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') ||
                           c == '_' || c == '.' || c == ':' || c == '+' || c == '-';
            if (!allowed)
                return false;
        }

        return true;
    }

    private static string ReadArgument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }
}
