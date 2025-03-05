var builder = WebApplication.CreateBuilder(args);

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddSingleton<IVocabularyCollectorService, VocabularyCollectorService>();
builder.Services.AddSingleton<IUsernameGeneratorService, UsernameGeneratorService>();
builder.Services.AddTransient<ICurrentUserProvider, HttpCurrentUserProvider>();


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

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();

app.RunWithGraphQLCommands(args);
