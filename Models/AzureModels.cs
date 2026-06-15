using System.Text.Json;
using System.Text.Json.Serialization;

namespace DcrDetailBlazor.Models;

public class AzureResourceListResponse<T>
{
    [JsonPropertyName("value")]
    public List<T> Value { get; set; } = [];
}

public class SubscriptionInfo
{
    [JsonPropertyName("subscriptionId")]
    public string SubscriptionId { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;
}

public class WorkspaceInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("properties")]
    public WorkspaceProperties Properties { get; set; } = new();
}

public class WorkspaceProperties
{
    [JsonPropertyName("customerId")]
    public string? CustomerId { get; set; }
}

public class DataCollectionRuleResource
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("properties")]
    public JsonElement Properties { get; set; }
}

public class DataConnectorResource
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("properties")]
    public JsonElement Properties { get; set; }
}

public class TableUsageRecord
{
    public string TableName { get; set; } = string.Empty;
    public double Volume7dGb { get; set; }
    public double Volume30dGb { get; set; }
}

public static class AzureResourceIdHelper
{
    public static string? GetSegmentValue(string resourceId, string segmentName)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            return null;
        }

        var segments = resourceId.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var index = 0; index < segments.Length - 1; index++)
        {
            if (string.Equals(segments[index], segmentName, StringComparison.OrdinalIgnoreCase))
            {
                return segments[index + 1];
            }
        }

        return null;
    }

    public static string? GetResourceGroup(string resourceId) => GetSegmentValue(resourceId, "resourceGroups");
    public static string? GetSubscriptionId(string resourceId) => GetSegmentValue(resourceId, "subscriptions");
}
