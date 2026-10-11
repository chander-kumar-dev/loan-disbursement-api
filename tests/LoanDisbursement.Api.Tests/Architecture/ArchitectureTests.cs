using System.Reflection;
using LoanDisbursement.Application.Abstractions;
using LoanDisbursement.Domain.Loans;
using LoanDisbursement.Infrastructure.Persistence;
using NetArchTest.Rules;

namespace LoanDisbursement.Api.Tests.Architecture;

/// <summary>
/// The README says dependencies point inward. These tests make that a rule the build enforces,
/// rather than a diagram that drifts.
/// </summary>
public class ArchitectureTests
{
    private const string DomainNamespace = "LoanDisbursement.Domain";
    private const string ApplicationNamespace = "LoanDisbursement.Application";
    private const string InfrastructureNamespace = "LoanDisbursement.Infrastructure";
    private const string ApiNamespace = "LoanDisbursement.Api";

    private static readonly Assembly DomainAssembly = typeof(Loan).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(IAppDbContext).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(AppDbContext).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    [Fact]
    public void Domain_DependsOnNoOtherLayer_AndNoFramework()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationNamespace,
                InfrastructureNamespace,
                ApiNamespace,
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        AssertRuleHolds(result);
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrApi()
    {
        // Deliberate exception: Application DOES depend on EF Core (IAppDbContext exposes DbSet<T>),
        // as explained in the README. It must still never see SQL Server or the web layer.
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace, "Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore.SqlServer")
            .GetResult();

        AssertRuleHolds(result);
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApi()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        AssertRuleHolds(result);
    }

    [Fact]
    public void Controllers_DoNotDependOnInfrastructure()
    {
        // Program.cs wires Infrastructure in (it is the composition root); controllers only talk to handlers.
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace($"{ApiNamespace}.Controllers")
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        AssertRuleHolds(result);
    }

    [Fact]
    public void CommandAndQueryHandlers_AreNamedHandler()
    {
        var handlers = ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.GetInterfaces().Any(i => i.IsGenericType
                            && (i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)
                                || i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))))
            .ToList();

        Assert.NotEmpty(handlers);
        Assert.All(handlers, handler => Assert.EndsWith("Handler", handler.Name));
    }

    [Fact]
    public void DomainTypes_HaveNoPublicSetters()
    {
        // State changes must go through domain methods, which enforce the rules.
        // Exceptions are excluded: System.Exception itself has settable properties such as HelpLink.
        var publicSetters = DomainAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith(DomainNamespace, StringComparison.Ordinal) == true
                        && !typeof(Exception).IsAssignableFrom(t))
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod?.IsPublic == true)
                .Select(p => $"{t.Name}.{p.Name}"))
            .ToList();

        Assert.Empty(publicSetters);
    }

    private static void AssertRuleHolds(TestResult result)
    {
        var offenders = result.FailingTypeNames ?? Array.Empty<string>();
        Assert.True(result.IsSuccessful, "Dependency rule broken by: " + string.Join(", ", offenders));
    }
}
