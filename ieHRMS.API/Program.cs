using ieHRMS.API.Middlewares;
using ieHRMS.Core.Config;
using ieHRMS.Core.DataAccess;
using ieHRMS.Core.Interface;
using ieHRMS.Core.Repository;
using ieHRMS.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using System.Reflection;
using BackgroundQueueSettings = ieHRMS.Core.Config.BackgroundQueueSettings;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;


var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────
// Add Framework Services
// ─────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ieHRMS API",
        Version = "v1",
        Description = "API documentation for ieHRMS"
    });

    // JWT Bearer Token Support
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token in the format: Bearer {your token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments (optional)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// ─────────────────────────────────────────────
// Register Core Services
// ─────────────────────────────────────────────
builder.Services.AddScoped<ICommonService, CommonServiceRepository>();
builder.Services.AddScoped<IEncryptDecrypt, EncryptDecryptRepository>();
builder.Services.AddScoped<IConversion, ConversionRepository>();
builder.Services.AddScoped<IUtilityRepository, UtilityRepository>();
builder.Services.AddScoped<ITenantResolverService, TenantResolverService>();
builder.Services.AddScoped<ITenantContextProvider, TenantContextProvider>();

// ─────────────────────────────────────────────
// Register Database Connections
// ─────────────────────────────────────────────
var dbConnRecruit = builder.Configuration.GetConnectionString("DBConnRecruitment");
builder.Services.AddScoped<AdoDataAccess>(_ => new AdoDataAccess(dbConnRecruit));
builder.Services.AddScoped<IDataService>(_ => new DataServiceRepository(new AdoDataAccess(dbConnRecruit), dbConnRecruit));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(dbConnRecruit));

builder.Services.AddIdentityCore<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// ─────────────────────────────────────────────
// JWT Token Configuration
// ─────────────────────────────────────────────
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddScoped<ITokenGenerator, TokenGenerator>();
// ─────────────────────────────────────────────
// Register Cookie Authentication
// ─────────────────────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "ieHRMS_Auth"; // Custom cookie name
        options.Cookie.HttpOnly = true; // Cannot access via JavaScript
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS only
        options.Cookie.SameSite = SameSiteMode.Strict; // Prevent cross-site sending
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.LoginPath = "/api/Candidate-SignIn"; // Optional - fallback path
    });

// ─────────────────────────────────────────────
// Background Queue Settings & Email Service
// ─────────────────────────────────────────────
builder.Services.Configure<BackgroundQueueSettings>(builder.Configuration.GetSection("BackgroundQueueSettings"));
builder.Services.AddOptions<BackgroundQueueSettings>()
    .Bind(builder.Configuration.GetSection("BackgroundQueueSettings"))
    .ValidateDataAnnotations();

builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddScoped<ITokenEmailService, TokenEmailService>();

// ─────────────────────────────────────────────
// CORS (Allow All - adjust in prod)
// ─────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// ─────────────────────────────────────────────
// Anti-Forgery (optional for forms)
// ─────────────────────────────────────────────
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "X-CSRF-TOKEN";
    options.Cookie.HttpOnly = false;
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var app = builder.Build();

// ─────────────────────────────────────────────
// Global Exception Handling Middleware
// ─────────────────────────────────────────────
app.UseExceptionHandler(config =>
{
    config.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"error\":\"An unexpected error occurred.\"}");
    });
});

// ─────────────────────────────────────────────
// Start background queue processing
// ─────────────────────────────────────────────
var queue = app.Services.GetRequiredService<IBackgroundTaskQueue>();
var lifetime = app.Lifetime;
_ = Task.Run(() => queue.ProcessQueueAsync(lifetime.ApplicationStopping));

// ─────────────────────────────────────────────
// Configure Middleware
// ─────────────────────────────────────────────
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseMiddleware<TokenValidationMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

if (builder.Configuration.GetValue<bool>("EnableSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ieHRMS API V1");
        c.RoutePrefix = string.Empty; // set "" to serve Swagger at root
    });
}

app.MapControllers();
 
app.Run();
