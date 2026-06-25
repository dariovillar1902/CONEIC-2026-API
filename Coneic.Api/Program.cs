using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Coneic.Api.Data;
using Coneic.Api.Models;
using Coneic.Api.Services;

// Derive SQLite path from DATA_FILE_PATH so the DB lives beside data.json on persistent storage
var dataFilePath = Environment.GetEnvironmentVariable("DATA_FILE_PATH");
var sqlitePath = Environment.GetEnvironmentVariable("SQLITE_DB_PATH");
// Ignore SQLITE_DB_PATH if it's a Windows-style path (not absolute on Linux)
if (!string.IsNullOrWhiteSpace(sqlitePath) && !Path.IsPathRooted(sqlitePath))
    sqlitePath = null;
if (string.IsNullOrWhiteSpace(sqlitePath) && !string.IsNullOrWhiteSpace(dataFilePath))
    sqlitePath = Path.Combine(Path.GetDirectoryName(dataFilePath)!, "coneic.db");

var builder = WebApplication.CreateBuilder(args);

if (!string.IsNullOrWhiteSpace(sqlitePath))
{
    Directory.CreateDirectory(Path.GetDirectoryName(sqlitePath)!);
    builder.Configuration["ConnectionStrings:DefaultConnection"] = $"Data Source={sqlitePath}";
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Azure Application Insights — only registered when connection string is present
var appInsightsConn = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (!string.IsNullOrWhiteSpace(appInsightsConn))
    builder.Services.AddApplicationInsightsTelemetry();

// Azure Blob Storage — para certificados de alumnos y comprobantes de pago
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

// Email service: ACS en producción, NullEmailService en desarrollo local
var acsConnectionString = builder.Configuration["ACS_CONNECTION_STRING"];
if (!string.IsNullOrWhiteSpace(acsConnectionString))
    builder.Services.AddSingleton<IEmailService, AcsEmailService>();
else
    builder.Services.AddSingleton<IEmailService, NullEmailService>();

// EF Core + SQLite for all entities (Users, Registrations, PaymentBatches, Speakers, Activities, Photos)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// Bind to PORT env var (required by Render)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5091";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// Apply EF Core migrations and seed from data.json on first run
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    if (!db.Users.Any())
    {
        // Find data.json to seed from (same search logic as the old JsonDataStore)
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        string? jsonPath = null;
        var overridePath = Environment.GetEnvironmentVariable("DATA_FILE_PATH");
        if (!string.IsNullOrWhiteSpace(overridePath) && File.Exists(overridePath))
            jsonPath = overridePath;
        else
        {
            var candidate = Path.Combine(AppContext.BaseDirectory, "data.json");
            if (File.Exists(candidate)) jsonPath = candidate;
            else
            {
                candidate = Path.Combine(env.ContentRootPath, "data.json");
                if (File.Exists(candidate)) jsonPath = candidate;
            }
        }

        if (jsonPath != null)
        {
            var seedOpts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new FlexInt32Converter() },
            };
            var root = JsonSerializer.Deserialize<SeedRoot>(File.ReadAllText(jsonPath), seedOpts);
            if (root != null)
            {
                // Apply role corrections before seeding
                foreach (var u in root.Users)
                    if (u.Email.Equals("tesoreria@coneic2026.com.ar", StringComparison.OrdinalIgnoreCase))
                        u.Role = "tesoreria";

                db.Users.AddRange(root.Users);
                db.Registrations.AddRange(root.Registrations);
                db.PaymentBatches.AddRange(root.PaymentBatches);
                db.SaveChanges();
            }
        }
    }
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

// Skip HTTPS redirection in production (Render handles SSL at the edge)
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseCors("AllowAll");

app.MapControllers();

app.Run();

// Minimal helpers used only for seeding — must be defined after app.Run()
file sealed class SeedRoot
{
    public List<User> Users { get; set; } = new();
    public List<Registration> Registrations { get; set; } = new();
    public List<PaymentBatch> PaymentBatches { get; set; } = new();
}

file sealed class FlexInt32Converter : System.Text.Json.Serialization.JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt32(out var i)) return i;
            if (reader.TryGetDouble(out var d)) return (int)d;
        }
        throw new JsonException($"Cannot convert token {reader.TokenType} to Int32");
    }
    public override void Write(Utf8JsonWriter w, int v, JsonSerializerOptions o) => w.WriteNumberValue(v);
}
