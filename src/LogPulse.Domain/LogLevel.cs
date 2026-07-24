namespace LogPulse.Domain;

/// <summary>
/// Severity of a log event.
/// </summary>
public enum LogLevel : byte
{
    /// <summary>
    /// The most detailed level: step-by-step execution noise.
    /// </summary>
    Trace = 0,

    /// <summary>
    /// Developer diagnostics, normally off in production.
    /// </summary>
    Debug = 1,

    /// <summary>
    /// Normal operation facts ("request served").
    /// </summary>
    Info = 2,

    /// <summary>
    /// Something is wrong but the operation continued.
    /// </summary>
    Warning = 3,

    /// <summary>
    /// An operation failed.
    /// </summary>
    Error = 4,

    /// <summary>
    /// The process (or a whole subsystem) cannot continue.
    /// </summary>
    Fatal = 5,
}

/// <summary>
/// Helper methods that give LogLevel values instance-style behavior.
/// </summary>
public static class LogLevelExtensions
{
    /// <summary>
    /// True when this level is equal to or more severe than <paramref name="minimum"/>.
    /// </summary>
    public static bool IsAtLeast(this LogLevel level, LogLevel minimum) => level >= minimum;
}
