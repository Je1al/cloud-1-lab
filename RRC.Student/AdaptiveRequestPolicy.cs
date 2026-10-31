using RRC.Abstractions;

namespace RRC.Student;

/// <summary>
/// A conservative, self-tuning policy for a server where a timeout does not
/// cancel the work already accepted by the server.
/// </summary>
public sealed class AdaptiveRequestPolicy : IRequestPolicy
{
    // A generous initial value is deliberate: a timeout cannot cancel server
    // work, so an early retry would create duplicate work during warm-up.
    private TimeSpan currentTimeout = TimeSpan.FromSeconds(1);
    private TimeSpan longestSuccessfulAttempt = TimeSpan.Zero;

    private static readonly TimeSpan MaximumTimeout = TimeSpan.FromSeconds(60);

    public TimeSpan GetTimeout(RequestContext context, Random random) => currentTimeout;

    public RetryDecision OnResult(RequestObservation observation, Random random)
    {
        if (observation.Outcome == RequestOutcome.Success)
        {
            // Keep a high-water mark instead of aggressively shrinking the
            // deadline: response time itself is not increased by a larger
            // timeout, but a too-small deadline turns a valid reply into a
            // failed request.
            if (observation.Elapsed > longestSuccessfulAttempt)
                longestSuccessfulAttempt = observation.Elapsed;

            var safeDeadline = Multiply(longestSuccessfulAttempt, 2);
            if (safeDeadline > currentTimeout)
                currentTimeout = Min(safeDeadline, MaximumTimeout);

            return RetryDecision.NoRetry();
        }

        // New requests may encounter the same congestion. Increase the
        // deadline promptly, rather than adding a duplicate attempt while the
        // timed-out one is still executing on the server.
        currentTimeout = Min(Multiply(currentTimeout, 2m), MaximumTimeout);
        return RetryDecision.NoRetry();
    }

    private static TimeSpan Multiply(TimeSpan value, decimal factor) =>
        TimeSpan.FromTicks((long)Math.Min(value.Ticks * factor, TimeSpan.MaxValue.Ticks));

    private static TimeSpan Min(TimeSpan left, TimeSpan right) => left <= right ? left : right;
}
