using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace VirtualAtmClient.Helpers;

public class RequestHandler : DelegatingHandler
{
    private readonly ILogger<RequestHandler> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RequestHandler(HttpMessageHandler innerHandler, ILogger<RequestHandler> logger) : base(innerHandler)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content != null)
        {
            if (request.Content.Headers?.ContentType?.MediaType == "application/json")
            {
                JsonElement json = JsonSerializer.Deserialize<JsonElement>(await request.Content.ReadAsStringAsync());
                _logger.LogTrace($"{request}\n{JsonSerializer.Serialize(json, _jsonOptions)}");
            }
            else
            {
                _logger.LogTrace(await request.Content.ReadAsStringAsync());
            }
        }
        else
        {
            _logger.LogTrace($"Request: {request}");
        }

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        if (response.Content != null)
        {
            if (response.Content.Headers?.ContentType?.MediaType == "application/json")
            {
                JsonElement json = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
                _logger.LogTrace($"{response}\n{JsonSerializer.Serialize(json, _jsonOptions)}");
            }
            else
            {
                _logger.LogTrace(await response.Content.ReadAsStringAsync());
            }
        }
        else
        {
            _logger.LogTrace($"Response: {response}");
        }

        return response;
    }
}
