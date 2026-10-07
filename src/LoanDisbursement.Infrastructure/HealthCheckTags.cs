namespace LoanDisbursement.Infrastructure;

public static class HealthCheckTags
{
    /// <summary>
    /// Checks with this tag run on /health/ready: dependencies the API needs before it can take traffic.
    /// </summary>
    public const string Ready = "ready";
}
