using System.Net;
using System.Text;
using DcrDetailBlazor.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Identity.Web;
using NSubstitute;
using Xunit;

namespace DcrDetailBlazor.Tests;

public class AzureArmServiceTests
{
    private const string SubscriptionsBaseUrl = "https://management.azure.com";
    private const string Page2NextLink =
        "https://management.azure.com/subscriptions?api-version=2022-12-01&$skiptoken=page2";

    [Fact]
    public async Task GetSubscriptionsAsync_AggregatesAllPages_WhenNextLinkPresent()
    {
        const string page1 = """
        {
          "value": [
            { "subscriptionId": "sub-1", "displayName": "Prod", "state": "Enabled" },
            { "subscriptionId": "sub-2", "displayName": "Dev", "state": "Enabled" }
          ],
          "nextLink": "https://management.azure.com/subscriptions?api-version=2022-12-01&$skiptoken=page2"
        }
        """;
        const string page2 = """
        {
          "value": [
            { "subscriptionId": "sub-3", "displayName": "Test", "state": "Enabled" }
          ]
        }
        """;

        var handler = new RecordingHandler(JsonResponse(page1), JsonResponse(page2));
        var service = CreateService(handler);

        var subscriptions = await service.GetSubscriptionsAsync(CancellationToken.None);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(
            new[] { "sub-1", "sub-2", "sub-3" },
            subscriptions.Select(s => s.SubscriptionId).ToArray());
    }

    [Fact]
    public async Task GetSubscriptionsAsync_ReturnsSinglePage_WhenNoNextLink()
    {
        const string singlePage = """
        {
          "value": [
            { "subscriptionId": "sub-1", "displayName": "Prod", "state": "Enabled" },
            { "subscriptionId": "sub-2", "displayName": "Dev", "state": "Enabled" }
          ]
        }
        """;

        var handler = new RecordingHandler(JsonResponse(singlePage));
        var service = CreateService(handler);

        var subscriptions = await service.GetSubscriptionsAsync(CancellationToken.None);

        // No nextLink => exactly one request, no extra round-trip.
        Assert.Single(handler.Requests);
        Assert.Equal(
            new[] { "sub-1", "sub-2" },
            subscriptions.Select(s => s.SubscriptionId).ToArray());
    }

    [Fact]
    public async Task GetSubscriptionsAsync_StopsOnRepeatingNextLink_WithoutLooping()
    {
        // Both pages advertise the SAME nextLink. The repeat-nextLink guard must break the
        // loop after the second fetch instead of requesting the same URL forever.
        var repeatingPage = $$"""
        {
          "value": [
            { "subscriptionId": "sub-loop", "displayName": "Loop", "state": "Enabled" }
          ],
          "nextLink": "{{Page2NextLink}}"
        }
        """;

        var handler = new RecordingHandler(JsonResponse(repeatingPage), JsonResponse(repeatingPage));
        var service = CreateService(handler);

        var subscriptions = await service.GetSubscriptionsAsync(CancellationToken.None);

        // Initial request + one repeat, then the guard trips before a third request.
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(2, subscriptions.Count);
    }

    private static AzureArmService CreateService(RecordingHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(SubscriptionsBaseUrl) };
        var httpClientFactory = new SingleClientFactory(httpClient);

        var tokenAcquisition = Substitute.For<ITokenAcquisition>();
        tokenAcquisition
            .GetAccessTokenForUserAsync(Arg.Any<IEnumerable<string>>())
            .ReturnsForAnyArgs("fake-access-token");

        var configuration = new ConfigurationBuilder().Build();
        var logger = NullLogger<AzureArmService>.Instance;

        return new AzureArmService(httpClientFactory, tokenAcquisition, configuration, logger);
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class SingleClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public RecordingHandler(params HttpResponseMessage[] responses) =>
            _responses = new Queue<HttpResponseMessage>(responses);

        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!.ToString());

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Unexpected request to {request.RequestUri}; no responses left in the queue.");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
