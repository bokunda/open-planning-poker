const string HttpClientName = "Fusion";
const string FusionConfigFileName = "gateway.fgp";

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddHttpClient(HttpClientName)
    .AddHttpMessageHandler<HeaderPropagationHandler>();

// Register the custom handler
builder.Services.AddTransient<HeaderPropagationHandler>();

builder.Services.AddHttpContextAccessor();

var corsConfig = new CorsConfiguration();
builder.Configuration.GetSection("Cors").Bind(corsConfig);
if (corsConfig.AllowedOrigins is null || corsConfig.AllowedOrigins.Length == 0)
{
    throw new InvalidOperationException("Cors:AllowedOrigins must contain at least one origin.");
}
// Only the configured origins are allowed. Auth uses the Authorization header, not cookies,
// so credentials are not needed for cross-origin requests.
builder.Services.AddCors(corsConfig.PolicyName, corsConfig.AllowedOrigins);

builder.Services
    .AddFusionGatewayServer()
    .ConfigureFromFile(FusionConfigFileName)
    // Query plans expose internal subgraph structure; keep them off.
    .ModifyFusionOptions(x => x.AllowQueryPlan = false)
    .CoreBuilder
    .AddMaxExecutionDepthRule(10)
    .ModifyRequestOptions(options =>
    {
        options.ExecutionTimeout = TimeSpan.FromSeconds(15);
        options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    })
    .DisableIntrospection(!builder.Environment.IsDevelopment());

// The gateway is the public entry point for HTTP GraphQL traffic.
builder.Services.AddClientRateLimiting(builder.Configuration);

// GraphQL requests are small; large bodies are only an attack vector.
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 256 * 1024);

builder.Services.AddOpenTelemetry(builder.Configuration);

var app = builder.Build();

app.UseCors(corsConfig.PolicyName);
app.UseRateLimiter();
app.UseWebSockets();
app.MapGraphQL().WithOptions(new GraphQLServerOptions
{
    // The Nitro IDE is for local development only.
    Tool = { Enable = app.Environment.IsDevelopment() }
});

app.Run();
