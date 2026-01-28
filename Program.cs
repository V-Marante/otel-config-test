using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Logging: only OTel provider, base level Warning
builder.Logging.ClearProviders();

// R3 + R2 filter:
// - Drop logs without an active Activity/trace (R3)
// - Keep Warning+ always
// - Keep Info/Debug only when the current request span is Error (R2)
builder.Logging.AddFilter((category, level) =>
{
    var activity = Activity.Current;
    if (activity is null) return false;                     // R3: orphan logs dropped
    if (level >= LogLevel.Warning) return true;             // warnings+ always
    return activity.Status == ActivityStatusCode.Error;     // Info/Debug only on failed trace
});

// OpenTelemetry + Azure Monitor (R1)
builder.Services.AddOpenTelemetry()
    .UseAzureMonitor(options =>
    {
        options.ConnectionString = "InstrumentationKey=3021274a-44ee-484c-b730-07e34fbced1c;IngestionEndpoint=https://northeurope-2.in.applicationinsights.azure.com/;LiveEndpoint=https://northeurope.livediagnostics.monitor.azure.com/;ApplicationId=539a817e-0bf2-47c2-a4a7-a457956ee605";
        options.SamplingRatio = 1.0f;          // full tracing per your requirement (override rate-limited default)
        options.TracesPerSecond = null;        // ensure percentage-based sampler is used
    })
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation(o =>
        {
            o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
            o.RecordException = true;          // sets span status on failures
        });
        
        t.AddHttpClientInstrumentation(o => o.RecordException = true);
        
        t.AddConsoleExporter();
    });

    builder.Logging.AddOpenTelemetry(otel =>
{
    otel.IncludeFormattedMessage = true;
    otel.IncludeScopes = true;
    otel.ParseStateValues = true;

    // Export logs to console (stdout)
    otel.AddConsoleExporter();
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("healthy"));
app.MapGet("/", (ILogger<Program> log) =>
{
    log.LogInformation("Info will only be exported if this trace fails.");
    return "Hello OTel!";
});
app.MapGet("/fail", (ILogger<Program> log) =>
{
    log.LogInformation("Info will be exported because this request fails.");
    throw new InvalidOperationException("Boom");
});

app.Run();