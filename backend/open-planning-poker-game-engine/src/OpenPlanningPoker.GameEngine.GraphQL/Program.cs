QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.AddGraphQLServices(builder.Configuration);
builder.Services.AddHealthCheckServices(builder.Configuration);
builder.Services.AddAuthentication(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ICurrentUserProvider, HttpCurrentUserProvider>();
builder.Services.AddTransient<IUserService, CacheUserService>();

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// Register IConnectionMultiplexer for Redis pub-sub and chat persistence (lazy — only connects when first used)
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration["ConnectionStrings:Cache"]!));

var corsConfig = new CorsConfiguration();
builder.Configuration.GetSection("Cors").Bind(corsConfig);
builder.Services.AddCors(corsConfig.PolicyName, corsConfig.AllowedOrigins);

builder.Services.AddOpenTelemetry(builder.Configuration);

var app = builder.Build();

if (args.Contains("schema"))
{
    app.MapGraphQL();
        // .RequireAuthorization();
    app.RunWithGraphQLCommands(args);
    return;
}

app.ApplyMigrations();

// Dynamic sitemap endpoint — serves active game URLs for SEO
app.MapGet("/sitemap.xml", async (OpenPlanningPokerGameEngineDbContext db) =>
{
    var now = DateTime.UtcNow.ToString("yyyy-MM-dd");
    var games = await db.Set<Game>()
        .OrderByDescending(g => EF.Property<DateTimeOffset>(g, "CreatedOn"))
        .Take(500)
        .Select(g => new { g.Id, CreatedOn = EF.Property<DateTimeOffset>(g, "CreatedOn") })
        .ToListAsync();

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
    foreach (var game in games)
    {
        sb.AppendLine("  <url>");
        sb.AppendLine($"    <loc>https://app.openplanningpoker.com/game/{game.Id}</loc>");
        sb.AppendLine($"    <lastmod>{game.CreatedOn:yyyy-MM-dd}</lastmod>");
        sb.AppendLine("    <changefreq>daily</changefreq>");
        sb.AppendLine("    <priority>0.6</priority>");
        sb.AppendLine("  </url>");
    }
    sb.AppendLine("</urlset>");

    ctx.Response.Headers["Cache-Control"] = "public, max-age=3600";
    return Results.Content(sb.ToString(), "application/xml; charset=utf-8");
});

//Future improvements, this shouldn't be visible to everyone
app.MapHealthChecks("/_health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseCustomExceptionHandler();

app.UseWebSockets();
app.MapGraphQL();
    //.RequireAuthorization();

app.UseCors(corsConfig.PolicyName);

app.RunWithGraphQLCommands(args);