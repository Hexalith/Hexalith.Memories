using System.Security.Cryptography;

using Dapr.Client;

using Hexalith.Memories.AccessTelemetry.Clock;
using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.ServiceDefaults;
using Hexalith.Memories.ServiceDefaults.Security;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(configureRedisInstrumentation: false);
builder.Services.AddSingleton<DaprClient>(_ => new DaprClientBuilder().Build());
builder.Services.AddHttpClient("authenticated-utc");
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<MonotonicRecordIdGenerator>();
builder.Services.AddSingleton<ECDsa>(services =>
{
    string secretStore = builder.Configuration["Clock:SecretStoreName"] ?? AccessTelemetryOptions.RequiredSecretStoreName;
    string secretName = builder.Configuration["Clock:SigningKeySecretName"] ?? "access-telemetry-clock-key";
    Dictionary<string, string> secret = services.GetRequiredService<DaprClient>()
        .GetSecretAsync(secretStore, secretName)
        .ConfigureAwait(false)
        .GetAwaiter()
        .GetResult();
    _ = secret.TryGetValue("signing-key-pkcs8", out string? encoded);
    if (string.IsNullOrWhiteSpace(encoded))
    {
        _ = secret.TryGetValue(secretName, out encoded);
    }

    ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    if (string.IsNullOrWhiteSpace(encoded))
    {
        key.Dispose();
        throw new InvalidOperationException("Clock signing material is unavailable from the scoped Dapr secret authority.");
    }

    key.ImportPkcs8PrivateKey(Convert.FromBase64String(encoded), out int bytesRead);
    if (bytesRead == 0)
    {
        key.Dispose();
        throw new InvalidOperationException("Clock signing key is malformed.");
    }

    return key;
});
builder.Services.AddSingleton<IClockAttestationSigner>(services => new EcdsaClockAttestationSigner(
    services.GetRequiredService<ECDsa>(),
    builder.Configuration["Clock:SignerKeyEpoch"] ?? "development-clock-key"));

foreach (IConfigurationSection source in builder.Configuration.GetSection("Clock:Sources").GetChildren())
{
    string sourceId = source["Id"] ?? throw new InvalidOperationException("Clock source ID is required.");
    var endpoint = new Uri(source["Endpoint"] ?? throw new InvalidOperationException("Clock source endpoint is required."), UriKind.Absolute);
    string token = source["AuthenticationToken"] ?? throw new InvalidOperationException("Clock source authentication is required.");
    builder.Services.AddSingleton<IAuthenticatedUtcSource>(services => new HttpAuthenticatedUtcSource(
        services.GetRequiredService<IHttpClientFactory>().CreateClient("authenticated-utc"),
        sourceId,
        endpoint,
        token));
}

if (!builder.Environment.IsProduction() && builder.Configuration.GetValue<bool>("Clock:AllowDevelopmentSources"))
{
    foreach (string sourceId in new[] { "development-utc-a", "development-utc-b", "development-utc-c" })
    {
        builder.Services.AddSingleton<IAuthenticatedUtcSource>(services => new DevelopmentAuthenticatedUtcSource(
            sourceId,
            services.GetRequiredService<TimeProvider>()));
    }
}

builder.Services.AddSingleton<ClockAttestationService>();

WebApplication app = builder.Build();

app.UseMiddleware<DaprApplicationTokenMiddleware>();
app.MapDefaultEndpoints();
app.MapPost("/v1/time/attest", async (
    ClockAttestationRequest request,
    ClockAttestationService service,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.AttestAsync(request, cancellationToken).ConfigureAwait(false));
    }
    catch (ClockAttestationException exception)
    {
        return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: exception.Reason.ToString());
    }
}).AllowAnonymous();

app.Run();

internal partial class Program;
