using System.Text;
using System.Text.Json;
using ClaudeDbQueryService.Core.Application.Configuration;
using ClaudeDbQueryService.Infrastructure.External.Models;
using Microsoft.Extensions.Options;

namespace ClaudeDbQueryService.Infrastructure.External.ApiServices;

public class ClaudeApiService : IClaudeApiService
{
    private readonly HttpClient _httpClient;
    private readonly ClaudeOptions _options;
    private readonly ILogger<ClaudeApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ClaudeApiService(
        HttpClient httpClient,
        IOptions<ClaudeOptions> options,
        ILogger<ClaudeApiService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        // Debug logging for configuration
        _logger.LogDebug("ClaudeApiService configuration loaded:");
        _logger.LogDebug("- ApiUrl: {ApiUrl}", _options.ApiUrl);
        _logger.LogDebug("- Model: {Model}", _options.Model);
        _logger.LogDebug("- MaxTokens: {MaxTokens}", _options.MaxTokens);
        _logger.LogDebug("- TimeoutSeconds: {TimeoutSeconds}", _options.TimeoutSeconds);
        _logger.LogDebug("- ApiKey configured: {HasApiKey}", !string.IsNullOrWhiteSpace(_options.ApiKey));
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogDebug("- ApiKey length: {ApiKeyLength}", _options.ApiKey.Length);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_options.ApiUrl);

        // Only add API key if it's not empty
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);
        }

        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<ClaudeResponse> SendMessageAsync(ClaudeRequest request, CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        var delay = TimeSpan.FromSeconds(_options.RetryDelaySeconds);

        while (attempt <= _options.MaxRetryAttempts)
        {
            try
            {
                _logger.LogDebug("Sending request to Claude API (attempt {Attempt}/{MaxAttempts}): {@Request}",
                    attempt + 1, _options.MaxRetryAttempts + 1, request);

                var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
                _logger.LogDebug("Sending JSON to Claude API: {JsonContent}", jsonContent);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("", content, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogDebug("Claude API response: {Response}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var claudeResponse = JsonSerializer.Deserialize<ClaudeResponse>(responseContent, _jsonOptions);
                    if (claudeResponse == null)
                    {
                        throw new InvalidOperationException("Failed to deserialize Claude API response");
                    }

                    _logger.LogInformation("Claude API request completed successfully on attempt {Attempt}. Tokens used: {InputTokens}/{OutputTokens}",
                        attempt + 1, claudeResponse.Usage?.InputTokens, claudeResponse.Usage?.OutputTokens);

                    return claudeResponse;
                }

                // Check if we should retry based on status code
                var shouldRetry = ShouldRetryForStatusCode(response.StatusCode);

                _logger.LogWarning("Claude API returned error: {StatusCode} - {Content}. Should retry: {ShouldRetry}",
                    response.StatusCode, responseContent, shouldRetry);

                if (!shouldRetry || attempt >= _options.MaxRetryAttempts)
                {
                    var errorResponse = JsonSerializer.Deserialize<ClaudeErrorResponse>(responseContent, _jsonOptions);
                    throw new HttpRequestException($"Claude API error: {errorResponse?.Error?.Message ?? "Unknown error"}");
                }

                // Wait before retrying with exponential backoff
                if (attempt < _options.MaxRetryAttempts)
                {
                    _logger.LogInformation("Retrying Claude API request in {DelaySeconds} seconds (attempt {Attempt}/{MaxAttempts})",
                        delay.TotalSeconds, attempt + 2, _options.MaxRetryAttempts + 1);

                    await Task.Delay(delay, cancellationToken);
                    delay = TimeSpan.FromSeconds(delay.TotalSeconds * _options.RetryMultiplier);
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Claude API request timed out on attempt {Attempt}", attempt + 1);

                if (attempt >= _options.MaxRetryAttempts)
                {
                    throw new TimeoutException("Claude API request timed out after all retry attempts", ex);
                }
            }
            catch (Exception ex) when (!(ex is HttpRequestException))
            {
                _logger.LogError(ex, "Unexpected error calling Claude API on attempt {Attempt}", attempt + 1);

                if (attempt >= _options.MaxRetryAttempts)
                {
                    throw;
                }
            }

            attempt++;
        }

        throw new InvalidOperationException("Exceeded maximum retry attempts for Claude API");
    }

    private static bool ShouldRetryForStatusCode(System.Net.HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            System.Net.HttpStatusCode.ServiceUnavailable => true, // 503 - Overloaded
            System.Net.HttpStatusCode.TooManyRequests => true,    // 429 - Rate limited
            System.Net.HttpStatusCode.BadGateway => true,         // 502 - Gateway error
            System.Net.HttpStatusCode.RequestTimeout => true,     // 408 - Request timeout
            System.Net.HttpStatusCode.InternalServerError => true, // 500 - Internal server error
            _ => false
        };
    }

    public async Task<ClaudeResponse> ProcessQueryAsync(string query, string? systemPrompt = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be empty", nameof(query));
        }

        var request = new ClaudeRequest
        {
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            Temperature = _options.Temperature,
            TopP = _options.TopP,
            Messages = new List<ClaudeMessage>
            {
                new()
                {
                    Role = "user",
                    Content = query
                }
            }
        };

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            request.System = systemPrompt;
        }

        return await SendMessageAsync(request, cancellationToken);
    }

    public async Task<ClaudeResponse> ProcessQueryWithToolsAsync(string query, List<ClaudeTool> tools, string? systemPrompt = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be empty", nameof(query));
        }

        var request = new ClaudeRequest
        {
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            Temperature = _options.Temperature,
            TopP = _options.TopP,
            Tools = tools,
            ToolChoice = new { type = "auto" },
            Messages = new List<ClaudeMessage>
            {
                new()
                {
                    Role = "user",
                    Content = query
                }
            }
        };

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            request.System = systemPrompt;
        }

        return await SendMessageAsync(request, cancellationToken);
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking Claude API health");

            // Check if API key is configured
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _logger.LogWarning("Claude API key is not configured");
                return false;
            }

            var healthCheckRequest = new ClaudeRequest
            {
                Model = _options.Model,
                MaxTokens = 10,
                Messages = new List<ClaudeMessage>
                {
                    new()
                    {
                        Role = "user",
                        Content = "Hello"
                    }
                }
                // No incluir propiedades opcionales como Temperature, TopP, System, Stream
            };

            var response = await SendMessageAsync(healthCheckRequest, cancellationToken);
            var isHealthy = response.Content?.Any() == true;

            _logger.LogDebug("Claude API health check result: {IsHealthy}", isHealthy);
            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude API health check failed");
            return false;
        }
    }
}