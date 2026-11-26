using OpenTelemetry.Metrics;
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);
var isSchemaExport = args.Contains("schema");

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.AddGraphQLServices(builder.Configuration);
builder.Services.AddHealthCheckServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration, isSchemaExport);
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ICurrentUserProvider, HttpCurrentUserProvider>();
builder.Services.AddTransient<IUserService, CacheUserService>();

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddRedisConnection(builder.Configuration);
builder.Services.AddClientRateLimiting(builder.Configuration);
builder.Services.AddSingleton<ActionRateLimiter>();
// Link preview images of shared recaps (OPP-171) are cached for a few minutes; at most 200 at a time.
builder.Services.AddMemoryCache(options => options.SizeLimit = 200);
// Slack and Teams apps (OPP-22, OPP-23): endpoints and the queue that posts reveal results.
builder.Services.AddChatIntegrations();

// GraphQL requests are small; large bodies are only an attack vector.
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 256 * 1024);

var corsConfig = new CorsConfiguration();
builder.Configuration.GetSection("Cors").Bind(corsConfig);
builder.Services.AddCors(corsConfig.PolicyName, corsConfig.AllowedOrigins);

builder.Services.AddOpenTelemetry(builder.Configuration);
// Website visits and rooms per website page (OPP-181).
builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddMeter(OpenPlanningPoker.GameEngine.GraphQL.Features.Info.SiteMetrics.MeterName));

// Public API and outgoing webhooks (OPP-141).
builder.Services.AddSingleton<OpenPlanningPoker.GameEngine.GraphQL.Features.Teams.WebhookQueue>();
builder.Services.AddSingleton<OpenPlanningPoker.GameEngine.Application.Features.Teams.IWebhookQueue>(sp =>
    sp.GetRequiredService<OpenPlanningPoker.GameEngine.GraphQL.Features.Teams.WebhookQueue>());
builder.Services.AddHttpClient(OpenPlanningPoker.GameEngine.GraphQL.Features.Teams.WebhookDispatcher.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(10))
    .ConfigurePrimaryHttpMessageHandler(OpenPlanningPoker.GameEngine.GraphQL.Features.Teams.WebhookDispatcher.CreateHandler);
if (!isSchemaExport) builder.Services.AddHostedService<OpenPlanningPoker.GameEngine.GraphQL.Features.Teams.WebhookDispatcher>();
// Activity log retention (GitHub #20).
if (!isSchemaExport) builder.Services.AddHostedService<OpenPlanningPoker.GameEngine.GraphQL.Features.Audits.AuditRetentionService>();

var app = builder.Build();

if (isSchemaExport)
{
    app.MapGraphQL();
        // .RequireAuthorization();
    app.RunWithGraphQLCommands(args);
    return;
}

app.ApplyMigrations();

// Dynamic sitemap endpoint — static routes only (games have TTL, avoid 404s)
app.MapGet("/sitemap.xml", (HttpContext ctx) =>
{
    var now = DateTime.UtcNow.ToString("yyyy-MM-dd");
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
    sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
    sb.AppendLine("  <url>");
    sb.AppendLine("    <loc>https://app.openplanningpoker.com/</loc>");
    sb.AppendLine($"    <lastmod>{now}</lastmod>");
    sb.AppendLine("    <changefreq>weekly</changefreq>");
    sb.AppendLine("    <priority>1.0</priority>");
    sb.AppendLine("  </url>");
    sb.AppendLine("  <url>");
    sb.AppendLine("    <loc>https://app.openplanningpoker.com/game</loc>");
    sb.AppendLine($"    <lastmod>{now}</lastmod>");
    sb.AppendLine("    <changefreq>weekly</changefreq>");
    sb.AppendLine("    <priority>0.8</priority>");
    sb.AppendLine("  </url>");
    sb.AppendLine("</urlset>");

    ctx.Response.Headers["Cache-Control"] = "public, max-age=3600";
    return Results.Content(sb.ToString(), "application/xml; charset=utf-8");
});

// Health details are internal: requests that came through nginx (X-Forwarded-For) get 404.
app.MapHealthChecks("/_health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AddEndpointFilter(async (context, next) =>
    context.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For")
        ? Results.NotFound()
        : await next(context));

app.UseCustomExceptionHandler();

app.UseCors(corsConfig.PolicyName);
// Messages and the PDF report in the caller's language (OPP-24). "sr" alone is listed because the
// browser and the app send it without a script.
app.UseRequestLocalization(options =>
{
    string[] cultures = ["en", "sr", "sr-Latn", "de"];
    options.SetDefaultCulture("en").AddSupportedCultures(cultures).AddSupportedUICultures(cultures);
});
app.UseRateLimiter();
app.UseWebSockets();
app.MapChatIntegrations();
app.MapRecapEndpoints();
OpenPlanningPoker.GameEngine.GraphQL.Features.Teams.PublicApiEndpoints.MapPublicApi(app);
OpenPlanningPoker.GameEngine.GraphQL.Features.Info.SiteMetrics.MapSiteMetrics(app);
// The Nitro IDE is for local development only.
app.MapGraphQL().WithOptions(options => options.Tool.Enable = app.Environment.IsDevelopment());

app.RunWithGraphQLCommands(args);

/// <summary>Entry point, public so the integration tests can host the app.</summary>
public partial class Program;
