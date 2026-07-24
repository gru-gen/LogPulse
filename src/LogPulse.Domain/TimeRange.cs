namespace LogPulse.Domain;

/// <summary>
/// Half-open time interval [Start, End).
/// </summary>
public readonly record struct TimeRange
{
    /// <summary>
    /// The first moment of the range (inclusive).
    /// </summary>
    public DateTimeOffset Start { get; }

    /// <summary>
    /// The end of the range (exclusive).
    /// </summary>
    public DateTimeOffset End { get; }

    /// <summary>
    /// Creates a range. Throws when End is before Start.
    /// </summary>
    public TimeRange(DateTimeOffset start, DateTimeOffset end)
    {
        if (end < start)
            throw new ArgumentException($"End ({end:O}) must not precede start ({start:O}).", nameof(end));
        Start = start;
        End = end;
    }

    /// <summary>
    /// True when the timestamp falls inside the range (Start inclusive, End exclusive).
    /// </summary>
    public bool Contains(DateTimeOffset timestamp) => timestamp >= Start && timestamp < End;

    /// <summary>
    /// A range that ends now and covers the given duration back:
    /// TimeRange.Past(TimeSpan.FromMinutes(15)) = "the past 15 minutes".
    /// </summary>
    public static TimeRange Past(TimeSpan interval, TimeProvider? clock = null)
    {
        DateTimeOffset now = (clock ?? TimeProvider.System).GetUtcNow();
        return new TimeRange(now - interval, now);
    }

    /// <summary>
    /// Formats the range as "start ... end".
    /// </summary>
    public override string ToString() => $"[{Start:O} .. {End:O})";
}
