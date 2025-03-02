var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration["ConnectionStrings:Database"]!)
    .AddCheck<CloudServiceHealthCheck>("CloudServiceProvider");

//builder.Services.AddKeyCloak(builder.Configuration);
builder.Services.AddMemoryCache();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAutoMapper(typeof(Program).Assembly);

//builder.Host.UseSerilog();

//Log.Logger = new LoggerConfiguration()
//    .ReadFrom.Configuration(builder.Configuration)
//    .CreateLogger();

builder.AddGraphQL()
    .AddQueryConventions()
    .AddMutationConventions()
    .AddTypes();

var app = builder.Build();

app.ApplyMigrations();

//app.UseHttpsRedirection();

// Future improvements, this shouldn't be visible to everyone
app.MapHealthChecks("/_health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseCustomExceptionHandler();

//app.UseAuthorization();

app.MapGraphQL();

app.RunWithGraphQLCommands(args);
