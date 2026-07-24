using LogPulse.Domain;
using LogPulse.Domain.Collections;

namespace LogPulse.Api;

public sealed class InMemoryEventStore
{
    private const int DefaultCapacity = 3;
    private readonly RingBuffer<LogEvent> _buffer = new(DefaultCapacity);
    private readonly object _gate = new();

    /// <summary>
    /// Accepts one parsed event.
    /// </summary>
    public void Add(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        lock (_gate) _buffer.Add(logEvent);
    }

    /// <summary>
    /// All stored events inside the time range, oldest first.
    /// </summary>
    public IReadOnlyList<LogEvent> Query(TimeRange range)
    {
        lock (_gate)
        {
            List<LogEvent> result = new List<LogEvent>();
            foreach (LogEvent logEvent in _buffer)
                if (range.Contains(logEvent.Timestamp))
                    result.Add(logEvent);
            return result;
        }
    }
}
