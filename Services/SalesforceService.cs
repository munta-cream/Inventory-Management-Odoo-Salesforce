using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Inventory_Management_Requirements.Services
{
    public class SalesforceService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SalesforceService> _logger;
        private string _accessToken;
        private string _instanceUrl;

        public SalesforceService(HttpClient httpClient, IConfiguration configuration, ILogger<SalesforceService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> AuthenticateAsync()
        {
            var clientId = _configuration["Salesforce:ClientId"];
            var clientSecret = _configuration["Salesforce:ClientSecret"];
            var username = _configuration["Salesforce:Username"];
            var password = _configuration["Salesforce:Password"];
            var securityToken = _configuration["Salesforce:SecurityToken"];

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password + securityToken)
            });

            var instanceUrlConfig = _configuration["Salesforce:InstanceUrl"];
            var tokenUrl = $"{instanceUrlConfig}/services/oauth2/token";

            var response = await _httpClient.PostAsync(tokenUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent);
                _accessToken = authResponse.access_token;
                _instanceUrl = authResponse.instance_url;
                _logger.LogInformation("Successfully authenticated with Salesforce.");
                _logger.LogInformation($"Authentication response: {responseContent}");
                return true;
            }
            else
            {
                _logger.LogError($"Salesforce authentication failed. Status: {response.StatusCode}, Response: {responseContent}");
                return false;
            }
        }

        public async Task<string> CreateAccountAsync(string name)
        {
            if (string.IsNullOrEmpty(_accessToken)) return null;

            var account = new { Name = name };
            var json = JsonSerializer.Serialize(account);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            _logger.LogInformation($"Creating Account with payload: {json}");

            var response = await _httpClient.PostAsync($"{_instanceUrl}/services/data/v59.0/sobjects/Account", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var createResponse = JsonSerializer.Deserialize<CreateResponse>(responseContent);
                _logger.LogInformation($"Successfully created Account with ID: {createResponse.id}");
                _logger.LogInformation($"Create Account response: {responseContent}");
                return createResponse.id;
            }
            else
            {
                _logger.LogError($"Failed to create Account. Status: {response.StatusCode}, Response: {responseContent}");
                return null;
            }
        }

        public async Task<bool> CreateContactAsync(string accountId, string firstName, string lastName, string email, string phone, string street, string city, string state, string postalCode, string country)
        {
            if (string.IsNullOrEmpty(_accessToken)) return false;

            var contact = new
            {
                AccountId = accountId,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Phone = phone,
                MailingStreet = street,
                MailingCity = city,
                MailingState = state,
                MailingPostalCode = postalCode,
                MailingCountry = country
            };
            var json = JsonSerializer.Serialize(contact);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            _logger.LogInformation($"Creating Contact with payload: {json}");

            var response = await _httpClient.PostAsync($"{_instanceUrl}/services/data/v59.0/sobjects/Contact", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully created Contact linked to Account.");
                _logger.LogInformation($"Create Contact response: {responseContent}");
                return true;
            }
            else
            {
                _logger.LogError($"Failed to create Contact. Status: {response.StatusCode}, Response: {responseContent}");
                return false;
            }
        }

        private class AuthResponse
        {
            public string access_token { get; set; }
            public string instance_url { get; set; }
        }

        private class CreateResponse
        {
            public string id { get; set; }
        }
    }
}
