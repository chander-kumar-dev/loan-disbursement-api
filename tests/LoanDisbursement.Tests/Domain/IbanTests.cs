using LoanDisbursement.Domain;
using LoanDisbursement.Domain.Loans;

namespace LoanDisbursement.Tests.Domain;

public class IbanTests
{
    [Theory]
    [InlineData("PK36SCBL0000001123456702")] // Pakistan
    [InlineData("GB82WEST12345698765432")]   // United Kingdom
    [InlineData("SA0380000000608010167519")] // Saudi Arabia
    public void ValidIban_IsAccepted(string input)
    {
        var iban = Iban.Create(input);

        Assert.Equal(input, iban.Value);
    }

    [Fact]
    public void Input_IsNormalised_SpacesRemovedAndUpperCased()
    {
        var iban = Iban.Create(" pk36 scbl 0000 0011 2345 6702 ");

        Assert.Equal("PK36SCBL0000001123456702", iban.Value);
    }

    [Fact]
    public void WrongCheckDigits_AreRejected()
    {
        // Last digit changed from 2 to 3: a typical typo, caught by the mod-97 checksum.
        var error = Assert.Throws<DomainValidationException>(() => Iban.Create("PK36SCBL0000001123456703"));

        Assert.Contains("check digits", error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("PK36SCBL")]                               // too short
    [InlineData("PK36SCBL00000011234567020000000000000")] // too long (37 characters)
    [InlineData("1236SCBL0000001123456702")]               // country code must be letters
    [InlineData("PKXXSCBL0000001123456702")]               // check digits must be digits
    [InlineData("PK36SCBL-000001123456702")]               // symbols are not allowed
    public void MalformedInput_IsRejected(string? input)
    {
        Assert.Throws<DomainValidationException>(() => Iban.Create(input));
    }

    [Fact]
    public void SameValue_IsEqual()
    {
        Assert.Equal(Iban.Create("GB82 WEST 1234 5698 7654 32"), Iban.Create("GB82WEST12345698765432"));
    }
}
