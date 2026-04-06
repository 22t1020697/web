using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using SV22T1020697.Shop;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký dịch vụ cho Controller và View
builder.Services.AddControllersWithViews()
                .AddMvcOptions(option =>
                {
                    // Cho phép các thuộc tính có thể null trong Model không bắt buộc phải có giá trị
                    option.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                });

// 2. Đăng ký Accessor để truy cập Session trong Layout/ViewComponent
builder.Services.AddHttpContextAccessor();

// 3. Cấu hình Cookie Authentication (Dành cho khách hàng đăng nhập)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(option =>
                {
                    option.Cookie.Name = "TNShop.Customer.Auth"; // Tên cookie riêng cho Shop
                    option.LoginPath = "/Customer/Login";       // Đường dẫn trang đăng nhập khách hàng
                    option.AccessDeniedPath = "/Customer/AccessDenied";
                    option.ExpireTimeSpan = TimeSpan.FromDays(7);
                    option.SlidingExpiration = true;
                    option.Cookie.HttpOnly = true;
                });

// 4. Cấu hình Session (Dành cho Giỏ hàng và lưu thông tin tạm thời)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromHours(2); // Thời gian chờ 2 giờ
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// 5. Cấu hình HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

// 6. Kích hoạt Authentication & Session (Thứ tự rất quan trọng)
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// 7. Cấu hình Route mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 8. Cấu hình định dạng ngôn ngữ Tiếng Việt (Tiền tệ, Ngày tháng)
var cultureInfo = new CultureInfo("vi-VN");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// 9. Khởi tạo Business Layer Configuration (KẾT NỐI DATABASE)
string connectionString = builder.Configuration.GetConnectionString("LiteCommerceDB")
    ?? throw new InvalidOperationException("ConnectionString 'LiteCommerceDB' not found.");

// Kích hoạt tầng nghiệp vụ
SV22T1020697.BusinessLayers.Configuration.Initialize(connectionString);
ApplicationContext.Configure(
    app.Services.GetRequiredService<IHttpContextAccessor>(),
    app.Services.GetRequiredService<IWebHostEnvironment>(),
    app.Configuration
);
app.Run();