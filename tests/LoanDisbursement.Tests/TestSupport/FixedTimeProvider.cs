namespace LoanDisbursement.Tests.TestSupport;

/// <summary>
/// A clock the test controls, so timestamps are predictable.
/// </summary>
public sealed class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset now)
    {
        _now = now;
    }

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan duration) => _now = _now.Add(duration);
}
