using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using EventManagementApi2.Data;
using EventManagementApi2.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Database configuration
builder.Services.AddDbContext<EventContext>(options => 
    options.UseInMemoryDatabase("EventsDb"));

// Register repositories and unit of work
builder.Services.AddScoped<EventManagementApi2.Data.Repositories.IEventRepository, EventManagementApi2.Data.Repositories.EventRepository>();
builder.Services.AddScoped<EventManagementApi2.Data.Repositories.ITierCategoryRepository, EventManagementApi2.Data.Repositories.TierCategoryRepository>();
builder.Services.AddScoped<EventManagementApi2.Data.Repositories.ITicketRepository, EventManagementApi2.Data.Repositories.TicketRepository>();
builder.Services.AddScoped<EventManagementApi2.Data.IUnitOfWork, EventManagementApi2.Data.UnitOfWork>();


// In-memory inventory to prevent overselling under concurrency (reservation/hold pattern).
// Registered as a singleton so counters are shared across all requests.
builder.Services.AddSingleton<IInventoryService, InMemoryInventoryService>();
builder.Services.AddHostedService<HoldExpiryService>();


// CORS configuration for production
builder.Services.AddCors(options =>
{
    options.AddPolicy("ProductionPolicy", corsPolicyBuilder =>
    {
        corsPolicyBuilder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Enable CORS
app.UseCors("ProductionPolicy");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Production health check endpoints
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithOpenApi();

// Readiness check endpoint
app.MapGet("/ready", () =>
{
    try
    {
        return Results.Ok(new { ready = true, timestamp = DateTime.UtcNow });
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
})
.WithName("ReadinessCheck")
.WithOpenApi();

// HTTPS redirection (optional in container)
if (!app.Environment.IsDevelopment())
{
    // app.UseHttpsRedirection(); // Uncomment if using HTTPS
}

app.UseAuthorization();

// Map all controllers (including TicketsController)
app.MapControllers();

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EventContext>();

    try
    {
        // Seed initial data
        EventSeeder.Seed(context);
        app.Logger.LogInformation("Database seeded successfully");

        // Seed in-memory inventory counters for the seeded events/categories.
        var inventory = scope.ServiceProvider.GetRequiredService<IInventoryService>();
        var events = context.Events
            .Select(e => new
            {
                e.Id,
                Categories = e.EventTierCategories.Select(etc => new { etc.TierCategoryId, etc.MaxTicketsPerCategory })
            })
            .ToList();

        foreach (var ev in events)
        {
            foreach (var category in ev.Categories)
            {
                await inventory.EnsureSeededAsync(ev.Id, category.TierCategoryId, category.MaxTicketsPerCategory);
            }
        }

        app.Logger.LogInformation("Inventory seeded successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error seeding database");
    }
}

app.Run();
