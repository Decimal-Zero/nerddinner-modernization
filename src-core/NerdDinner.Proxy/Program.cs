var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Diagnostic-only endpoint, handled directly by the new app rather than
// proxied — proves this app is reachable through the proxy entry point on
// its own, distinct from the "no routes migrated yet" legacy catch-all
// below. Not one of M8+'s migrated routes.
app.MapGet("/_proxy/health", () => Results.Text("NerdDinner.Proxy (ASP.NET Core) is alive"));

app.MapReverseProxy();

app.Run();
