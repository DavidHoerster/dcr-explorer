using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DcrDetailBlazor.Models;
using Microsoft.Identity.Web;

namespace DcrDetailBlazor.Services;

public class AzureArmService(
    IHttpClientFactory httpClientFactory,
    ITokenAcquisition tokenAcquisition,
    IConfiguration configuration,
    ILogger<AzureArmService> logger) : IAzureArmService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string[] _scopes = configuration
        .GetSection("DownstreamApis:AzureManagement:Scopes")
        .Get<string[]>() ?? ["https://management.azure.com/user_impersonation"];

    public async Task<IReadOnlyList<SubscriptionInfo>> GetSubscriptionsAsync(CancellationToken cancellationToken = default) =>
        await GetPagedAsync<SubscriptionInfo>(
            "/subscriptions?api-version=2022-12-01",
            "subscriptions",
            cancellationToken);

    public async Task<IReadOnlyList<WorkspaceInfo>> GetWorkspacesAsync(string subscriptionId, CancellationToken cancellationToken = default) =>
        await GetPagedAsync<WorkspaceInfo>(
            $"/subscriptions/{subscriptionId}/providers/Microsoft.OperationalInsights/workspaces?api-version=2023-09-01",
            "Log Analytics workspaces",
            cancellationToken);

    public async Task<IReadOnlyList<DataCollectionRuleResource>> GetDataCollectionRulesAsync(string subscriptionId, CancellationToken cancellationToken = default) =>
        await GetPagedAsync<DataCollectionRuleResource>(
            $"/subscriptions/{subscriptionId}/providers/Microsoft.Insights/dataCollectionRules?api-version=2023-03-11",
            "data collection rules",
            cancellationToken);

    public async Task<IReadOnlyList<DataConnectorResource>> GetDataConnectorsAsync(WorkspaceInfo workspace, CancellationToken cancellationToken = default) =>
        await GetPagedAsync<DataConnectorResource>(
            $"{workspace.Id}/providers/Microsoft.SecurityInsights/dataConnectors?api-version=2025-03-01",
            "Sentinel data connectors",
            cancellationToken);

    public async Task<IReadOnlyList<TableUsageRecord>> GetUsageAsync(WorkspaceInfo workspace, CancellationToken cancellationToken = default)
    {
        var query = """
            Usage
            | where TimeGenerated >= ago(30d)
            | summarize Volume30dGb = sum(Quantity) / 1024.0,
                        Volume7dGb = sumif(Quantity, TimeGenerated >= ago(7d)) / 1024.0
              by TableName = tostring(DataType)
            | order by Volume30dGb desc
            """;

        var payload = JsonSerializer.Serialize(new
        {
            query
        });

        var response = await SendAsync(
            HttpMethod.Post,
            $"{workspace.Id}/api/query?api-version=2020-08-01",
            "workspace usage data",
            new StringContent(payload, Encoding.UTF8, "application/json"),
            cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("tables", out var tables) || tables.GetArrayLength() == 0)
        {
            return [];
        }

        var rows = tables[0].GetProperty("rows");
        var usage = new List<TableUsageRecord>();

        foreach (var row in rows.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Array || row.GetArrayLength() < 3)
            {
                continue;
            }

            usage.Add(new TableUsageRecord
            {
                TableName = row[0].GetString() ?? "Unknown",
                Volume30dGb = row[1].TryGetDouble(out var gb30) ? gb30 : 0,
                Volume7dGb = row[2].TryGetDouble(out var gb7) ? gb7 : 0
            });
        }

        return usage;
    }

    private async Task<List<T>> GetPagedAsync<T>(string initialUrl, string resourceDescription, CancellationToken cancellationToken)
    {
        const int maxPages = 1000;
        var items = new List<T>();
        string? nextUrl = initialUrl;
        string? previousUrl = null;
        var pageCount = 0;

        while (!string.IsNullOrWhiteSpace(nextUrl))
        {
            if (++pageCount > maxPages)
            {
                logger.LogWarning(
                    "ARM pagination for {ResourceDescription} exceeded {MaxPages} pages; stopping to avoid an unbounded loop. Results may be incomplete.",
                    resourceDescription, maxPages);
                break;
            }

            if (string.Equals(nextUrl, previousUrl, StringComparison.Ordinal))
            {
                logger.LogWarning(
                    "ARM pagination for {ResourceDescription} returned a repeating nextLink; stopping to avoid an infinite loop. Results may be incomplete.",
                    resourceDescription);
                break;
            }

            var page = await GetAsync<AzureResourceListResponse<T>>(nextUrl, resourceDescription, cancellationToken);
            items.AddRange(page.Value);

            previousUrl = nextUrl;
            nextUrl = page.NextLink;
        }

        return items;
    }

    private async Task<T> GetAsync<T>(string requestUri, string resourceDescription, CancellationToken cancellationToken)
    {
        var response = await SendAsync(HttpMethod.Get, requestUri, resourceDescription, null, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException($"Azure Resource Manager returned an empty response for {resourceDescription}.");
        }

        return result;
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string requestUri,
        string resourceDescription,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync(cancellationToken);
        using var request = new HttpRequestMessage(method, requestUri) { Content = content };
        var response = await client.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var details = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogWarning("ARM request for {ResourceDescription} failed with status {StatusCode}: {Details}", resourceDescription, response.StatusCode, details);
        throw new InvalidOperationException(BuildFriendlyError(resourceDescription, response.StatusCode));
    }

    private async Task<HttpClient> CreateClientAsync(CancellationToken cancellationToken)
    {
        var token = await tokenAcquisition.GetAccessTokenForUserAsync(_scopes);
        var client = httpClientFactory.CreateClient("AzureManagement");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string BuildFriendlyError(string resourceDescription, HttpStatusCode statusCode) =>
        statusCode switch
        {
            HttpStatusCode.Unauthorized => $"Azure authorization expired while retrieving {resourceDescription}. Sign out and sign in again.",
            HttpStatusCode.Forbidden => $"You do not have permission to read {resourceDescription}. Confirm your Azure RBAC assignments.",
            HttpStatusCode.NotFound => $"Azure could not find the requested {resourceDescription}. Refresh the picker and try again.",
            _ => $"Azure returned {(int)statusCode} while retrieving {resourceDescription}. Please try again."
        };
}
