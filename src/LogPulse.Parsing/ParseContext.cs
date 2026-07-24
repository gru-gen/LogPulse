namespace LogPulse.Parsing;

/// <summary>
/// Ambient facts a parser can't get from the line itself.
/// </summary>
public readonly record struct ParseContext(DateTimeOffset ReceivedAt, string DefaultService)
{
    /// <summary>
    /// Creates a context with the current UTC time and a default service.
    /// </summary>
    public static ParseContext Now(string defaultService = LogEvent.UnknownService) =>
        new(DateTimeOffset.UtcNow, defaultService);
}
