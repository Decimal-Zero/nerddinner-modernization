var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseStaticFiles();

// Diagnostic-only endpoint, handled directly by the new app rather than
// proxied — proves this app is reachable through the proxy entry point on
// its own, distinct from the migrated routes below.
app.MapGet("/_proxy/health", () => Results.Text("NerdDinner.Proxy (ASP.NET Core) is alive"));

// M8: Home is the first controller migrated off the legacy app. Scoped to
// controller=Home only (not a generic {controller}/{action} route) so every
// other path — including "/", handled by nothing else here — falls through
// to the YARP catch-all below rather than this app claiming routes it
// hasn't actually implemented yet.
app.MapControllerRoute(
    name: "home",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    constraints: new { controller = "Home" });

app.MapReverseProxy();

app.Run();

// Exposes the generated top-level-statements Program class to
// NerdDinner.Proxy.Tests, which hosts this app for real via
// WebApplicationFactory<Program> rather than testing controllers in
// isolation.
public partial class Program { }
