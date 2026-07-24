using LogPulse.Domain;
using LogPulse.Domain.Collections;

DateTimeOffset baseTime = new DateTimeOffset(2026, 07, 09, 12, 00, 00, TimeSpan.Zero);

// ════════════════════════════════════════════════════════════════════════════════════
Section("LogEvent record: required members, with-expressions, value equality", () =>
{
    LogEvent evt = new LogEvent
    {
        Timestamp = baseTime,
        Message = "Payment authorization failed",
        Level = LogLevel.Error,
        Service = "payments",
    };

    Console.WriteLine($"value equality: {evt == (evt with { })}  (distinct instances, same data)");
});

// ════════════════════════════════════════════════════════════════════════════════════
Section("RingBuffer<T>: our own IEnumerable<T>/IEnumerator<T>, from the inside", () =>
{
    RingBuffer<LogEvent> tail = new RingBuffer<LogEvent>(capacity: 3);
    for (int i = 0; i <= 5; i++)
        tail.Add(CreateEvent($"error #{i}", i));

    Console.WriteLine($"5 adds into capacity 3 : count {tail.Count}, latest: '{tail.Latest.Message}'");

    // (a) foreach. The compiler lowers this to EXACTLY the protocol below.
    Console.Write("foreach (struct enumerator, zero alloc): ");
    foreach(LogEvent e in tail) Console.Write($"[{e.Message}] ");
    Console.WriteLine();

    // (b) The same loop, desugared by hand — what foreach actually compiles to:
    Console.Write("manual protocol (what foreach becomes):  ");
    RingBuffer<LogEvent>.Enumerator enumerator = tail.GetEnumerator();
    try
    {
        while (enumerator.MoveNext())
            Console.Write($"[{enumerator.Current.Message}] ");
    }
    finally { enumerator.Dispose(); }
    Console.WriteLine();

    // (c) The interface path: LINQ sees only IEnumerable<T> (struct gets boxed here — the
    //     price of polymorphism, paid only by callers who need it).
    Console.WriteLine($"via LINQ/IEnumerable<T>: newest error = '{tail.Last().Message}'");

    // (d) Fail-fast versioning: mutation during enumeration throws instead of lying.
    try
    {
        foreach (LogEvent _ in tail) tail.Add(CreateEvent("error #{6}", 6));
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"mutation during enumeration : {ex.GetType().Name}: {ex.Message}");
    }

    // (e) Indexers — positional access over the modulo mapping. Logical order: 0 = oldest, ^1 = newest.
    Console.WriteLine($"indexers: oldest tail[0] = '{tail[0].Message}', newest tail[^1] = '{tail[^1].Message}'");

    try { _ = tail[tail.Count]; }
    catch (ArgumentOutOfRangeException)
    {
        Console.WriteLine($"tail[{tail.Count}] with {tail.Count} items : ArgumentOutOfRangeException (fail-fast — no default(T) lies)");
    }
});


// ── helpers ──────────────────────────────────────────────────────────────────────────
static void Section(string title, Action action)
{
    Console.WriteLine();
    Console.WriteLine($"═══ {title} ═══");
    action();
}

LogEvent CreateEvent(string message, double secondsOffset = 0, LogLevel level = LogLevel.Info) => new()
{
    Timestamp = baseTime.AddSeconds(secondsOffset),
    Message = message,
    Level = level,
    Service = "demo",
};