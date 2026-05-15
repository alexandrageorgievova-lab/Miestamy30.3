using Miestamy30._3.Data;
using Miestamy30._3.Repositories;
using Miestamy30._3.Repositories.Interfaces;
using Miestamy30._3.Services;
using Miestamy30._3.Services.Scrapers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddScoped<IKategoriaRepository, KategoriaRepository>();
builder.Services.AddScoped<IFilterRepository, FilterRepository>();
builder.Services.AddScoped<IMiestoRepository, MiestoRepository>();
builder.Services.AddScoped<ITypPodujatiaRepository, TypPodujatiaRepository>();
builder.Services.AddScoped<IEventFilterRepository, EventFilterRepository>();
builder.Services.AddScoped<IPodujatieRepository, PodujatieRepository>();
builder.Services.AddScoped<DatabaseInitializer>();

builder.Services.AddSingleton<IEventScraper, CvernovkaScraper>();
builder.Services.AddSingleton<IEventScraper, A4Scraper>();
builder.Services.AddSingleton<IEventScraper, StaraTrznicaScraper>();
builder.Services.AddSingleton<IEventScraper, SndScraper>();
builder.Services.AddSingleton<IEventScraper, SngScraper>();
builder.Services.AddHostedService<EventScraperService>();

var app = builder.Build();

// Initialize database and seed on startup
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseDefaultFiles();

var contentTypes = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
contentTypes.Mappings[".woff2"] = "font/woff2";
contentTypes.Mappings[".woff"]  = "font/woff";
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = contentTypes });
app.MapControllers();
app.Run();
