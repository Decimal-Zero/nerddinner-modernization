using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NerdDinner.Proxy.ModelBinders;
using NerdDinner.Proxy.Models;
using NerdDinner.Proxy.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
    {
        // Ported from the legacy app's DbGeographyModelBinder -- see
        // ModelBinders/PointModelBinder.cs.
        options.ModelBinderProviders.Insert(0, new PointModelBinderProvider());
    })
    // Keeps JSON property names as declared (PascalCase), matching the
    // legacy Web API controller's default Json.NET output -- NerdDinner.js
    // (still in use, carried over from M8) binds to "Title", "Url",
    // "RSVPCount", etc. verbatim. See decision-log.md DL-028.
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddDbContext<NerdDinnerCoreContext>(options =>
    options
        // Matches legacy EF6's default lazy-loading-on-virtual-navigation
        // behavior -- DinnersController/RSVPController port their bodies
        // over close to verbatim (db.Dinners.Find(id) then touching
        // dinner.RSVPs directly), which depends on that. EF Core doesn't
        // lazy-load by default; this restores the same semantics rather
        // than rewriting every call site to explicit .Include(...).
        .UseLazyLoadingProxies()
        .UseSqlServer(
            builder.Configuration.GetConnectionString("NerdDinnerContext"),
            sql => sql.UseNetTopologySuite()));

// M10 (decision-log.md DL-029): ASP.NET Core Identity replaces the M9
// placeholder cookie-only scheme. Points at the SAME shared
// "NerdDinner.Identity" LocalDB database the legacy app's ApplicationDbContext
// already uses -- same strangler-fig reuse principle as NerdDinnerCoreContext.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Matches the legacy app's ApplicationUserManager.Create policy
        // (App_Start/IdentityConfig.cs) -- itself deliberately matching
        // SimpleMembership's old, looser password/username policy rather
        // than Identity's stricter defaults (see decision-log.md DL-014).
        // Carried forward unchanged so registering a "new" account here
        // behaves identically to the legacy app's.
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.User.AllowedUserNameCharacters = null;
        options.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
});

var authBuilder = builder.Services.AddAuthentication();

// External login providers: keys stay externalized to config, same
// conditional-on-non-empty-secret wiring as the legacy app's
// Startup.Auth.cs. None are configured in this dev environment (all four
// ship blank in appsettings.json, same as the legacy Web.config) -- so
// these flows are structurally ported but not live-tested here, same
// documented limitation the legacy app's own test suite already had.
var googleClientId = builder.Configuration["googleClientId"];
var googleClientSecret = builder.Configuration["googleClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });
}

var microsoftClientId = builder.Configuration["microsoftClientId"];
var microsoftClientSecret = builder.Configuration["microsoftClientSecret"];
if (!string.IsNullOrEmpty(microsoftClientId) && !string.IsNullOrEmpty(microsoftClientSecret))
{
    authBuilder.AddMicrosoftAccount(options =>
    {
        options.ClientId = microsoftClientId;
        options.ClientSecret = microsoftClientSecret;
    });
}

var facebookAppId = builder.Configuration["facebookAppId"];
var facebookAppSecret = builder.Configuration["facebookAppSecret"];
if (!string.IsNullOrEmpty(facebookAppId) && !string.IsNullOrEmpty(facebookAppSecret))
{
    authBuilder.AddFacebook(options =>
    {
        options.AppId = facebookAppId;
        options.AppSecret = facebookAppSecret;
    });
}

var twitterConsumerKey = builder.Configuration["twitterConsumerKey"];
var twitterConsumerSecret = builder.Configuration["twitterConsumerSecret"];
if (!string.IsNullOrEmpty(twitterConsumerKey) && !string.IsNullOrEmpty(twitterConsumerSecret))
{
    authBuilder.AddTwitter(options =>
    {
        options.ConsumerKey = twitterConsumerKey;
        options.ConsumerSecret = twitterConsumerSecret;
        options.RetrieveUserDetails = true;
    });
}

builder.Services.AddMemoryCache();
builder.Services.AddScoped<GeolocationService>();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Identity's schema needs no seed data, same reasoning the legacy app's
// own DefaultConnection used (decision-log.md DL-018) -- EnsureCreated,
// not formal Migrations. Unlike NerdDinnerCoreContext (M9), this does
// NOT reuse the legacy app's existing "NerdDinner.Identity" database
// as-is: ASP.NET Core Identity's schema (NormalizedUserName,
// ConcurrencyStamp, LockoutEnd as DateTimeOffset, etc.) isn't
// wire-compatible with ASP.NET Identity 2.x/EF6's schema that database
// had. See decision-log.md DL-029 for the live failure that found this
// and why a fresh schema (no real user data existed to preserve) was
// the right fix, consistent with DL-014's identical reasoning at M4.
using (var scope = app.Services.CreateScope())
{
    var identityDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    identityDb.Database.EnsureCreated();
}

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Diagnostic-only endpoint, handled directly by the new app rather than
// proxied — proves this app is reachable through the proxy entry point on
// its own, distinct from the migrated routes below.
app.MapGet("/_proxy/health", () => Results.Text("NerdDinner.Proxy (ASP.NET Core) is alive"));

// M8: Home. M9: Dinners, RSVP. M10: Account -- all conventional
// {controller}/{action}/{id} MVC routes. Search is an "api/Search"-shaped
// route, not conventional MVC -- kept as its own mapping rather than
// shoehorned into the constraint list below. With Account now migrated,
// every legacy controller (Home, Dinners, RSVP, Search, Account) is
// served by this app -- the YARP catch-all below is now a true fallback
// for genuinely nonexistent routes, not an active handoff to a still-live
// legacy feature, per M11's eventual decommission.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    constraints: new { controller = "Home|Dinners|RSVP|Account" });

// Search is attribute-routed ([Route("api/Search")] on the controller
// itself, per SearchController.cs), registered via MapControllers rather
// than a conventional MapControllerRoute.
app.MapControllers();

app.MapReverseProxy();

app.Run();

// Exposes the generated top-level-statements Program class to
// NerdDinner.Proxy.Tests, which hosts this app for real via
// WebApplicationFactory<Program> rather than testing controllers in
// isolation.
public partial class Program { }
