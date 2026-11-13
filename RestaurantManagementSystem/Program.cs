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
builder.Services.AddTransient<RestaurantManagementSystem.Services.IEmailSender, RestaurantManagementSystem.Services.EmailSender>();

var app = builder.Build();

// ---- TEMPORARY: auto-insert dummy data on startup (remove before submitting) ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    await db.Database.MigrateAsync();   // use EnsureCreatedAsync() if you don't use migrations
    await DbSeeder.EnsureSeededAsync(db);
}
// ---------------------------------------------------------------------------------

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