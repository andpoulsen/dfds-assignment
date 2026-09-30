namespace Dfds.TruckPlans.Domain;

/// <summary>A half-open time interval [Start, End).</summary>
public readonly record struct TimeWindow
{
    public TimeWindow(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
            throw new ArgumentException("End must be after start.", nameof(end));

        Start = start;
        End = end;
    }

    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }
    public TimeSpan Duration => End - Start;

    public bool Contains(DateTimeOffset instant) => instant >= Start && instant < End;

    public bool Overlaps(TimeWindow other) => Start < other.End && other.Start < End;

    /// <summary>The overlapping part of two windows, or null if they don't overlap.</summary>
    public TimeWindow? Intersect(TimeWindow other) =>
        Overlaps(other)
            ? new TimeWindow(Max(Start, other.Start), Min(End, other.End))
            : null;

    private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;
    private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;
}
