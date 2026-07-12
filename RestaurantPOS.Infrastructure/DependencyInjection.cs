using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Interfaces;
using RestaurantPOS.Infrastructure.Data;
using RestaurantPOS.Infrastructure.Identity;
using RestaurantPOS.Infrastructure.Repositories;
using RestaurantPOS.Infrastructure.Services;
using System.Text;

namespace RestaurantPOS.Infrastructure;

/// <summary>Infrastructure layer DI registration extension.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // --- Entity Framework Core ---
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql =>
                {
                    sql.UseCompatibilityLevel(140);
                    sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    sql.EnableRetryOnFailure(3);
                    sql.CommandTimeout(30);
                }));

        // --- ASP.NET Identity ---
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // --- JWT Authentication ---
        var jwtKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured.");
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        // --- Caching ---
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(o => o.Configuration = redisConnection);
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        // --- Hangfire ---
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection"),
                new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                }));
        services.AddHangfireServer();

        // --- SignalR ---
        services.AddSignalR();

        // --- HTTP Clients ---
        services.AddHttpClient("Zatca", client =>
        {
            client.BaseAddress = new Uri(configuration["Zatca:BaseUrl"] ?? "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // --- Core services ---
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddSingleton<IDateTimeService, DateTimeService>();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<INumberGenerator, NumberGenerator>();
        services.AddScoped<IZatcaService, ZatcaService>();
        services.AddScoped<IExcelExportService, ExcelExportService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPdfExportService, PdfExportService>();
        services.AddScoped<Application.Interfaces.IIdentityService, IdentityService>();

        return services;
    }

    public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder app)
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            DashboardTitle = "RestaurantPOS Jobs",
            Authorization = [new HangfireAuthorizationFilter()]
        });

        return app;
    }
}

/// <summary>Restrict Hangfire dashboard to admin users only.</summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        return http.User.IsInRole("Admin") || http.User.IsInRole("SuperAdmin") || !http.User.Identity?.IsAuthenticated == true;
    }
}
