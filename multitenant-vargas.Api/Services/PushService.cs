using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace multitenant_vargas.Api.Services;

/// <summary>
/// Envio de push con Firebase Admin SDK.
///
/// La cuenta de servicio NUNCA va en el repositorio. Se toma de:
///   1) Firebase:CredentialsJson (JSON completo en la variable de entorno del hosting)
///   2) Firebase:CredentialsPath (ruta a un archivo fuera del repo / User Secrets)
///   3) GOOGLE_APPLICATION_CREDENTIALS
///
/// Si no hay credenciales, la app sigue funcionando y solo se avisa por log.
/// </summary>
public sealed class PushService(IConfiguration configuration, ILogger<PushService> logger)
{
    public const string CanalAndroid = "vargas_importantes";
    public const string IconoAndroid = "ic_stat_icon_config_sample";
    public const string ColorAndroid = "#0B6BCB";

    private static readonly SemaphoreSlim Candado = new(1, 1);
    private static bool credencialesIntentadas;
    private static FirebaseMessaging? messaging;

    public bool Configurado => messaging is not null;

    public async Task<IReadOnlyList<string>> EnviarAsync(
        IReadOnlyCollection<string> tokens,
        string titulo,
        string mensaje,
        long? pedidoId,
        CancellationToken ct = default)
    {
        if (tokens.Count == 0) return [];
        var cliente = await ObtenerMessagingAsync();
        if (cliente is null) return [];

        var datos = new Dictionary<string, string>
        {
            ["tipo"] = "pedido",
            ["pedido_id"] = pedidoId?.ToString() ?? string.Empty,
        };

        var multicast = new MulticastMessage
        {
            Tokens = tokens.ToList(),
            Notification = new Notification { Title = titulo, Body = mensaje },
            Data = datos,
            Android = new AndroidConfig
            {
                Priority = Priority.High,
                TimeToLive = TimeSpan.FromHours(12),
                Notification = new AndroidNotification
                {
                    Title = titulo,
                    Body = mensaje,
                    ChannelId = CanalAndroid,
                    Icon = IconoAndroid,
                    Color = ColorAndroid,
                    Sound = "default",
                    DefaultVibrateTimings = true,
                },
            },
        };

        var respuesta = await cliente.SendEachForMulticastAsync(multicast, ct);
        if (respuesta.FailureCount == 0)
        {
            logger.LogInformation("Push enviada a {Cantidad} telefonos.", respuesta.SuccessCount);
            return [];
        }

        var invalidos = new List<string>();
        for (var i = 0; i < respuesta.Responses.Count; i++)
        {
            var intento = respuesta.Responses[i];
            if (intento.IsSuccess) continue;
            var codigo = intento.Exception?.MessagingErrorCode;
            // Tokens viejos o desinstalados: se borran para no reintentar siempre.
            if (codigo is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
                invalidos.Add(tokens.ElementAt(i));
            logger.LogWarning("Push rechazada ({Codigo}): {Mensaje}", codigo, intento.Exception?.Message);
        }

        logger.LogInformation("Push: {Ok} ok, {Error} errores, {Invalidos} tokens a borrar.",
            respuesta.SuccessCount, respuesta.FailureCount, invalidos.Count);
        return invalidos;
    }

    private async Task<FirebaseMessaging?> ObtenerMessagingAsync()
    {
        if (messaging is not null || credencialesIntentadas) return messaging;
        await Candado.WaitAsync();
        try
        {
            if (messaging is not null || credencialesIntentadas) return messaging;
            credencialesIntentadas = true;

            var json = configuration["Firebase:CredentialsJson"];
            var ruta = configuration["Firebase:CredentialsPath"];
            var rutaEntorno = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

            GoogleCredential? credencial = null;
            if (!string.IsNullOrWhiteSpace(json))
                credencial = GoogleCredential.FromJson(json);
            else if (!string.IsNullOrWhiteSpace(ruta))
                credencial = GoogleCredential.FromFile(ruta);
            else if (!string.IsNullOrWhiteSpace(rutaEntorno))
                credencial = GoogleCredential.FromFile(rutaEntorno);

            if (credencial is null)
            {
                logger.LogWarning("Firebase sin credenciales: las push quedan deshabilitadas. " +
                    "Configure Firebase:CredentialsJson o Firebase:CredentialsPath.");
                return null;
            }

            FirebaseApp app;
            try
            {
                app = FirebaseApp.GetInstance("vargas");
            }
            catch (InvalidOperationException)
            {
                app = FirebaseApp.Create(new AppOptions
                {
                    Credential = credencial,
                    ProjectId = configuration["Firebase:ProjectId"],
                }, "vargas");
            }
            messaging = FirebaseMessaging.GetMessaging(app);
            logger.LogInformation("Firebase Admin inicializado para push.");
            return messaging;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo inicializar Firebase Admin; las push quedan deshabilitadas.");
            messaging = null;
            return null;
        }
        finally
        {
            Candado.Release();
        }
    }
}
