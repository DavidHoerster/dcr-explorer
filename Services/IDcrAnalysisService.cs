using DcrDetailBlazor.Models;

namespace DcrDetailBlazor.Services;

public interface IDcrAnalysisService
{
    Task<DcrReportData> AnalyzeAsync(
        WorkspaceInfo workspace,
        IReadOnlyList<DataCollectionRuleResource> dataCollectionRules,
        IReadOnlyList<DataConnectorResource> dataConnectors,
        IReadOnlyList<TableUsageRecord> usageRecords,
        CancellationToken cancellationToken = default);
}
