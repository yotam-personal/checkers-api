using ChekersAPI;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// The SPA and this API are served from one origin (checkers.yotam.xyz, with the API under
// /api), so same-origin requests never reach this policy at all. It stays for the two
// origins that are genuinely cross-site: a developer's ng serve, and the Firebase copy of
// the front end, which remains the rollback until the migration is finished.
builder.Services.AddCors(options =>
{
    options.AddPolicy("MyAllowSpecificOrigins",
                      policy =>
                      {
                          policy.WithOrigins(
                              "http://localhost:4200",
                              "https://checkers.yotam.xyz",
                              "https://checkers-198a5.web.app",
                              "https://yotam.xyz"
                              )
                                 .AllowAnyHeader()
                                 .AllowAnyMethod();
                      });
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.TypeInfoResolver = new MyJsonSerializerContext();
});

builder.Services.AddHostedService<DatabaseStartup>();

var app = builder.Build();

// The ingress routes /api here without rewriting it, so the application has to know it
// lives under that prefix. Doing it with PathBase rather than by prefixing every route
// keeps the routes identical to how they run locally, and keeps the front end's paths the
// same whether it is talking to a local ng serve or to the cluster.
string pathBase = Environment.GetEnvironmentVariable("PATH_BASE") ?? "/api";
if (!string.IsNullOrEmpty(pathBase) && pathBase != "/")
{
    app.UsePathBase(pathBase);
}

app.UseCors("MyAllowSpecificOrigins");
app.UseRouting();
app.UseHttpMetrics();

// Deliberately independent of the database. This is both the liveness and the readiness
// probe, and a probe that fails when Postgres is unreachable would restart — then refuse
// to serve — an application that can still play a full game of checkers.
app.MapGet("/healthz", () => Results.Text("ok"));
app.MapMetrics();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
