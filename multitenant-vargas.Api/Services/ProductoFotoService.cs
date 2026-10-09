namespace multitenant_vargas.Api.Services;

public sealed class ProductoFotoService(IWebHostEnvironment environment, ILogger<ProductoFotoService> logger)
{
    public const long MaxBytes = 5 * 1024 * 1024;
    private string Carpeta => Path.Combine(environment.WebRootPath, "uploads", "productos");

    public async Task<(string? Url, string? Error)> GuardarAsync(IFormFile? foto, CancellationToken ct)
    {
        if (foto is null) return (null, null);
        if (foto.Length <= 0 || foto.Length > MaxBytes)
            return (null, "La foto debe pesar entre 1 byte y 5 MiB.");

        var extension = foto.ContentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => null
        };
        if (extension is null) return (null, "Solo se permiten imagenes JPEG, PNG o WebP.");

        await using var input = foto.OpenReadStream();
        var firma = new byte[12];
        var leidos = await input.ReadAtLeastAsync(firma, 12, throwOnEndOfStream: false, cancellationToken: ct);
        var valida = extension switch
        {
            ".jpg" => leidos >= 3 && firma[0] == 0xff && firma[1] == 0xd8 && firma[2] == 0xff,
            ".png" => leidos >= 8 && firma.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".webp" => leidos == 12 && firma.AsSpan(0, 4).SequenceEqual("RIFF"u8) && firma.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
        if (!valida) return (null, "La firma del archivo no coincide con el tipo de imagen.");

        Directory.CreateDirectory(Carpeta);
        var url = $"/uploads/productos/{Guid.NewGuid():N}{extension}";
        try
        {
            await using var output = new FileStream(Path.Combine(Carpeta, Path.GetFileName(url)), FileMode.CreateNew);
            await output.WriteAsync(firma.AsMemory(0, leidos), ct);
            var buffer = new byte[81920];
            long total = leidos;
            int count;
            while ((count = await input.ReadAsync(buffer, ct)) > 0)
            {
                total += count;
                if (total > MaxBytes) throw new InvalidDataException("La foto supera 5 MiB.");
                await output.WriteAsync(buffer.AsMemory(0, count), ct);
            }
            return (url, null);
        }
        catch (InvalidDataException ex)
        {
            Borrar(url);
            return (null, ex.Message);
        }
        catch
        {
            Borrar(url);
            throw;
        }
    }

    public void Borrar(string? url)
    {
        if (url is null || !url.StartsWith("/uploads/productos/", StringComparison.Ordinal)) return;
        try { File.Delete(Path.Combine(Carpeta, Path.GetFileName(url))); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "No se pudo limpiar la foto {FotoUrl}.", url);
        }
    }
}
