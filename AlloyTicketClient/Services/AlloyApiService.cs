namespace AlloyTicketClient.Services
{
    using AlloyTicketClient.Models;
    using AlloyTicketClient.Models.DTOs;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using System.Collections.Generic;

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

        public async Task<Guid> GetFormIdByObjectId(string objectId)
        {
            if (string.IsNullOrWhiteSpace(objectId))
                return Guid.Empty;

            var apiUrl = $"{GetBaseUrl()}/formfields/object/{objectId}";

            var response = await _client.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
                return Guid.Empty;

            var formId = await response.Content.ReadFromJsonAsync<Guid>();
            return formId;
        }

        public async Task<Guid> GetFormIdByActionId(int? actionId)
        {
            if (actionId == null)
                return Guid.Empty;

            var apiUrl = $"{GetBaseUrl()}/formfields/action/{actionId}";

            var response = await _client.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
                return Guid.Empty;

            var formId = await response.Content.ReadFromJsonAsync<Guid>();
            return formId;
        }

        public async Task<List<PageDto>> GetFormPagesAsync(Guid formId)
        {
            var apiUrl = $"{GetBaseUrl()}/formfields/pages/{formId}";
            var response = await _client.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
                return new List<PageDto>();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

            var json = await response.Content.ReadAsStringAsync();
            var pages = JsonSerializer.Deserialize<List<PageDto>>(json, options);
            return pages ?? new List<PageDto>();
        }

        public async Task<List<DropdownOptionDto>> GetDropdownOptionsAsync(FieldInputDto field)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));

            var apiUrl = $"{GetBaseUrl()}/formfields/dropdown-options";
            var serializedPayload = JsonSerializer.Serialize(field);
            var content = new StringContent(serializedPayload, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(apiUrl, content);
            if (!response.IsSuccessStatusCode)
                return new List<DropdownOptionDto>();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var json = await response.Content.ReadAsStringAsync();
            var dropdownOptions = JsonSerializer.Deserialize<List<DropdownOptionDto>>(json, options);
            return dropdownOptions ?? new List<DropdownOptionDto>();
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
