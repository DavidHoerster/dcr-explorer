using DcrDetailBlazor.Models;
using Xunit;

namespace DcrDetailBlazor.Tests;

public class AzureResourceIdHelperTests
{
    private const string ResourceId =
        "/subscriptions/11111111-2222-3333-4444-555555555555/resourceGroups/rg-prod/providers/Microsoft.Insights/dataCollectionRules/dcr-app";

    [Fact]
    public void GetSegmentValue_ReturnsValueFollowingSegment()
    {
        Assert.Equal("Microsoft.Insights", AzureResourceIdHelper.GetSegmentValue(ResourceId, "providers"));
        Assert.Equal("dcr-app", AzureResourceIdHelper.GetSegmentValue(ResourceId, "dataCollectionRules"));
    }

    [Fact]
    public void GetSegmentValue_IsCaseInsensitiveOnSegmentName()
    {
        Assert.Equal("rg-prod", AzureResourceIdHelper.GetSegmentValue(ResourceId, "RESOURCEGROUPS"));
        Assert.Equal("rg-prod", AzureResourceIdHelper.GetSegmentValue(ResourceId, "resourcegroups"));
    }

    [Fact]
    public void GetSegmentValue_ReturnsNullWhenSegmentMissing()
    {
        Assert.Null(AzureResourceIdHelper.GetSegmentValue(ResourceId, "managementGroups"));
    }

    [Fact]
    public void GetSegmentValue_ReturnsNullWhenSegmentHasNoTrailingValue()
    {
        const string trailing = "/subscriptions/sub-1/resourceGroups";
        Assert.Null(AzureResourceIdHelper.GetSegmentValue(trailing, "resourceGroups"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void GetSegmentValue_ReturnsNullForEmptyOrNullInput(string? input)
    {
        Assert.Null(AzureResourceIdHelper.GetSegmentValue(input!, "subscriptions"));
    }

    [Fact]
    public void GetResourceGroup_ReturnsResourceGroupName()
    {
        Assert.Equal("rg-prod", AzureResourceIdHelper.GetResourceGroup(ResourceId));
    }

    [Fact]
    public void GetSubscriptionId_ReturnsSubscriptionGuid()
    {
        Assert.Equal("11111111-2222-3333-4444-555555555555", AzureResourceIdHelper.GetSubscriptionId(ResourceId));
    }

    [Fact]
    public void GetResourceGroup_ReturnsNullWhenAbsent()
    {
        const string subscriptionOnly = "/subscriptions/sub-1";
        Assert.Null(AzureResourceIdHelper.GetResourceGroup(subscriptionOnly));
    }
}
