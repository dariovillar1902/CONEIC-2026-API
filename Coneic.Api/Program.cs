using Microsoft.EntityFrameworkCore;
using Coneic.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JSON data store for Users and Registrations (replaces SQLite for these entities)
builder.Services.AddSingleton<JsonDataStore>();

// EF Core + SQLite kept for Activities and Speakers (schedule data)
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

// Apply EF Core migrations for Activities/Speakers/Photos schema
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
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
