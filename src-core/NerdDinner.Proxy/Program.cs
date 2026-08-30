using Microsoft.AspNetCore.Authentication.Cookies;
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

// [Authorize]-gated actions (Dinners Create/Edit/Delete, RSVP) need SOME
// configured auth scheme to challenge against, or ASP.NET Core throws
// rather than redirecting. This app doesn't share the legacy app's OWIN
// authentication cookie -- cross-app session handling is explicitly M10's
// job (Migrate Auth), not M9's. Same documented interim degradation
// pattern as M8's always-logged-out _LoginPartial (decision-log.md
// DL-022): a real user is never recognized as authenticated here yet,
// which is visible/honest rather than silently broken. LoginPath left at
// ASP.NET Core's own default ("/Account/Login") -- unmigrated, so it
// falls through the YARP catch-all to the legacy app's real login page,
// which still won't leave this app recognizing the user afterward.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

builder.Services.AddMemoryCache();
builder.Services.AddScoped<GeolocationService>();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Diagnostic-only endpoint, handled directly by the new app rather than
// proxied — proves this app is reachable through the proxy entry point on
// its own, distinct from the migrated routes below.
app.MapGet("/_proxy/health", () => Results.Text("NerdDinner.Proxy (ASP.NET Core) is alive"));

// M8: Home. M9: Dinners, RSVP (both conventional {controller}/{action}/{id}
// MVC routes), and Search (an "api/Search"-shaped route, not conventional
// MVC -- kept as its own mapping rather than shoehorned into the
// constraint list below). Scoped explicitly so every other path (Account,
// pending M10) falls through to the YARP catch-all rather than this app
// claiming routes it hasn't implemented yet.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    constraints: new { controller = "Home|Dinners|RSVP" });

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
