using ieRecruitment.UI;
using Microsoft.AspNetCore.Antiforgery;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────
// Services
// ─────────────────────────────────────────────────────

builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

builder.Services.AddHttpClient();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// ✅ Required for CSRF
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN"; // JS will read from cookie and send in header
    options.Cookie.Name = "XSRF-TOKEN";  // Name of the cookie set to browser
    options.Cookie.HttpOnly = false;     // Allow JavaScript to read it
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Or Always in production
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<TenantRouteTransformer>();

var app = builder.Build();

// ─────────────────────────────────────────────────────
// Middleware
// ─────────────────────────────────────────────────────

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();

app.UseRouting();
app.UseAuthorization();

// ✅ Antiforgery Token Injection Middleware
app.Use(async (context, next) =>
{
    var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
    var tokens = antiforgery.GetAndStoreTokens(context);

    context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
        new CookieOptions
        {
            HttpOnly = false, // Must be readable by JS
            Secure = app.Environment.IsProduction(), // Use true in production
            SameSite = SameSiteMode.Strict
        });

    await next();
});

// ─────────────────────────────────────────────────────
// Routing
// ─────────────────────────────────────────────────────

app.MapDynamicControllerRoute<TenantRouteTransformer>(
    pattern: "{tenantUrl}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.Run();
