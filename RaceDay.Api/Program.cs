using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;
using RaceDay.Api.Security;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enums (e.g. none currently, kept for future use) as strings
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        // Keep response JSON property names exactly matching the C# DTO
        // property names (PascalCase) instead of ASP.NET Core's default
        // camelCase output policy. The default only affects OUTPUT - input
        // binding is already case-insensitive - but a mismatched OUTPUT
        // policy would silently break case-sensitive clients (including the
        // System.Net.Http.Json client used by the xUnit integration tests),
        // which would deserialise {"role":"Organiser"} into a null Role
        // property. Disabling the policy here keeps client and server in
        // exact agreement with zero surprises.
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// EF Core - Code First against the RaceDayDB schema designed in Part 1.
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayDb")));

// Server-side session store: the session cookie holds only a session ID,
// the actual UserId/Role/FullName values are kept server-side in the
// distributed cache (in-memory by default), as required by the PoE brief.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDay.Session";
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddHttpContextAccessor();

// Password hashing (PBKDF2 via ASP.NET Core Identity's PasswordHasher) -
// passwords are never stored or compared in plain text. See
// Security/PasswordHasherUser.cs for why a marker type is enough here.
builder.Services.AddSingleton<IPasswordHasher<PasswordHasherUser>, PasswordHasher<PasswordHasherUser>>();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "RaceDay Event Management System - RESTful API for the South African road running, " +
                       "walking and cycling community. Built against the Part 1 ERD and API Endpoint Plan."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// ---------------------------------------------------------------------
// Middleware pipeline
// ---------------------------------------------------------------------

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
});

app.UseHttpsRedirection();

// Session must be registered before routing/endpoints so the session
// store is available to the SessionAuthorize filter on every request.
// NOTE: authorization here is enforced by the custom SessionAuthorize
// action filter (see Security/SessionAuthorizeAttribute.cs), not the
// built-in ASP.NET Core [Authorize] pipeline, so app.UseAuthorization()
// is deliberately not registered (it requires AddAuthorization() and
// would throw at startup otherwise, for functionality we don't use).
app.UseSession();

app.MapControllers();

app.Run();

// Exposed so RaceDay.Api.Tests can boot this app via WebApplicationFactory<Program>.
public partial class Program { }
