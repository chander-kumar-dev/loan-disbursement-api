using LoanDisbursement.Domain.Loans;
using Microsoft.Extensions.Options;

namespace LoanDisbursement.Api.Configuration;

/// <summary>
/// Loan product limits, bound from the "LoanProduct" section of configuration.
/// This is the only place the limits are defined; the Domain enforces them through <see cref="LoanLimits"/>.
/// </summary>
public sealed class LoanProductOptions
{
    public const string SectionName = "LoanProduct";

    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public int MinTenureMonths { get; set; }
    public int MaxTenureMonths { get; set; }

    public bool IsValid =>
        MinAmount > 0
        && MaxAmount >= MinAmount
        && MinTenureMonths >= 1
        && MaxTenureMonths >= MinTenureMonths;

    public LoanLimits ToLoanLimits() => new(MinAmount, MaxAmount, MinTenureMonths, MaxTenureMonths);
}

public static class LoanProductServiceCollectionExtensions
{
    /// <summary>
    /// Binds and validates the loan product limits. ValidateOnStart makes the app refuse to start
    /// with a missing or invalid section, instead of failing on the first loan request.
    /// </summary>
    public static IServiceCollection AddLoanProduct(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LoanProductOptions>()
            .Bind(configuration.GetSection(LoanProductOptions.SectionName))
            .Validate(
                options => options.IsValid,
                $"'{LoanProductOptions.SectionName}' is missing or invalid: need MinAmount > 0, MaxAmount >= MinAmount, "
                + "MinTenureMonths >= 1 and MaxTenureMonths >= MinTenureMonths.")
            .ValidateOnStart();

        services.AddSingleton(provider => provider.GetRequiredService<IOptions<LoanProductOptions>>().Value.ToLoanLimits());

        return services;
    }
}
