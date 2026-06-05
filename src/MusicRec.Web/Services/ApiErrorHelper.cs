using System.Net;
using System.Text.Json;

namespace MusicRec.Web.Services;

internal static class ApiErrorHelper
{
    public static async Task EnsureSuccessAsync(HttpResponseMessage response, string serviceName)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = await BuildMessageAsync(response, serviceName);
        throw new InvalidOperationException(message);
    }

    private static async Task<string> BuildMessageAsync(HttpResponseMessage response, string serviceName)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            return $"{serviceName} request failed with status code {(int)response.StatusCode}.";
        }

        try
        {
            var problem = JsonSerializer.Deserialize<ApiProblemDetails>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (problem is not null)
            {
                if (problem.Errors?.Count > 0)
                {
                    var firstError = problem.Errors
                        .SelectMany(x => x.Value ?? [])
                        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

                    if (!string.IsNullOrWhiteSpace(firstError))
                    {
                        return firstError!;
                    }
                }

                if (!string.IsNullOrWhiteSpace(problem.Detail))
                {
                    return problem.Detail!;
                }

                if (!string.IsNullOrWhiteSpace(problem.Title))
                {
                    return problem.Title!;
                }
            }
        }
        catch
        {
        }

        return content;
    }

    private sealed class ApiProblemDetails
    {
        public string? Title { get; set; }
        public string? Detail { get; set; }
        public HttpStatusCode? Status { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
