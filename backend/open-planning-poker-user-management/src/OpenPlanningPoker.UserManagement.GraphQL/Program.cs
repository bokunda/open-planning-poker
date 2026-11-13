var builder = WebApplication.CreateBuilder(args);
var isSchemaExport = args.Contains("schema");

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddSingleton<IVocabularyCollectorService, VocabularyCollectorService>();
builder.Services.AddSingleton<IUsernameGeneratorService, UsernameGeneratorService>();
builder.Services.AddTransient<ICurrentUserProvider, HttpCurrentUserProvider>();
builder.Services.AddTransient<IUserService, CacheUserService>();


builder.Services
    .AddHttpClient(Constants.DefaultHttpClientName)
    .AddStandardResilienceHandler();

builder.Services.AddRedis(builder.Configuration);
CacheExtensions.AddHybridCache(builder.Services);

builder.Services.AddJwtAuthentication(builder.Configuration, isSchemaExport);

builder
    .AddGraphQL()
    .AddAuthorization()
    .AddQueryConventions()
    .AddMutationConventions()
    .AddMaxExecutionDepthRule(10)
    .ModifyRequestOptions(options =>
    {
        options.ExecutionTimeout = TimeSpan.FromSeconds(10);
        options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    })
    .DisableIntrospection(!builder.Environment.IsDevelopment())
    .TryAddTypeInterceptor<OpenPlanningPoker.UserManagement.GraphQL.Localization.ApplicationErrorMessageInterceptor>()
    .AddTypes();

builder.Services.AddClientRateLimiting(builder.Configuration);
builder.Services.AddSingleton<ActionRateLimiter>();

// GraphQL requests are small; large bodies are only an attack vector.
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 256 * 1024);

builder.Services.AddHttpContextAccessor();

builder.Services.AddOpenTelemetry(builder.Configuration);

var corsConfig = new CorsConfiguration();
builder.Configuration.GetSection("Cors").Bind(corsConfig);
builder.Services.AddCors(corsConfig.PolicyName, corsConfig.AllowedOrigins);

var app = builder.Build();

if (isSchemaExport)
{
    app.MapGraphQL();
    app.RunWithGraphQLCommands(args);
    return;
}

app.UseSerilogRequestLogging();

app.UseCors(corsConfig.PolicyName);
// Error messages follow Accept-Language (OPP-24).
app.UseRequestLocalization(options =>
{
    string[] cultures = ["en", "sr", "sr-Latn", "de"];
    options.SetDefaultCulture("en").AddSupportedCultures(cultures).AddSupportedUICultures(cultures);
});
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL().WithOptions(new GraphQLServerOptions
{
    // The Nitro IDE is for local development only.
    Tool = { Enable = app.Environment.IsDevelopment() }
});

app.RunWithGraphQLCommands(args);
