using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace LogPulse.Parsing;

/// <summary>
/// The fallback: any line becomes an event with the whole line as its message.
/// Even here we parse a leading ISO timestamp and a severity word when present —
/// free structure beats no structure.
/// </summary>
public sealed class PlainTextParser
{
    private const int MinTimestampLength = 19;
    private const int MaxWordsToScan = 4;
    private const string Decorations = "[](){}<>:;,.!-";

    /// <summary>
    /// Turns any non-empty line into an event, parsing a leading timestamp and a severity word when present.
    /// </summary>
    // EXAMPLE — input line : event:
    //     "2026-07-17T12:00:01Z ERROR disk full"
    //         : Timestamp 12:00:01 (from the line), Level Error, Message "ERROR disk full"
    //     "[warn] slow response"
    //         : Timestamp = ReceivedAt (no leading time), Level Warning
    //     "hello"
    //         : Level Info, Message "hello"
    public bool TryParse(ReadOnlySpan<char> line, in ParseContext context, [NotNullWhen(true)] out LogEvent? logEvent)
    {
        ReadOnlySpan<char> text = line.Trim();
        if (text.IsEmpty)
        {
            logEvent = null;
            return false;
        }

        // Step 1 — timestamp: from the line when it starts with one;
        //          otherwise the arrival time is the honest best value.
        if (TryParseLeadingTimestamp(text, out DateTimeOffset timestamp, out ReadOnlySpan<char> rest))
            text = rest;
        else
            timestamp = context.ReceivedAt;

        // Step 2 — severity: parsed from the first words; Info when absent.
        LogLevel level = ParseLevelOrDefault(text);

        // Step 3 — the event. Message keeps everything after the timestamp,
        //          severity word included: the original text is evidence, and
        //          we never rewrite evidence.
        logEvent = new LogEvent
        {
            Timestamp = timestamp,
            Message = text.ToString(),
            Level = level,
            Service = context.DefaultService
        };

        return true;
    }

    /// <summary>
    /// Parses a leading ISO-8601 timestamp ("2026-07-17T12:00:01Z rest…").
    /// On success, <paramref name="rest"/> is the text after it.
    /// </summary>
    private static bool TryParseLeadingTimestamp(
        ReadOnlySpan<char> text, out DateTimeOffset timestamp, out ReadOnlySpan<char> rest)
    {
        int firstSpace = text.IndexOf(' ');
        if (firstSpace >= MinTimestampLength && char.IsAsciiDigit(text[0])
            && DateTimeOffset.TryParse(text[..firstSpace], CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out timestamp))
        {
            rest = text[(firstSpace + 1)..].TrimStart();
            return true;
        }

        timestamp = default;
        rest = default;
        return false;
    }

    // EXAMPLE — "[ERROR]" → Error;  "warn:" : Warning;  "the error was ours" :
    //     Error too (word 2 of 4 scanned); "all good here today" : Info.
    private static LogLevel ParseLevelOrDefault(ReadOnlySpan<char> text)
    {
        for (int i = 0; i < MaxWordsToScan && !text.IsEmpty; i++)
        {
            int space = text.IndexOf(' ');
            ReadOnlySpan<char> candidate = (space < 0 ? text : text[..space]).Trim(Decorations);

            if (!candidate.IsEmpty && SeverityKeywords.TryParse(candidate, out LogLevel level))
                return level;

            // Move past this word (and any repeated spaces) — or stop:
            // no space left means that was the last word.
            text = space < 0 ? default : text[(space + 1)..].TrimStart();
        }

        return LogLevel.Info;
    }
}
