namespace LoanDisbursement.Domain.Loans;

/// <summary>
/// An International Bank Account Number, validated with the ISO 13616 mod-97 checksum.
/// Stored in normalised form: no spaces, upper case.
/// Catching a mistyped account number here is far cheaper than a rejected payment at the core banking system.
/// </summary>
public sealed record Iban
{
    private const int MinLength = 15; // shortest IBAN in use (Norway)
    private const int MaxLength = 34; // ISO 13616 maximum

    public string Value { get; }

    private Iban(string value)
    {
        Value = value;
    }

    /// <summary>Validates and normalises user input. Spaces are allowed and removed.</summary>
    public static Iban Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new DomainValidationException("Account number is required.");
        }

        var value = string.Concat(input.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();

        if (value.Length < MinLength || value.Length > MaxLength)
        {
            throw new DomainValidationException($"Account number must be an IBAN of {MinLength} to {MaxLength} characters.");
        }

        if (!char.IsAsciiLetterUpper(value[0]) || !char.IsAsciiLetterUpper(value[1])
            || !char.IsAsciiDigit(value[2]) || !char.IsAsciiDigit(value[3]))
        {
            throw new DomainValidationException("Account number must start with a two-letter country code and two check digits.");
        }

        if (!value.All(c => char.IsAsciiLetterUpper(c) || char.IsAsciiDigit(c)))
        {
            throw new DomainValidationException("Account number may contain only letters and digits.");
        }

        if (Mod97(value) != 1)
        {
            throw new DomainValidationException("Account number is not a valid IBAN: the check digits do not match.");
        }

        return new Iban(value);
    }

    /// <summary>
    /// Rebuilds an IBAN that was validated before it was stored. Used only when reading from the database,
    /// so tightening validation later cannot make existing rows unreadable.
    /// </summary>
    public static Iban FromTrusted(string value) => new(value);

    public override string ToString() => Value;

    /// <summary>
    /// ISO 13616: move the first four characters to the end, replace each letter with two digits
    /// (A = 10 ... Z = 35), and take the number modulo 97. The number has up to 68 digits,
    /// so the remainder is computed digit by digit instead of parsing it.
    /// </summary>
    private static int Mod97(string iban)
    {
        var rearranged = iban[4..] + iban[..4];
        var remainder = 0;

        foreach (var c in rearranged)
        {
            remainder = char.IsAsciiDigit(c)
                ? (remainder * 10 + (c - '0')) % 97
                : (remainder * 100 + (c - 'A' + 10)) % 97;
        }

        return remainder;
    }
}
