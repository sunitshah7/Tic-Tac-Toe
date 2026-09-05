using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TicTacToe.Api.Services;
using TicTacToe.Infrastructure;
using TicTacToe.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Enums travel as their names ("X", "InProgress") rather than integers: the contract stays
// readable in Swagger and in the browser network tab, and the Angular client can use the
// same string unions the backend uses.
builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Tic Tac Toe API",
        Version = "v1",
        Description = "Game sessions, moves, undo and the session scoreboard. The backend is "
                    + "the source of truth for all game state."
    });

    var xmlDoc = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlDoc))
    {
        options.IncludeXmlComments(xmlDoc);
    }
});

const string FrontendCors = "angular-dev-client";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options => options.AddPolicy(
    FrontendCors,
    policy => policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("GameDatabase") ?? "Data Source=tictactoe.db");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<GameSessionLocks>();
builder.Services.AddScoped<GameService>();
builder.Services.AddExceptionHandler<GameExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// The database is a local SQLite file; creating it on startup keeps the run instructions to
// a single "dotnet run" with no migration step for the reviewer.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    context.Database.EnsureCreated();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(FrontendCors);
app.MapControllers();

app.Run();

/// <summary>Exposed so the integration tests can host the API in-process.</summary>
public partial class Program;
