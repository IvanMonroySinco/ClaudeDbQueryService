using System.Text;
using System.Text.Json;
using MCPServer.Configuration;
using MCPServer.Models.Claude;
using Microsoft.Extensions.Options;

namespace MCPServer.Services;

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
        try
        {
            _logger.LogDebug("Sending request to Claude API: {@Request}", request);

            var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
            _logger.LogDebug("Sending JSON to Claude API: {JsonContent}", jsonContent);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("", content, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Claude API response: {Response}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Claude API returned error: {StatusCode} - {Content}",
                    response.StatusCode, responseContent);

                var errorResponse = JsonSerializer.Deserialize<ClaudeErrorResponse>(responseContent, _jsonOptions);
                throw new HttpRequestException($"Claude API error: {errorResponse?.Error?.Message ?? "Unknown error"}");
            }

            var claudeResponse = JsonSerializer.Deserialize<ClaudeResponse>(responseContent, _jsonOptions);
            if (claudeResponse == null)
            {
                throw new InvalidOperationException("Failed to deserialize Claude API response");
            }

            _logger.LogInformation("Claude API request completed successfully. Tokens used: {InputTokens}/{OutputTokens}",
                claudeResponse.Usage?.InputTokens, claudeResponse.Usage?.OutputTokens);

            return claudeResponse;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Claude API request timed out");
            throw new TimeoutException("Claude API request timed out", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Claude API");
            throw;
        }
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