using Microsoft.AspNetCore.Authentication.Cookies;
using SV22T1020570.BusinessLayers;
using SV22T1020570.DataLayers.Interfaces;
using SV22T1020570.DataLayers.SQLServer;
using SV22T1020570.Shop;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddHttpContextAccessor();

builder.Services.AddControllersWithViews()
    .AddMvcOptions(option =>
    {
        option.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    });


builder.Services.AddAuthentication("ShopScheme")
    .AddCookie("ShopScheme", option =>
    {
        option.Cookie.Name = "LiteCommerce.Shop";   // cookie riêng
        option.LoginPath = "/Account/Login";
        option.AccessDeniedPath = "/Account/AccessDenied";
        option.ExpireTimeSpan = TimeSpan.FromDays(7);
        option.SlidingExpiration = true;
        option.Cookie.HttpOnly = true;
        option.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });


builder.Services.AddSession(option =>
{
    option.IdleTimeout = TimeSpan.FromHours(2);
    option.Cookie.HttpOnly = true;
    option.Cookie.IsEssential = true;
});
builder.Services.AddControllersWithViews();



builder.Services.AddScoped<IShoppingCartRepository>(sp =>
    new ShoppingCartRepository(Configuration.ConnectionString));

builder.Services.AddScoped<IProductRepository>(sp =>
    new ProductRepository(Configuration.ConnectionString));
builder.Services.AddScoped<ShoppingCartDBService>();


var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");


var cultureInfo = new CultureInfo("vi-VN");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;



ApplicationContext.Configure(
    httpContextAccessor: app.Services.GetRequiredService<IHttpContextAccessor>(),
    webHostEnvironment: app.Services.GetRequiredService<IWebHostEnvironment>(),
    configuration: app.Configuration
);


string connectionString = builder.Configuration.GetConnectionString("LiteCommerceDB")
    ?? throw new InvalidOperationException("ConnectionString not found");

Configuration.Initialize(connectionString);


app.Run();