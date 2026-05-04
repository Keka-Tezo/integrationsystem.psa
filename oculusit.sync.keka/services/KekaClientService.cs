using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using oculusit.sync.keka.configurations;
using oculusit.sync.keka.modules;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace oculusit.sync.keka.services;

public sealed class KekaClientService(
    IHttpClientFactory httpClientFactory,
    IOptions<KekaConfiguration> config,
    IKekaTokenService tokenService,
    ILogger<KekaClientService> logger) : IKekaClientService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(nameof(KekaClientService));
    private readonly KekaConfiguration _config = config.Value;
    private readonly ILogger<KekaClientService> _logger = logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private async Task SetAuthHeaderAsync(CancellationToken cancellationToken)
    {
        var token = await tokenService.GetAccessTokenAsync(cancellationToken);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task RefreshAuthHeaderAsync(CancellationToken cancellationToken)
    {
        var token = await tokenService.RefreshAccessTokenAsync(cancellationToken);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    // All Keka PSA client endpoints live under /api/v1/psa/clients
    private Uri BuildUri(string relativePath) =>
        new(new Uri(_config.ApiBaseUrl), $"/api/v1{relativePath}");

    public async Task<KekaClient?> GetClientAsync(string clientId, CancellationToken cancellationToken = default)
    {
        await SetAuthHeaderAsync(cancellationToken);

        var uri = BuildUri($"/psa/clients/{clientId}");
        _logger.LogDebug("Fetching Keka client {ClientId}", clientId);

        var response = await _httpClient.GetAsync(uri, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Received 401 fetching Keka client {ClientId}. Refreshing token.", clientId);
            await RefreshAuthHeaderAsync(cancellationToken);
            response = await _httpClient.GetAsync(uri, cancellationToken);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Keka client {ClientId} not found.", clientId);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to get Keka client {ClientId}. StatusCode: {StatusCode}, Body: {Body}",
                clientId, response.StatusCode, errorBody);
            throw new HttpRequestException(
                $"Keka GET /psa/clients/{clientId} failed ({(int)response.StatusCode}): {errorBody}",
                null, response.StatusCode);
        }
        var envelope = await response.Content.ReadFromJsonAsync<KekaDataResponse<KekaClient>>(_jsonOptions, cancellationToken);
        _logger.LogInformation("Successfully fetched Keka client {ClientId}", clientId);
        return envelope?.Data;
    }

    public async Task<IReadOnlyList<KekaClient>> GetAllClientsAsync(CancellationToken cancellationToken = default)
    {
        await SetAuthHeaderAsync(cancellationToken);

        var uri = BuildUri("/psa/clients");
        _logger.LogDebug("Fetching all Keka clients.");

        var response = await _httpClient.GetAsync(uri, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Received 401 fetching all Keka clients. Refreshing token.");
            await RefreshAuthHeaderAsync(cancellationToken);
            response = await _httpClient.GetAsync(uri, cancellationToken);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to fetch all Keka clients. StatusCode: {StatusCode}, Body: {Body}",
                response.StatusCode, errorBody);
            throw new HttpRequestException(
                $"Keka GET /psa/clients failed ({(int)response.StatusCode}): {errorBody}",
                null, response.StatusCode);
        }

        var envelope = await response.Content
            .ReadFromJsonAsync<KekaDataListResponse<KekaClient>>(_jsonOptions, cancellationToken);

        var clients = envelope?.Data ?? [];
        _logger.LogInformation("Fetched {Count} Keka clients.", clients.Count);
        return clients;
    }

    public async Task<KekaClient> CreateClientAsync(KekaClientRequest request, CancellationToken cancellationToken = default)
    {
        await SetAuthHeaderAsync(cancellationToken);

        var uri = BuildUri("/psa/clients");
        _logger.LogDebug("Creating Keka client with name {Name}", request.Name);

        var response = await _httpClient.PostAsJsonAsync(uri, request, _jsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Received 401 creating Keka client. Refreshing token.");
            await RefreshAuthHeaderAsync(cancellationToken);
            response = await _httpClient.PostAsJsonAsync(uri, request, _jsonOptions, cancellationToken);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to create Keka client. StatusCode: {StatusCode}, Body: {Body}",
                response.StatusCode, errorBody);
            throw new HttpRequestException(
                $"Keka POST /psa/clients failed ({(int)response.StatusCode}): {errorBody}",
                null, response.StatusCode);
        }

        // Keka wraps created object in { "data": { ... } }
        var envelope = await response.Content.ReadFromJsonAsync<KekaDataResponse<KekaClient>>(_jsonOptions, cancellationToken);
        var result = envelope?.Data
            ?? throw new InvalidOperationException("Keka create client response could not be deserialized.");

        _logger.LogInformation("Successfully created Keka client {ClientId}", result.Id);
        return result;
    }

    public async Task<KekaClient> UpdateClientAsync(string clientId, KekaClientRequest request, CancellationToken cancellationToken = default)
    {
        await SetAuthHeaderAsync(cancellationToken);

        var uri = BuildUri($"/psa/clients/{clientId}");
        _logger.LogDebug("Updating Keka client {ClientId}", clientId);

        var response = await _httpClient.PutAsJsonAsync(uri, request, _jsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Received 401 updating Keka client {ClientId}. Refreshing token.", clientId);
            await RefreshAuthHeaderAsync(cancellationToken);
            response = await _httpClient.PutAsJsonAsync(uri, request, _jsonOptions, cancellationToken);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to update Keka client {ClientId}. StatusCode: {StatusCode}, Body: {Body}",
                clientId, response.StatusCode, errorBody);
            throw new HttpRequestException(
                $"Keka PUT /psa/clients/{clientId} failed ({(int)response.StatusCode}): {errorBody}",
                null, response.StatusCode);
        }

        // Keka wraps updated object in { "data": { ... } }
        var envelope = await response.Content.ReadFromJsonAsync<KekaDataResponse<KekaClient>>(_jsonOptions, cancellationToken);
        var result = envelope?.Data
            ?? throw new InvalidOperationException("Keka update client response could not be deserialized.");

        _logger.LogInformation("Successfully updated Keka client {ClientId}", clientId);
        return result;
    }
}