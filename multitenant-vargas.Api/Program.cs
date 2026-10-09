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
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

const string corsPolicy = "IonicDevelopment";

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("No se configuro ConnectionStrings:Default.");

builder.Services.AddControllers().AddJsonOptions(options => ConfigurarJson(options.JsonSerializerOptions));
builder.Services.Configure<ApiBehaviorOptions>(options =>
    options.InvalidModelStateResponseFactory = _ => new BadRequestObjectResult(new
    {
        codigo = "solicitud_invalida", mensaje = "Los campos de la solicitud son invalidos o faltan campos obligatorios."
    }));
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
builder.Services.AddScoped<ProductoFotoService>();
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
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { codigo = "no_autenticado", mensaje = "Se requiere una sesion valida." });
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { codigo = "sin_permiso", mensaje = "No tiene permiso para esta operacion." });
            },
            OnTokenValidated = async context =>
            {
                var idTexto = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!long.TryParse(idTexto, out var id)) { context.Fail("Identidad inválida."); return; }
                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var actual = await AmbitoService.UsuariosConRoles(db).AsNoTracking().Where(x => x.Id == id)
                    .FirstOrDefaultAsync(context.HttpContext.RequestAborted);
                if (actual is null || context.Principal?.FindFirstValue("ambito_version") != "1" ||
                    !AmbitoService.Valido(actual, new(context.Principal?.FindFirstValue(ClaimTypes.Role), AmbitoService.EmpresaId(context.Principal!))))
                { context.Fail("El ambito del usuario cambio; vuelva a iniciar sesion."); return; }
                var sesionHash = context.Principal!.FindFirstValue("sesion");
                var empresa = AmbitoService.EmpresaId(context.Principal!);
                var rol = context.Principal!.FindFirstValue(ClaimTypes.Role);
                var ahora = DateTimeOffset.UtcNow;
                if (!await db.RefreshTokens.AsNoTracking().AnyAsync(x => x.UsuarioId == id && x.TokenHash == sesionHash &&
                    x.VersionAmbito == 1 && x.EmpresaId == empresa && x.RolSesion == rol && x.RevocadoEn == null &&
                    x.ExpiraEn > ahora, context.HttpContext.RequestAborted)) context.Fail("La sesion fue revocada.");
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

QuestPDF.Settings.License = LicenseType.Community;
var webRoot = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(webRoot, "uploads", "productos"));
builder.Environment.WebRootPath = webRoot;
builder.Environment.WebRootFileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webRoot);
var app = builder.Build();
app.UseExceptionHandler(error => error.Run(async context =>
{
    var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    var status = exception is BadHttpRequestException badRequest
        ? badRequest.StatusCode : StatusCodes.Status500InternalServerError;
    context.Response.StatusCode = status;
    await context.Response.WriteAsJsonAsync(new
    {
        codigo = status == 500 ? "error_interno" : $"http_{status}",
        mensaje = status == 413 ? "La solicitud supera el limite permitido." : "No se pudo completar la operacion."
    });
}));
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    await response.WriteAsJsonAsync(new
    {
        codigo = $"http_{response.StatusCode}",
        mensaje = response.StatusCode == 413 ? "La solicitud supera el limite permitido." : "No se pudo procesar la solicitud."
    });
});
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

public partial class Program { }
