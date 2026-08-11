using CustomerDirectory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using CustomerDirectory.Application.Services;
using CustomerDirectory.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<ICustomerService, CustomerService>();
// Add the DbContext with SQLite provider
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));
// Add controllers with views
builder.Services.AddControllersWithViews();
// Add antiforgery services with a custom header name
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
// Add a global filter to automatically validate antiforgery tokens for unsafe HTTP methods
builder.Services.AddControllers(options =>
{
    options.Filters.Add<CustomerDirectory.Web.Filters.AutoValidateAntiforgeryTokenFilter>();
});
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DbInitializer.Seed(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
