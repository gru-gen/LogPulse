using System.Collections.Frozen;

namespace LogPulse.Domain;

/// <summary>
/// Maps the severity spellings found in real-world logs ("ERROR", "warn", "CRIT"…)
/// to LogLevel.
/// </summary>
public static class SeverityKeywords
{
    private static readonly FrozenDictionary<string, LogLevel> Map =
        new Dictionary<string, LogLevel>(StringComparer.OrdinalIgnoreCase)
        {
            ["trace"] = LogLevel.Trace,
            ["verbose"] = LogLevel.Trace,
            ["debug"] = LogLevel.Debug,
            ["dbg"] = LogLevel.Debug,
            ["info"] = LogLevel.Info,
            ["information"] = LogLevel.Info,
            ["notice"] = LogLevel.Info,
            ["warn"] = LogLevel.Warning,
            ["warning"] = LogLevel.Warning,
            ["error"] = LogLevel.Error,
            ["err"] = LogLevel.Error,
            ["severe"] = LogLevel.Error,
            ["fatal"] = LogLevel.Fatal,
            ["crit"] = LogLevel.Fatal,
            ["critical"] = LogLevel.Fatal,
            ["emerg"] = LogLevel.Fatal,
            ["panic"] = LogLevel.Fatal,
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Longest fast-path spelling is "warning" (7)
    /// </summary>
    private const int LongestFastPathKeyword = 7;

    /// <summary>
    /// Span overload for the parsing hot path — resolves a level WITHOUT allocating a string.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> keyword, out LogLevel level) 
    {
        if (keyword.Length <= LongestFastPathKeyword)
        {
            Span<char> lowered = stackalloc char[LongestFastPathKeyword];
            int loweredlength = keyword.ToLowerInvariant(lowered);
            switch (lowered[..loweredlength])
            {
                case "info":                level = LogLevel.Info;      return true;
                case "error":               level = LogLevel.Error;     return true;
                case "warn" or "warning":   level = LogLevel.Warning;   return true;
                case "debug":               level = LogLevel.Debug;     return true;
                case "fatal":               level = LogLevel.Fatal;     return true;
                case "trace":               level = LogLevel.Trace;     return true;
            }
        }

        return Map.TryGetValue(keyword.ToString(), out level);  // rare spellings only
    }
}
