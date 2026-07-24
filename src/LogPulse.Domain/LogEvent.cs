namespace LogPulse.Domain;

/// <summary>
/// One immutable log event. The core value of the whole system.
/// </summary>
public sealed record LogEvent
{
    /// <summary>
    /// The sentinel service name for events whose producer did not say.
    /// </summary>
    public const string UnknownService = "unknown";

    /// <summary>
    /// Unique id.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// When the event happened (with time-zone offset).
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// The human-readable log text.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// The severity of the event.
    /// </summary>
    public LogLevel Level { get; init; } = LogLevel.Info;

    /// <summary>
    /// Logical service/application that produced the event.
    /// </summary>
    public string Service { get; init; } = UnknownService;
}
