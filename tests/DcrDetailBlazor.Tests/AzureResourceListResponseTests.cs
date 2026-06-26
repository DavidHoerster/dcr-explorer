using System.Text.Json;
using DcrDetailBlazor.Models;
using Xunit;

namespace DcrDetailBlazor.Tests;

public class AzureResourceListResponseTests
{
    // Mirrors the options AzureArmService uses when deserializing ARM responses.
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Deserialize_PopulatesNextLink_WhenPresent()
    {
        const string json = """
        {
          "value": [
            { "subscriptionId": "sub-1", "displayName": "Prod", "state": "Enabled" }
          ],
          "nextLink": "https://management.azure.com/subscriptions?api-version=2022-12-01&$skiptoken=next"
        }
        """;

        var response = JsonSerializer.Deserialize<AzureResourceListResponse<SubscriptionInfo>>(json, WebOptions);

        Assert.NotNull(response);
        Assert.Equal(
            "https://management.azure.com/subscriptions?api-version=2022-12-01&$skiptoken=next",
            response!.NextLink);
        var subscription = Assert.Single(response.Value);
        Assert.Equal("sub-1", subscription.SubscriptionId);
    }

    [Fact]
    public void Deserialize_LeavesNextLinkNull_WhenAbsent()
    {
        const string json = """
        {
          "value": [
            { "subscriptionId": "sub-1", "displayName": "Prod", "state": "Enabled" }
          ]
        }
        """;

        var response = JsonSerializer.Deserialize<AzureResourceListResponse<SubscriptionInfo>>(json, WebOptions);

        Assert.NotNull(response);
        Assert.Null(response!.NextLink);
        Assert.Single(response.Value);
    }

    [Fact]
    public void Deserialize_LeavesNextLinkNull_WhenExplicitlyNull()
    {
        const string json = """
        {
          "value": [],
          "nextLink": null
        }
        """;

        var response = JsonSerializer.Deserialize<AzureResourceListResponse<SubscriptionInfo>>(json, WebOptions);

        Assert.NotNull(response);
        Assert.Null(response!.NextLink);
        Assert.Empty(response.Value);
    }
}
