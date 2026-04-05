using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace ChatRumi.IntegrationTesting;

public static class SmsOfficeWireMock
{
    /// <summary>Stub GET .../api/v2/send* with a successful SMS API JSON body.</summary>
    public static void SetupSuccessfulSend(WireMockServer server)
    {
        server
            .Given(Request.Create().UsingGet().WithPath(new WildcardMatcher("/api/v2/send*")))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(
                    """
                    {"success":true,"message":"ok","output":null,"errorCode":0}
                    """));
    }
}
