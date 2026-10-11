using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoanDisbursement.Api.Tests.Infrastructure;

/// <summary>
/// JSON settings matching the API: camelCase names and enums as strings.
/// </summary>
public static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(Options);
        return value ?? throw new InvalidOperationException($"Response body was empty; expected {typeof(T).Name}.");
    }
}
