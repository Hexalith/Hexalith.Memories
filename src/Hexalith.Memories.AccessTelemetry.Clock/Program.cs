using System.Security.Cryptography;

using Hexalith.Memories.AccessTelemetry.Clock;
using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.ServiceDefaults;
using Hexalith.Memories.ServiceDefaults.Security;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(configureRedisInstrumentation: false);
builder.Services.AddHttpClient("authenticated-utc");
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<MonotonicRecordIdGenerator>();
builder.Services.AddSingleton<ECDsa>(_ =>
{
    string? encoded = Environment.GetEnvironmentVariable("ACCESS_TELEMETRY_CLOCK_SIGNING_KEY_PKCS8");
    ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    if (string.IsNullOrWhiteSpace(encoded))
    {
        if (builder.Environment.IsProduction())
        {
            key.Dispose();
            throw new InvalidOperationException("Production clock signing key was not injected from the clock-only secret authority.");
        }

        return key;
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
