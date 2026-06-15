using System.Text.Json;
using DcrDetailBlazor.Models;

namespace DcrDetailBlazor.Services;

public class DcrAnalysisService : IDcrAnalysisService
{
    public Task<DcrReportData> AnalyzeAsync(
        WorkspaceInfo workspace,
        IReadOnlyList<DataCollectionRuleResource> dataCollectionRules,
        IReadOnlyList<DataConnectorResource> dataConnectors,
        IReadOnlyList<TableUsageRecord> usageRecords,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedWorkspaceId = workspace.Id.ToLowerInvariant();
        var usageByTable = usageRecords
            .GroupBy(x => NormalizeTableName(x.TableName))
            .ToDictionary(
                group => group.Key,
                group => new TableUsageRecord
                {
                    TableName = group.Key,
                    Volume7dGb = group.Sum(x => x.Volume7dGb),
                    Volume30dGb = group.Sum(x => x.Volume30dGb)
                },
                StringComparer.OrdinalIgnoreCase);

        var connectorMap = BuildConnectorMap(dataCollectionRules, dataConnectors);

        var rows = new List<DataFlowRow>();

        foreach (var dcr in dataCollectionRules.Where(rule => TargetsWorkspace(rule.Properties, normalizedWorkspaceId)))
        {
            var kind = DetectKind(dcr);
            var immutableId = GetNestedString(dcr.Properties, "immutableId");
            var destinationMap = GetDestinationMap(dcr.Properties, normalizedWorkspaceId);
            var flows = GetDataFlows(dcr.Properties);

            if (flows.Count == 0)
            {
                rows.Add(BuildFallbackRow(dcr, kind, immutableId, connectorMap, usageByTable));
                continue;
            }

            for (var index = 0; index < flows.Count; index++)
            {
                var flow = flows[index];
                var inputStreams = GetStringArray(flow, "streams");
                var outputStream = GetString(flow, "outputStream");
                var destinationNames = GetStringArray(flow, "destinations");
                var transformKql = GetString(flow, "transformKql");
                var connectedVia = ResolveConnectors(dcr.Id, immutableId, connectorMap);
                var destinationTable = DeriveDestinationTable(outputStream, inputStreams);
                var usage = usageByTable.GetValueOrDefault(NormalizeTableName(destinationTable));
                var isActive = IsActive(kind, connectedVia);

                rows.Add(new DataFlowRow
                {
                    DcrName = dcr.Name,
                    DcrResourceId = dcr.Id,
                    DcrKind = kind,
                    FlowIndex = index,
                    InputStreams = inputStreams.Count > 0 ? string.Join(", ", inputStreams) : "—",
                    OutputStream = string.IsNullOrWhiteSpace(outputStream) ? string.Join(", ", inputStreams.DefaultIfEmpty("—")) : outputStream,
                    DestinationTable = destinationTable,
                    TableTier = DetermineTableTier(destinationNames, destinationMap, destinationTable),
                    HasTransform = !string.IsNullOrWhiteSpace(transformKql),
                    TransformKql = transformKql,
                    TransformInsights = DescribeTransform(transformKql),
                    Status = isActive ? "Active" : "Orphaned",
                    ConnectedVia = connectedVia.Count > 0 ? string.Join(", ", connectedVia) : "—",
                    IngestedGb7d = usage?.Volume7dGb,
                    IngestedGb30d = usage?.Volume30dGb,
                    DailyAvgGb = usage is null ? null : usage.Volume30dGb / 30d
                });
            }
        }

        var dcrGroups = rows.GroupBy(x => x.DcrResourceId).ToList();
        var report = new DcrReportData
        {
            WorkspaceName = workspace.Name,
            WorkspaceResourceId = workspace.Id,
            TotalDcrs = dcrGroups.Count,
            TotalDataFlows = rows.Count,
            ActiveDcrs = dcrGroups.Count(group => group.Any(row => row.Status == "Active")),
            OrphanedDcrs = dcrGroups.Count(group => group.All(row => row.Status == "Orphaned")),
            WithTransform = dcrGroups.Count(group => group.Any(row => row.HasTransform)),
            WithoutTransform = dcrGroups.Count(group => group.All(row => !row.HasTransform)),
            TotalVolume7dGb = usageRecords.Sum(x => x.Volume7dGb),
            TotalVolume30dGb = usageRecords.Sum(x => x.Volume30dGb),
            Rows = rows
                .OrderBy(x => x.Status)
                .ThenBy(x => x.ConnectedVia)
                .ThenBy(x => x.DcrName)
                .ThenBy(x => x.FlowIndex)
                .ToList()
        };

        return Task.FromResult(report);
    }

    private static DataFlowRow BuildFallbackRow(
        DataCollectionRuleResource dcr,
        string kind,
        string? immutableId,
        IReadOnlyDictionary<string, HashSet<string>> connectorMap,
        IReadOnlyDictionary<string, TableUsageRecord> usageByTable)
    {
        var connectedVia = ResolveConnectors(dcr.Id, immutableId, connectorMap);
        return new DataFlowRow
        {
            DcrName = dcr.Name,
            DcrResourceId = dcr.Id,
            DcrKind = kind,
            FlowIndex = 0,
            InputStreams = "—",
            OutputStream = "—",
            DestinationTable = "Unknown",
            TableTier = "Unknown",
            HasTransform = false,
            TransformKql = string.Empty,
            TransformInsights = "None",
            Status = IsActive(kind, connectedVia) ? "Active" : "Orphaned",
            ConnectedVia = connectedVia.Count > 0 ? string.Join(", ", connectedVia) : "—",
            IngestedGb7d = usageByTable.GetValueOrDefault("Unknown")?.Volume7dGb,
            IngestedGb30d = usageByTable.GetValueOrDefault("Unknown")?.Volume30dGb,
            DailyAvgGb = null
        };
    }

    private static IReadOnlyDictionary<string, HashSet<string>> BuildConnectorMap(
        IReadOnlyList<DataCollectionRuleResource> dataCollectionRules,
        IReadOnlyList<DataConnectorResource> dataConnectors)
    {
        var immutableLookup = dataCollectionRules
            .Select(rule => new
            {
                rule.Id,
                ImmutableId = GetNestedString(rule.Properties, "immutableId")
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.ImmutableId))
            .ToDictionary(x => x.ImmutableId!, x => x.Id, StringComparer.OrdinalIgnoreCase);

        var connectorMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var connector in dataConnectors)
        {
            var connectorName = GetNestedString(connector.Properties, "connectorUiConfig", "title")
                ?? GetNestedString(connector.Properties, "connectorDefinitionName")
                ?? connector.Name;

            foreach (var reference in CollectPotentialReferences(connector.Properties))
            {
                AddConnectorReference(connectorMap, reference, connectorName);

                if (immutableLookup.TryGetValue(reference, out var dcrId))
                {
                    AddConnectorReference(connectorMap, dcrId, connectorName);
                }
            }
        }

        return connectorMap;
    }

    private static void AddConnectorReference(IDictionary<string, HashSet<string>> connectorMap, string key, string connectorName)
    {
        if (!connectorMap.TryGetValue(key, out var connectors))
        {
            connectors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            connectorMap[key] = connectors;
        }

        connectors.Add(connectorName);
    }

    private static HashSet<string> ResolveConnectors(string dcrId, string? immutableId, IReadOnlyDictionary<string, HashSet<string>> connectorMap)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (connectorMap.TryGetValue(dcrId, out var byId))
        {
            result.UnionWith(byId);
        }

        if (!string.IsNullOrWhiteSpace(immutableId) && connectorMap.TryGetValue(immutableId, out var byImmutableId))
        {
            result.UnionWith(byImmutableId);
        }

        return result;
    }

    private static bool TargetsWorkspace(JsonElement properties, string workspaceId)
    {
        foreach (var destination in GetArray(properties, "destinations", "logAnalytics"))
        {
            var destinationWorkspaceId = GetString(destination, "workspaceResourceId");
            if (string.Equals(destinationWorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (var candidate in CollectPotentialReferences(properties))
        {
            if (string.Equals(candidate, workspaceId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static Dictionary<string, JsonElement> GetDestinationMap(JsonElement properties, string workspaceId)
    {
        var destinations = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        foreach (var destination in GetArray(properties, "destinations", "logAnalytics"))
        {
            var destinationWorkspaceId = GetString(destination, "workspaceResourceId");
            if (!string.Equals(destinationWorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var name = GetString(destination, "name");
            if (!string.IsNullOrWhiteSpace(name))
            {
                destinations[name] = destination;
            }
        }

        return destinations;
    }

    private static List<JsonElement> GetDataFlows(JsonElement properties) => GetArray(properties, "dataFlows");

    private static string DetectKind(DataCollectionRuleResource dcr)
    {
        var explicitKind = dcr.Kind;
        if (!string.IsNullOrWhiteSpace(explicitKind))
        {
            return explicitKind;
        }

        if (HasArray(dcr.Properties, "dataSources", "windowsEventLogs") || HasArray(dcr.Properties, "dataSources", "windowsPerformanceCounters"))
        {
            return "Windows";
        }

        if (HasArray(dcr.Properties, "dataSources", "syslog") || HasArray(dcr.Properties, "dataSources", "performanceCounters"))
        {
            return "Linux";
        }

        if (HasArray(dcr.Properties, "references", "applicationInsights") || GetString(dcr.Properties, "kind")?.Contains("Telemetry", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "PlatformTelemetry";
        }

        if (GetDataFlows(dcr.Properties).Any(flow => !string.IsNullOrWhiteSpace(GetString(flow, "transformKql"))))
        {
            return "WorkspaceTransforms";
        }

        return "Direct";
    }

    private static bool IsActive(string kind, HashSet<string> connectedVia) =>
        connectedVia.Count > 0 ||
        kind is "WorkspaceTransforms" or "AgentSettings" or "PlatformTelemetry";

    private static string DeriveDestinationTable(string? outputStream, IReadOnlyList<string> inputStreams)
    {
        if (!string.IsNullOrWhiteSpace(outputStream))
        {
            return NormalizeTableName(outputStream);
        }

        return inputStreams.Count switch
        {
            0 => "Unknown",
            1 => NormalizeTableName(inputStreams[0]),
            _ => string.Join(", ", inputStreams.Select(NormalizeTableName).Distinct(StringComparer.OrdinalIgnoreCase))
        };
    }

    private static string NormalizeTableName(string? streamOrTable)
    {
        if (string.IsNullOrWhiteSpace(streamOrTable))
        {
            return "Unknown";
        }

        var value = streamOrTable.Trim();

        if (value.StartsWith("Custom-", StringComparison.OrdinalIgnoreCase))
        {
            value = value["Custom-".Length..];
        }
        else if (value.StartsWith("Microsoft-", StringComparison.OrdinalIgnoreCase))
        {
            value = value["Microsoft-".Length..];
        }

        return value;
    }

    private static string DetermineTableTier(
        IReadOnlyList<string> destinationNames,
        IReadOnlyDictionary<string, JsonElement> destinationMap,
        string destinationTable)
    {
        foreach (var destinationName in destinationNames)
        {
            if (destinationMap.TryGetValue(destinationName, out var destination))
            {
                var tier = GetString(destination, "tableType")
                    ?? GetString(destination, "tableTier")
                    ?? GetString(destination, "type");

                if (!string.IsNullOrWhiteSpace(tier))
                {
                    return tier;
                }
            }
        }

        return destinationTable.EndsWith("_CL", StringComparison.OrdinalIgnoreCase) ? "Custom" : "Analytics";
    }

    private static string DescribeTransform(string? transformKql)
    {
        if (string.IsNullOrWhiteSpace(transformKql))
        {
            return "None";
        }

        var value = transformKql.ToLowerInvariant();
        var insights = new List<string>();

        if (value.Contains("where "))
        {
            insights.Add("Filters");
        }

        if (value.Contains("extend ") || value.Contains("project "))
        {
            insights.Add("Shapes columns");
        }

        if (value.Contains("parse") || value.Contains("extract"))
        {
            insights.Add("Parses payload");
        }

        if (value.Contains("summarize"))
        {
            insights.Add("Aggregates");
        }

        return insights.Count > 0 ? string.Join(", ", insights) : "Custom KQL";
    }

    private static List<string> CollectPotentialReferences(JsonElement element)
    {
        var results = new List<string>();
        Walk(element, results);
        return results;

        static void Walk(JsonElement current, ICollection<string> collector)
        {
            switch (current.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in current.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.String)
                        {
                            var value = property.Value.GetString();
                            if (!string.IsNullOrWhiteSpace(value) &&
                                (property.Name.Contains("dataCollectionRule", StringComparison.OrdinalIgnoreCase) ||
                                 property.Name.Contains("immutableId", StringComparison.OrdinalIgnoreCase) ||
                                 property.Name.Contains("workspaceResourceId", StringComparison.OrdinalIgnoreCase)))
                            {
                                collector.Add(value);
                            }
                        }

                        Walk(property.Value, collector);
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (var item in current.EnumerateArray())
                    {
                        Walk(item, collector);
                    }
                    break;
            }
        }
    }

    private static bool HasArray(JsonElement element, params string[] path) => GetArray(element, path).Count > 0;

    private static List<JsonElement> GetArray(JsonElement element, params string[] path)
    {
        var current = element;

        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
            {
                return [];
            }
        }

        if (current.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return current.EnumerateArray().ToList();
    }

    private static IReadOnlyList<string> GetStringArray(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return property
            .EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString() ?? string.Empty)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
        {
            return string.Empty;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() ?? string.Empty : string.Empty;
    }

    private static string? GetNestedString(JsonElement element, params string[] path)
    {
        var current = element;

        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
            {
                return null;
            }
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
    }
}
