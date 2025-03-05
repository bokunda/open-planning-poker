var builder = WebApplication.CreateBuilder(args);

builder.AddGraphQLServices();
builder.Services.AddHealthCheckServices(builder.Configuration);
builder.Services.AddAuthentication(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ICurrentUserProvider, HttpCurrentUserProvider>();

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);


var app = builder.Build();

app.ApplyMigrations();

 //Future improvements, this shouldn't be visible to everyone
app.MapHealthChecks("/_health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseCustomExceptionHandler();
app.MapGraphQL()
    .RequireAuthorization();

app.RunWithGraphQLCommands(args);