using System.Text.Json.Serialization;
using LoanDisbursement.Api.Errors;
using LoanDisbursement.Application;
using LoanDisbursement.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHealthChecks();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // Built-in OpenAPI document: /openapi/v1.json
    app.MapOpenApi();

    // Swagger UI at /swagger, reading the built-in document
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Loan Disbursement API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Loan Disbursement API";
    });

    // Development-only route map. Never expose this outside Development.
    app.MapGet("/routes", (IEnumerable<EndpointDataSource> sources) =>
        sources.SelectMany(s => s.Endpoints)
               .OfType<RouteEndpoint>()
               .Select(e => new
               {
                   Methods = e.Metadata.OfType<HttpMethodMetadata>()
                              .FirstOrDefault()?.HttpMethods ?? new[] { "ANY" },
                   Route = e.RoutePattern.RawText
               }))
       .ExcludeFromDescription();

    // Root URL opens Swagger
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.MapControllers();

// Liveness: the process is up. Runs no dependency checks, so a database outage
// does not make the orchestrator restart healthy instances.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

// Readiness: dependencies are reachable. A load balancer should stop sending traffic while this fails.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(HealthCheckTags.Ready)
});

// Kept for existing callers; behaves like /health/live.
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });

app.Run();

public partial class Program { }