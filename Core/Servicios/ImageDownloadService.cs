using CodigoLimpio.Core.DTOs;

namespace CodigoLimpio.Core.Servicios;

public class ImageDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly int _maxConcurrentDownloads;

    public ImageDownloadService(HttpClient httpClient, int maxConcurrentDownloads = 5)
    {
        _httpClient = httpClient;
        _maxConcurrentDownloads = maxConcurrentDownloads;
    }

    public async Task<List<ProductoDto>> DescargarImagenesAsync(
        List<ProductoDto> productos,
        string carpetaDestino,
        IProgress<int>? progress = null)
    {
        // Crear carpeta para imágenes
        var carpetaImagenes = Path.Combine(carpetaDestino, "Imagenes");
        if (!Directory.Exists(carpetaImagenes))
        {
            Directory.CreateDirectory(carpetaImagenes);
        }

        var productosConImagenLocal = new List<ProductoDto>();
        int descargadas = 0;
        int errores = 0;

        // Usar SemaphoreSlim para controlar concurrencia
        using var semaphore = new SemaphoreSlim(_maxConcurrentDownloads);
        var tasks = new List<Task>();

        foreach (var producto in productos)
        {
            await semaphore.WaitAsync();

            var task = DescargarImagenProductoAsync(producto, carpetaImagenes)
                .ContinueWith(t =>
                {
                    semaphore.Release();

                    lock (productosConImagenLocal)
                    {
                        if (t.Result.imagenLocal)
                        {
                            descargadas++;
                            productosConImagenLocal.Add(new ProductoDto(
                                t.Result.rutaLocal,
                                producto.Precio,
                                producto.Descripcion
                            ));
                        }
                        else
                        {
                            errores++;
                            // Si falla la descarga, mantener la URL original
                            productosConImagenLocal.Add(producto);
                        }
                    }

                    progress?.Report(descargadas + errores);
                });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        Console.WriteLine($"\nDescarga de imágenes completada:");
        Console.WriteLine($"   Exitosas: {descargadas}");
        Console.WriteLine($"   Fallidas: {errores}");
        Console.WriteLine($"   Ubicación: {carpetaImagenes}");

        return productosConImagenLocal;
    }

    private async Task<(bool imagenLocal, string rutaLocal)> DescargarImagenProductoAsync(
        ProductoDto producto,
        string carpetaImagenes)
    {
        try
        {
            if (string.IsNullOrEmpty(producto.ImagenUrl))
            {
                return (false, string.Empty);
            }

            // Generar nombre de archivo seguro
            string nombreArchivo = GenerarNombreArchivoSeguro(producto.Descripcion);
            string extension = ObtenerExtension(producto.ImagenUrl);
            string rutaCompleta = Path.Combine(carpetaImagenes, $"{nombreArchivo}{extension}");

            // Si ya existe, no descargar de nuevo
            if (File.Exists(rutaCompleta))
            {
                return (true, rutaCompleta);
            }

            // Descargar imagen
            var response = await _httpClient.GetAsync(producto.ImagenUrl);

            if (response.IsSuccessStatusCode)
            {
                await using var stream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = File.Create(rutaCompleta);
                await stream.CopyToAsync(fileStream);

                return (true, rutaCompleta);
            }
            else
            {
                Console.WriteLine($"   ⚠️ Error HTTP {response.StatusCode}: {producto.Descripcion}");
                return (false, producto.ImagenUrl);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️ Error descargando imagen de '{producto.Descripcion}': {ex.Message}");
            return (false, producto.ImagenUrl);
        }
    }

    private string GenerarNombreArchivoSeguro(string descripcion)
    {
        // Limpiar el nombre para que sea válido como archivo
        var nombreLimpio = descripcion
            .Replace(" ", "_")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace(":", "-")
            .Replace("*", "")
            .Replace("?", "")
            .Replace("\"", "")
            .Replace("<", "")
            .Replace(">", "")
            .Replace("|", "")
            .Replace(",", "")
            .Replace(".", "")
            .Trim();

        // Limitar longitud
        if (nombreLimpio.Length > 100)
        {
            nombreLimpio = nombreLimpio.Substring(0, 100);
        }

        // Agregar timestamp para evitar colisiones
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return $"{nombreLimpio}_{timestamp}";
    }

    private string ObtenerExtension(string url)
    {
        try
        {
            var uri = new Uri(url);
            var extension = Path.GetExtension(uri.AbsolutePath).ToLower();

            // Si no tiene extensión o es muy larga, usar .jpg por defecto
            if (string.IsNullOrEmpty(extension) || extension.Length > 5)
            {
                return ".jpg";
            }

            return extension;
        }
        catch
        {
            return ".jpg";
        }
    }
}