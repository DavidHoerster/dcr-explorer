namespace DcrDetailBlazor.Models;

public class DataFlowRow
{
    public string DcrName { get; set; } = string.Empty;
    public string DcrResourceId { get; set; } = string.Empty;
    public string DcrKind { get; set; } = string.Empty;
    public int FlowIndex { get; set; }
    public string InputStreams { get; set; } = string.Empty;
    public string OutputStream { get; set; } = string.Empty;
    public string DestinationTable { get; set; } = string.Empty;
    public string TableTier { get; set; } = string.Empty;
    public bool HasTransform { get; set; }
    public string TransformKql { get; set; } = string.Empty;
    public string TransformInsights { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ConnectedVia { get; set; } = "—";
    public double? IngestedGb7d { get; set; }
    public double? IngestedGb30d { get; set; }
    public double? DailyAvgGb { get; set; }
}
