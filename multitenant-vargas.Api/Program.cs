using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net;
using Microsoft.EntityFrameworkCore;
using multitenant_vargas.Api.Data;

var builder = WebApplication.CreateBuilder(args);

const string corsPolicy = "IonicDevelopment";

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("No se configuro ConnectionStrings:Default.");

builder.Services.AddControllers().AddJsonOptions(options => ConfigurarJson(options.JsonSerializerOptions));
builder.Services.ConfigureHttpJsonOptions(options => ConfigurarJson(options.SerializerOptions));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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

var app = builder.Build();
app.UseCors(corsPolicy);
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTimeOffset.UtcNow }));

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