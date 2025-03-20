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
builder.Services.AddCors(corsConfig.PolicyName, corsConfig.AllowedOrigins, true);

builder.Services
    .AddFusionGatewayServer()
    .ConfigureFromFile(FusionConfigFileName)
    // Note: AllowQueryPlan is enabled for demonstration purposes. Disable in production environments.
    .ModifyFusionOptions(x => x.AllowQueryPlan = true);

builder.Services.AddOpenTelemetry(builder.Configuration);

var app = builder.Build();

app.UseWebSockets();
app.MapGraphQL();
app.UseCors(corsConfig.PolicyName);

app.Run();
