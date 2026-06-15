namespace DcrDetailBlazor.Models;

public class DcrReportData
{
    public string WorkspaceName { get; set; } = string.Empty;
    public string WorkspaceResourceId { get; set; } = string.Empty;
    public int TotalDcrs { get; set; }
    public int TotalDataFlows { get; set; }
    public int ActiveDcrs { get; set; }
    public int OrphanedDcrs { get; set; }
    public int WithTransform { get; set; }
    public int WithoutTransform { get; set; }
    public double TotalVolume30dGb { get; set; }
    public double TotalVolume7dGb { get; set; }
    public List<DataFlowRow> Rows { get; set; } = [];
}
