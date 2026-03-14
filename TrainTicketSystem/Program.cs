using Microsoft.EntityFrameworkCore;
using TrainTicketSystem.Hubs;
using TrainTicketSystem.Jobs;
using TrainTicketSystem.Models;
using TrainTicketSystem.Service;
using TrainTicketSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSession();
builder.Services.AddDbContext<TrainTicketDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("MyCnn")));
builder.Services.AddScoped<UserSession>();

// ---- SignalR (real-time seat tracking) ----
builder.Services.AddSignalR();

// ---- Seat service & background cleanup job ----
builder.Services.AddScoped<ISeatService, SeatService>();
builder.Services.AddHostedService<SeatHoldCleanupJob>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapRazorPages();

// ---- SignalR Hub endpoint ----
app.MapHub<SeatHub>("/seatHub");

app.Run();

