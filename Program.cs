using PowerBiProxy;

var builder = WebApplication.CreateBuilder(args);

var settings = new PowerBiSettings();
builder.Configuration.GetSection("PowerBi").Bind(settings);
builder.Services.AddSingleton(settings);

// NuGet-based (Microsoft.PowerBI.Api)
builder.Services.AddSingleton<PowerBiClientFactory>();
builder.Services.AddScoped<PowerBiService>();

// Direct HTTP-based (no PowerBI NuGet)
builder.Services.AddHttpClient("entra");
builder.Services.AddHttpClient("powerbi");
builder.Services.AddScoped<ApiBasedService>();

// RLS — XMLA endpoint with CustomData for CUSTOMDATA()-based RLS
builder.Services.AddSingleton<XmlaService>();
builder.Services.AddScoped<RlsService>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
