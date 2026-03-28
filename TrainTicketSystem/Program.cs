using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TrainTicketSystem.Hubs;
using TrainTicketSystem.Jobs;
using TrainTicketSystem.Models;
using TrainTicketSystem.Service;
using TrainTicketSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// ---- Redis configuration ----
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnection))
{
    // Distributed cache with Redis
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "TrainTicket_";
    });

    // Data Protection keys stored in Redis (shared across instances)
    var redis = ConnectionMultiplexer.Connect(redisConnection);
    builder.Services.AddDataProtection()
        .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys")
        .SetApplicationName("TrainTicketSystem");
}
else
{
    // Fallback: in-memory cache (local dev without Redis)
    builder.Services.AddDistributedMemoryCache();
}

// Session configuration
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<TrainTicketDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("MyCnn")));
Console.WriteLine(builder.Configuration.GetConnectionString("MyCnn"));
builder.Services.AddScoped<UserSession>();

// ---- SignalR (real-time seat tracking) ----
builder.Services.AddSignalR();

// ---- Seat service & background cleanup job ----
builder.Services.AddScoped<ISeatService, SeatService>();
builder.Services.AddHostedService<SeatHoldCleanupJob>();

// ---- VNPay payment service ----
builder.Services.AddScoped<VnpayService>();

var app = builder.Build();

// ---- Auto-create database if not exists ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TrainTicketDbContext>();
    db.Database.EnsureCreated();

    // Call seed data here
    TrainTicketSystem.Models.DbInitializer.Initialize(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// ---- Load balancer instance identifier (for debugging) ----
var instanceName = Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "local";
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Instance-Name"] = instanceName;
    await next();
});

app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapRazorPages();

// ---- SignalR Hub endpoints ----
app.MapHub<SeatHub>("/seatHub");
app.MapHub<BookingNotificationHub>("/bookingNotificationHub");

app.Run();

