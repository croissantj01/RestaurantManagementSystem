using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Configure Stripe API Secret Key globally (Removed ApiVersion since it's read-only)
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

QuestPDF.Settings.License =
    QuestPDF.Infrastructure.LicenseType.Community;

builder.Services.AddRazorPages();
builder.Services.AddControllers();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Explicitly disambiguated 'Customer' to prevent conflicts with Stripe.Customer
builder.Services.AddScoped<IPasswordHasher<RestaurantManagementSystem.Models.Customer>, PasswordHasher<RestaurantManagementSystem.Models.Customer>>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();