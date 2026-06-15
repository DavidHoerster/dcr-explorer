using DcrDetailBlazor.Models;

namespace DcrDetailBlazor.Services;

public interface IAzureArmService
{
    Task<IReadOnlyList<SubscriptionInfo>> GetSubscriptionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkspaceInfo>> GetWorkspacesAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataCollectionRuleResource>> GetDataCollectionRulesAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataConnectorResource>> GetDataConnectorsAsync(WorkspaceInfo workspace, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TableUsageRecord>> GetUsageAsync(WorkspaceInfo workspace, CancellationToken cancellationToken = default);
}
