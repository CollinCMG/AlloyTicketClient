namespace AlloyTicketClient.Services
{
    using AlloyTicketClient.Models;
    using Microsoft.Extensions.Configuration;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using System;

    public class AlloyApiService
    {
        private readonly HttpClient _client;
        private readonly IConfiguration _config;
        private readonly JwtTokenService _jwtTokenService;

        public AlloyApiService(HttpClient client, IConfiguration config, JwtTokenService jwtTokenService)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
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

        public async Task<(bool Success, string Message)> PostAsync<T>(T payload)
        {
            try
            {
                var apiUrl = $"{GetBaseUrl()}/NewHire";
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                using var response = await _client.PostAsync(apiUrl, content);
                string apiResponse = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return (true, "Success");

                try
                {
                    var respObj = System.Text.Json.JsonSerializer.Deserialize<AlloyApiResponse>(apiResponse);
                    if (respObj == null)
                        return (false, "Error communicating with Alloy. Response object is NULL.");
                    if (respObj.Status != 200)
                        return (false, $"Unsuccessful. ({respObj.Status}) {respObj.Error}");
                }
                catch (Exception ex)
                {
                    return (false, $"Error parsing Alloy API response: {ex.Message}");
                }

                return (false, "Unknown error.");
            }
            catch (Exception ex)
            {
                return (false, $"Exception occurred in PostAsync: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteAsync(string endpoint, string requester, string email = null)
        {
            try
            {
                SetAuthHeader(requester, email);
                var apiUrl = $"{GetBaseUrl()}/{endpoint.TrimStart('/')}";
                using var response = await _client.DeleteAsync(apiUrl);
                string apiResponse = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return (true, "Success");

                try
                {
                    var respObj = System.Text.Json.JsonSerializer.Deserialize<AlloyApiResponse>(apiResponse);
                    if (respObj == null)
                        return (false, "Error communicating with Alloy. Response object is NULL.");
                    if (respObj.Status != 200)
                        return (false, $"Unsuccessful. ({respObj.Status}) {respObj.Error}");
                }
                catch (Exception ex)
                {
                    return (false, $"Error parsing Alloy API response: {ex.Message}");
                }

                return (false, "Unknown error.");
            }
            catch (Exception ex)
            {
                return (false, $"Exception occurred in DeleteAsync: {ex.Message}");
            }
        }
    }
}
