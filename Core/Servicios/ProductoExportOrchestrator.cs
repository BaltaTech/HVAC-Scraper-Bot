using CodigoLimpio.Core.DTOs;
using CodigoLimpio.Core.Interfaces;

namespace CodigoLimpio.Core.Servicios;

public class ProductoExportOrchestrator
{
    private readonly IEnumerable<IProductoExportService> _exportServices;
    private readonly ImageDownloadService _imageDownloadService;

    public ProductoExportOrchestrator(
        IEnumerable<IProductoExportService> exportServices,
        ImageDownloadService imageDownloadService)
    {
        _exportServices = exportServices;
        _imageDownloadService = imageDownloadService;
    }

    public async Task ExportarConImagenesAsync(
        List<ProductoDto> productos,
        string carpetaDestino,
        bool descargarImagenes = true)
    {
        if (!Directory.Exists(carpetaDestino))
        {
            Directory.CreateDirectory(carpetaDestino);
        }

        List<ProductoDto> productosFinales = productos;

        // Fase 1: Descargar imágenes (opcional)
        if (descargarImagenes)
        {
            Console.WriteLine("\nDescargando imágenes de productos...");
            var progress = new Progress<int>(p =>
                Console.Write($"\r   Progreso: {p}/{productos.Count} imágenes procesadas"));

            productosFinales = await _imageDownloadService.DescargarImagenesAsync(
                productos,
                carpetaDestino,
                progress
            );
        }

        // Fase 2: Exportar en diferentes formatos
        Console.WriteLine("\nExportando datos en múltiples formatos...");
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        foreach (var exportService in _exportServices)
        {
            try
            {
                string extension = exportService.Formato.ToLower();
                string rutaArchivo = Path.Combine(
                    carpetaDestino,
                    $"productos_ryse_{timestamp}.{extension}"
                );

                await exportService.ExportarAsync(productosFinales, rutaArchivo);
                Console.WriteLine($"   Exportado en {exportService.Formato}: {Path.GetFileName(rutaArchivo)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Error exportando en {exportService.Formato}: {ex.Message}");
            }
        }
    }

    public async Task ExportarEnTodosFormatosAsync(List<ProductoDto> productos, string carpetaDestino)
    {
        await ExportarConImagenesAsync(productos, carpetaDestino, descargarImagenes: true);
    }

    public async Task ExportarEnFormatoAsync(
        List<ProductoDto> productos,
        string rutaDestino,
        string formato,
        bool descargarImagenes = true)
    {
        var exportService = _exportServices.FirstOrDefault(s =>
            s.Formato.Equals(formato, StringComparison.OrdinalIgnoreCase));

        if (exportService == null)
        {
            throw new NotSupportedException(
                $"Formato {formato} no soportado. " +
                $"Formatos disponibles: {string.Join(", ", _exportServices.Select(s => s.Formato))}");
        }

        // Descargar imágenes si se solicita
        if (descargarImagenes)
        {
            var carpetaBase = Path.GetDirectoryName(rutaDestino) ?? ".";
            var productosConImagenes = await _imageDownloadService.DescargarImagenesAsync(
                productos,
                carpetaBase
            );
            await exportService.ExportarAsync(productosConImagenes, rutaDestino);
        }
        else
        {
            await exportService.ExportarAsync(productos, rutaDestino);
        }

        Console.WriteLine($"Exportado exitosamente en formato {formato}");
    }
}