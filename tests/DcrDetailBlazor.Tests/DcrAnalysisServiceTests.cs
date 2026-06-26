using System.Text.Json;
using DcrDetailBlazor.Models;
using DcrDetailBlazor.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DcrDetailBlazor.Tests;

public class DcrAnalysisServiceTests
{
    private const string WorkspaceId =
        "/subscriptions/sub-1/resourceGroups/rg-obs/providers/Microsoft.OperationalInsights/workspaces/law-prod";

    private readonly DcrAnalysisService _service = new(NullLogger<DcrAnalysisService>.Instance);
    private static readonly WorkspaceInfo Workspace = new() { Id = WorkspaceId, Name = "law-prod" };

    // Parse JSON and detach the element from its document so it stays valid for the test's lifetime.
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static DataCollectionRuleResource ActiveRule() => new()
    {
        Id = "/subscriptions/sub-1/resourceGroups/rg-obs/providers/Microsoft.Insights/dataCollectionRules/dcr-active",
        Name = "dcr-active",
        Kind = "Linux",
        Properties = Json($$"""
        {
          "immutableId": "dcr-imm-active",
          "dataFlows": [
            {
              "streams": ["Custom-AppLogs_CL"],
              "outputStream": "Custom-AppLogs_CL",
              "destinations": ["la"]
            }
          ],
          "destinations": {
            "logAnalytics": [
              { "name": "la", "workspaceResourceId": "{{WorkspaceId}}" }
            ]
          }
        }
        """)
    };

    private static DataConnectorResource MatchingConnector() => new()
    {
        Id = "/subscriptions/sub-1/resourceGroups/rg-obs/providers/Microsoft.SecurityInsights/dataConnectors/conn-1",
        Name = "conn-1",
        Properties = Json("""
        {
          "connectorDefinitionName": "AzureActivity",
          "dataCollectionRuleId": "dcr-imm-active"
        }
        """)
    };

    [Fact]
    public async Task AnalyzeAsync_MarksDcrActive_WhenConnectorReferencesIt()
    {
        var rules = new[] { ActiveRule() };
        var connectors = new[] { MatchingConnector() };

        var report = await _service.AnalyzeAsync(Workspace, rules, connectors, [], CancellationToken.None);

        Assert.Equal(1, report.TotalDcrs);
        Assert.Equal(1, report.TotalDataFlows);
        Assert.Equal(1, report.ActiveDcrs);
        Assert.Equal(0, report.OrphanedDcrs);

        var row = Assert.Single(report.Rows);
        Assert.Equal("Active", row.Status);
        Assert.Equal("AzureActivity", row.ConnectedVia);
        Assert.Equal("AppLogs_CL", row.DestinationTable);
    }

    [Fact]
    public async Task AnalyzeAsync_MarksDcrOrphaned_WhenNoConnectorReferencesIt()
    {
        var rule = new DataCollectionRuleResource
        {
            Id = "/subscriptions/sub-1/resourceGroups/rg-obs/providers/Microsoft.Insights/dataCollectionRules/dcr-orphan",
            Name = "dcr-orphan",
            Kind = "Direct",
            Properties = Json($$"""
            {
              "immutableId": "dcr-imm-orphan",
              "dataFlows": [
                {
                  "streams": ["Custom-Lonely_CL"],
                  "outputStream": "Custom-Lonely_CL",
                  "destinations": ["la"]
                }
              ],
              "destinations": {
                "logAnalytics": [
                  { "name": "la", "workspaceResourceId": "{{WorkspaceId}}" }
                ]
              }
            }
            """)
        };

        var report = await _service.AnalyzeAsync(Workspace, new[] { rule }, [], [], CancellationToken.None);

        Assert.Equal(1, report.TotalDcrs);
        Assert.Equal(0, report.ActiveDcrs);
        Assert.Equal(1, report.OrphanedDcrs);

        var row = Assert.Single(report.Rows);
        Assert.Equal("Orphaned", row.Status);
    }

    [Fact]
    public async Task AnalyzeAsync_DetectsTransform_AndDescribesInsights()
    {
        var rule = new DataCollectionRuleResource
        {
            Id = "/subscriptions/sub-1/resourceGroups/rg-obs/providers/Microsoft.Insights/dataCollectionRules/dcr-xform",
            Name = "dcr-xform",
            Properties = Json($$"""
            {
              "immutableId": "dcr-imm-xform",
              "dataFlows": [
                {
                  "streams": ["Custom-Raw_CL"],
                  "outputStream": "Custom-Shaped_CL",
                  "destinations": ["la"],
                  "transformKql": "source | where Level == 'Error' | extend Env = 'prod' | summarize Count = count() by bin(TimeGenerated, 1h)"
                }
              ],
              "destinations": {
                "logAnalytics": [
                  { "name": "la", "workspaceResourceId": "{{WorkspaceId}}" }
                ]
              }
            }
            """)
        };

        var report = await _service.AnalyzeAsync(Workspace, new[] { rule }, [], [], CancellationToken.None);

        Assert.Equal(1, report.WithTransform);
        var row = Assert.Single(report.Rows);
        Assert.True(row.HasTransform);
        Assert.Equal("Filters, Shapes columns, Aggregates", row.TransformInsights);
    }

    [Fact]
    public async Task AnalyzeAsync_AggregatesUsageVolumes()
    {
        var rules = new[] { ActiveRule() };
        var connectors = new[] { MatchingConnector() };
        var usage = new[]
        {
            new TableUsageRecord { TableName = "AppLogs_CL", Volume7dGb = 7.0, Volume30dGb = 30.0 },
            new TableUsageRecord { TableName = "Other_CL", Volume7dGb = 3.5, Volume30dGb = 12.0 },
        };

        var report = await _service.AnalyzeAsync(Workspace, rules, connectors, usage, CancellationToken.None);

        Assert.Equal(10.5, report.TotalVolume7dGb, 3);
        Assert.Equal(42.0, report.TotalVolume30dGb, 3);

        var row = Assert.Single(report.Rows);
        Assert.Equal(7.0, row.IngestedGb7d);
        Assert.Equal(30.0, row.IngestedGb30d);
        Assert.Equal(1.0, row.DailyAvgGb);
    }

    [Fact]
    public async Task AnalyzeAsync_Throws_WhenCancellationAlreadyRequested()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _service.AnalyzeAsync(Workspace, [], [], [], cts.Token));
    }
}
