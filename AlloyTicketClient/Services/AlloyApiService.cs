namespace AlloyTicketClient.Services
{
    using AlloyTicketClient.Models;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class AlloyApiService
    {
        private readonly HttpClient _client;
        private readonly IConfiguration _config;
        private readonly JwtTokenService _jwtTokenService;
        private readonly ILogger<AlloyApiService> _logger;

        public AlloyApiService(HttpClient client, IConfiguration config, JwtTokenService jwtTokenService, ILogger<AlloyApiService> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(bool Success, string Message)> CreateRequestAsync(RequestActionPayload payload)
        {
            try
            {
                SetAuthHeader(payload.Requester_ID);
                var apiUrl = $"{GetBaseUrl()}/request";
                // Serialize and send the RequestActionPayload object directly (no wrapper)
                var serializedPayload = JsonSerializer.Serialize(payload);
                _logger.LogInformation("Sending request to {ApiUrl} with payload: {Payload}", apiUrl, serializedPayload);
                var content = new StringContent(serializedPayload, Encoding.UTF8, "application/json");
                using var response = await _client.PostAsync(apiUrl, content);
                var apiResponse = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Received response: {StatusCode} - {Response}", response.StatusCode, apiResponse);

                if (response.IsSuccessStatusCode)
                    return (true, "Success");

                return (false, $"API Error: {response.StatusCode} - {apiResponse}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in PostAsync");
                return (false, $"Exception occurred in CreateRequestAsync: {ex.Message}");
            }
        }

        private void SetAuthHeader(string requester, string email = null)
        {
            if (string.IsNullOrWhiteSpace(requester))
                throw new ArgumentException("Requester cannot be null or empty.", nameof(requester));
            string jwtToken = _jwtTokenService.GenerateJwtToken(requester, email ?? requester);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
        }

        private string GetBaseUrl()
        {
            var baseUrl = _config.GetSection("AlloyAPI")["BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("AlloyAPI BaseUrl is not configured.");
            return baseUrl;
        }

        public static JsonElement FormatDateTimesInJsonElement(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var dict = new Dictionary<string, object?>();
                foreach (var prop in element.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String && DateTime.TryParse(prop.Value.GetString(), out var dt))
                    {
                        dict[prop.Name] = dt.ToString("yyyy-MM-ddTHH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.Object)
                    {
                        dict[prop.Name] = FormatDateTimesInJsonElement(prop.Value);
                    }
                    else
                    {
                        dict[prop.Name] = prop.Value.Deserialize<object?>();
                    }
                }
                var json = System.Text.Json.JsonSerializer.Serialize(dict);
                return System.Text.Json.JsonDocument.Parse(json).RootElement;
            }
            return element;
        }
    }
}
