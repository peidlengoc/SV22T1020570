using Microsoft.AspNetCore.Authentication.Cookies;
using SV22T1020570.BusinessLayers;
using SV22T1020570.DataLayers.Interfaces;
using SV22T1020570.DataLayers.SQLServer;
using SV22T1020570.Shop;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// ================= SERVICES =================

// HttpContext
builder.Services.AddHttpContextAccessor();

// MVC
builder.Services.AddControllersWithViews()
    .AddMvcOptions(option =>
    {
        option.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    });


// 🔥 AUTH CHO SHOP (QUAN TRỌNG NHẤT)
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

// SESSION (dùng cho giỏ hàng sau này)
builder.Services.AddSession(option =>
{
    option.IdleTimeout = TimeSpan.FromHours(2);
    option.Cookie.HttpOnly = true;
    option.Cookie.IsEssential = true;
});
builder.Services.AddControllersWithViews();

// ====== ADD Ở ĐÂY ======

builder.Services.AddScoped<IShoppingCartRepository>(sp =>
    new ShoppingCartRepository(Configuration.ConnectionString));

builder.Services.AddScoped<IProductRepository>(sp =>
    new ProductRepository(Configuration.ConnectionString));
builder.Services.AddScoped<ShoppingCartDBService>();


var app = builder.Build();

// ================= PIPELINE =================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

// 🔥 BẮT BUỘC (thiếu là login không chạy)
app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

// ================= ROUTING =================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// ================= CULTURE =================

var cultureInfo = new CultureInfo("vi-VN");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// ================= APPLICATION CONTEXT =================

ApplicationContext.Configure(
    httpContextAccessor: app.Services.GetRequiredService<IHttpContextAccessor>(),
    webHostEnvironment: app.Services.GetRequiredService<IWebHostEnvironment>(),
    configuration: app.Configuration
);

// ================= DATABASE =================

string connectionString = builder.Configuration.GetConnectionString("LiteCommerceDB")
    ?? throw new InvalidOperationException("ConnectionString not found");

Configuration.Initialize(connectionString);

// ================= RUN =================

app.Run();