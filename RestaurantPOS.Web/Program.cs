using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using RestaurantPOS.Application;
using RestaurantPOS.Infrastructure;
using RestaurantPOS.Infrastructure.Data.Seed;
using RestaurantPOS.Infrastructure.Services;
using Serilog;
using Serilog.Events;
using System.Globalization;

// --- Serilog Bootstrap ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // --- Serilog ---
    builder.Host.UseSerilog((ctx, services, configuration) => configuration
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // --- Application + Infrastructure layers ---
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Infrastructure registers JWT as the default auth scheme (for the API).
    // For the Web (Razor Pages) app we need cookie/Identity as the default so
    // SignInManager and [Authorize] work correctly in the browser.
    builder.Services.PostConfigure<AuthenticationOptions>(opts =>
    {
        opts.DefaultScheme = IdentityConstants.ApplicationScheme;
        opts.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
        opts.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
        opts.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        opts.DefaultSignOutScheme = IdentityConstants.ApplicationScheme;
        opts.DefaultForbidScheme = IdentityConstants.ApplicationScheme;
    });

    // --- Razor Pages with antiforgery ---
    builder.Services.AddRazorPages(options =>
    {
        options.Conventions.AuthorizeFolder("/");
        options.Conventions.AllowAnonymousToPage("/Account/Login");
        options.Conventions.AllowAnonymousToPage("/Account/ForgotPassword");
    }).AddViewLocalization();

    // --- Localization ---
    builder.Services.AddLocalization(opts => opts.ResourcesPath = "Resources");
    builder.Services.Configure<RequestLocalizationOptions>(opts =>
    {
        var cultures = new[] { new CultureInfo("ar"), new CultureInfo("en") };
        opts.DefaultRequestCulture = new RequestCulture("ar");
        opts.SupportedCultures = cultures;
        opts.SupportedUICultures = cultures;
        opts.RequestCultureProviders =
        [
            new CookieRequestCultureProvider(),
            new AcceptLanguageHeaderRequestCultureProvider()
        ];
    });

    // --- Health Checks ---
    builder.Services.AddHealthChecks()
        .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!,
            name: "sql-server", tags: ["db", "ready"]);

    // --- Session ---
    builder.Services.AddSession(o =>
    {
        o.IdleTimeout = TimeSpan.FromHours(8);
        o.Cookie.HttpOnly = true;
        o.Cookie.IsEssential = true;
    });

    // --- HTTPS ---
    builder.Services.AddHsts(o =>
    {
        o.MaxAge = TimeSpan.FromDays(365);
        o.IncludeSubDomains = true;
    });

    var app = builder.Build();

    // --- Seed database ---
    await DatabaseSeeder.SeedAsync(app.Services);

    // --- Middleware pipeline ---
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }
    else
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseRequestLocalization();
    app.UseSerilogRequestLogging();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseSession();
    app.UseInfrastructure();

    app.MapRazorPages();
    app.MapHub<PosHub>("/hubs/pos");
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
