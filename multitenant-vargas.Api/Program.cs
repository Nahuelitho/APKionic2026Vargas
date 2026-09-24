using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using multitenant_vargas.Api.Data;
using multitenant_vargas.Api.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

const string corsPolicy = "IonicDevelopment";

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("No se configuro ConnectionStrings:Default.");

builder.Services.AddControllers().AddJsonOptions(options => ConfigurarJson(options.JsonSerializerOptions));
builder.Services.ConfigureHttpJsonOptions(options => ConfigurarJson(options.SerializerOptions));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
    {
        policy
            .SetIsOriginAllowed(EsOrigenDesarrolloPermitido)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 0))
    ).UseSnakeCaseNamingConvention()
);
builder.Services.AddScoped<TokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = TokenService.ObtenerClave(builder.Configuration),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var idTexto = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!long.TryParse(idTexto, out var id)) { context.Fail("Identidad inválida."); return; }
                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var actual = await db.Usuarios.AsNoTracking().Where(x => x.Id == id)
                    .Select(x => new { x.Activo, Rol = x.Rol == null || !x.Rol.Activo ? null : x.Rol.Codigo })
                    .FirstOrDefaultAsync(context.HttpContext.RequestAborted);
                if (actual is null || !actual.Activo) { context.Fail("El usuario ya no está activo."); return; }
                if (actual.Rol != context.Principal?.FindFirstValue(ClaimTypes.Role))
                    context.Fail("El rol del usuario cambió; volvé a iniciar sesión.");
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();
app.UseCors(corsPolicy);
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTimeOffset.UtcNow })).AllowAnonymous();

app.Run();

static void ConfigurarJson(JsonSerializerOptions options)
{
    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
}
static bool EsOrigenDesarrolloPermitido(string origin)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;

    if (uri.Scheme is "capacitor" or "ionic" && uri.Host == "localhost") return true;

    if (uri.Scheme is not ("http" or "https")) return false;

    if (uri.Host is "localhost" or "127.0.0.1") return true;

    return IPAddress.TryParse(uri.Host, out var ip) && EsIpPrivada(ip);
}

static bool EsIpPrivada(IPAddress ip)
{
    if (IPAddress.IsLoopback(ip)) return true;

    var bytes = ip.GetAddressBytes();

    return ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && (
        bytes[0] == 10 ||
        bytes[0] == 192 && bytes[1] == 168 ||
        bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31
    );
}
