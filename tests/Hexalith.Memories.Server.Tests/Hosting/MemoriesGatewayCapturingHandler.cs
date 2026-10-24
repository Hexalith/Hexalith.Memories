namespace Hexalith.Memories.Server.Tests.Hosting;

using System.Net;
using System.Net.Http.Json;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.ServiceDefaults.Authentication;

internal sealed class MemoriesGatewayCapturingHandler : HttpMessageHandler
{
    internal string? Address { get; private set; }
    internal string? AppId { get; private set; }
    internal string? SidecarToken { get; private set; }
    internal string? Authorization { get; private set; }
    internal string? Assertion { get; private set; }

    internal string? Body { get; private set; }
    internal string? CommandProof { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        CommandProof = request.Headers.TryGetValues("X-Hexalith-Bound-Workload-Command-Proof", out IEnumerable<string>? proofs) ? proofs.Single() : null;
        Address = request.RequestUri!.ToString();
        AppId = request.Headers.GetValues("dapr-app-id").Single();
        SidecarToken = request.Headers.GetValues("dapr-api-token").Single();
        Assertion = request.Headers.GetValues(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName).Single();
        Authorization = request.Headers.Authorization?.ToString();
        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(Body!);
        System.Text.Json.JsonElement command = document.RootElement.TryGetProperty("command", out System.Text.Json.JsonElement bound) ? bound : document.RootElement;
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new SubmitCommandResponse("correlation", MessageId: command.GetProperty("messageId").GetString())) };
    }
}
