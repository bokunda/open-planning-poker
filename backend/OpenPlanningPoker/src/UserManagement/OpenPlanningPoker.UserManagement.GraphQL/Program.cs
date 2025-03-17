var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddAuthentication(builder.Configuration);

builder
    .AddGraphQL()
    .AddAuthorization()
    .AddQueryConventions()
    .AddMutationConventions()
    .AddTypes();

builder.Services.AddHttpContextAccessor();

builder.Services.AddOpenTelemetry(builder.Configuration);

var corsConfig = new CorsConfiguration();
builder.Configuration.GetSection("Cors").Bind(corsConfig);
builder.Services.AddCors(corsConfig.PolicyName, corsConfig.AllowedOrigins);

var app = builder.Build();

if (args.Contains("schema"))
{
    app.MapGraphQL();
    app.RunWithGraphQLCommands(args);
    return;
}

app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();

app.UseCors(corsConfig.PolicyName);

app.RunWithGraphQLCommands(args);
