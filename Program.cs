using CodigoLimpio.Core.Interfaces;
using CodigoLimpio.Core.Servicios;
using CodigoLimpio.Core.Servicios.Exportadores;
using HvacScraper.Console.Infrastructure.Bots;

// 1. Instanciar el cliente HTTP real que el bot usará para navegar por internet
using var httpClient = new HttpClient();

// 2. Configurar las estrategias de infraestructura (Registramos los bots)
var listaBots = new List<IScrapingStrategy>
{
    new RyseScraperBot(httpClient), // Bot real para Ryse México
    new TiendaHvacScraperBot(),  // Bot simulado de respaldo
    new AireyClimaScraperBot(httpClient),  // NUEVO: Bot para Aire y Clima Especializado

};

// 3. Configurar el servicio de descarga de imágenes
var imageDownloadService = new ImageDownloadService(httpClient, maxConcurrentDownloads: 3);

// 4. Configurar los servicios de exportación
var listaExportadores = new List<IProductoExportService>
{
    new CsvExportService(),    // Exporta a CSV para Excel
    new JsonExportService(),   // Exporta a JSON para APIs
    new HtmlCatalogoExportService(),  // HTML interactivo con imágenes incrustadas

};

// 5. Inicializar los orquestadores
var orquestadorScraping = new ScraperOrquestador(listaBots);
var orquestadorExport = new ProductoExportOrchestrator(listaExportadores, imageDownloadService);

// 6. URL Objetivo y carpeta de exportación
string urlObjetivo = "https://aireyclimaespecializado.com.mx/";
string carpetaExportacion = Path.Combine(
    AppDomain.CurrentDomain.BaseDirectory,
    "Exportaciones",
    DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") 
);

Console.Clear();
Console.WriteLine("========================================");
Console.WriteLine("   SISTEMA HVAC SCRAPER BOT v2.0");
Console.WriteLine("   Extracción y Exportación Automatizada");
Console.WriteLine("========================================\n");

try
{
    // ═══════════════════════════════════════
    // FASE 1: EXTRACCIÓN DE PRODUCTOS
    // ═══════════════════════════════════════
    Console.WriteLine("[FASE 1/3] Extrayendo productos de Ryse México...");
    Console.WriteLine($"   URL: {urlObjetivo}\n");

    var productos = await orquestadorScraping.EjecutarAsync(urlObjetivo);

    if (productos == null || !productos.Any())
    {
        Console.WriteLine(" No se encontraron productos. Verifica la URL o la conexión.");
        Console.WriteLine("\nPresiona ENTER para salir...");
        Console.ReadLine();
        return;
    }

    Console.WriteLine($"Extracción exitosa: {productos.Count} productos encontrados\n");

    // ═══════════════════════════════════════
    // FASE 2: MOSTRAR RESUMEN EN CONSOLA
    // ═══════════════════════════════════════
    Console.WriteLine("[FASE 2/3] Mostrando resumen de productos:");
    Console.WriteLine(new string('═', 70));

    for (int i = 0; i < Math.Min(productos.Count, 5); i++) // Mostrar solo primeros 5
    {
        var producto = productos[i];
        Console.WriteLine($"\nProducto #{i + 1}");
        Console.WriteLine($"   Equipo: {producto.Descripcion}");
        Console.WriteLine($"   Precio: {producto.Precio:C} MXN");
        Console.WriteLine($"   Imagen URL: {(string.IsNullOrEmpty(producto.ImagenUrl) ? "No disponible" : producto.ImagenUrl.Substring(0, Math.Min(60, producto.ImagenUrl.Length)) + "...")}");
    }

    if (productos.Count > 5)
    {
        Console.WriteLine($"\n   ... y {productos.Count - 5} productos más.");
    }

    // ═══════════════════════════════════════
    // FASE 3: EXPORTACIÓN CON IMÁGENES
    // ═══════════════════════════════════════
    Console.WriteLine($"\n{new string('═', 70)}");
    Console.WriteLine("[FASE 3/3] Descargando imágenes y exportando datos...");

    await orquestadorExport.ExportarConImagenesAsync(
        productos,
        carpetaExportacion,
        descargarImagenes: true
    );

    // Mostrar resumen final
    Console.WriteLine($"\n{new string('═', 70)}");
    Console.WriteLine("¡PROCESO COMPLETADO EXITOSAMENTE!");
    Console.WriteLine($"\nCarpeta de exportación:");
    Console.WriteLine($"   {carpetaExportacion}");

    Console.WriteLine("\nEstructura generada:");
    Console.WriteLine($"   {Path.GetFileName(carpetaExportacion)}/");
    Console.WriteLine($"   Imagenes/");

    var imagenesDescargadas = Directory.GetFiles(
        Path.Combine(carpetaExportacion, "Imagenes"), "*.*"
    ).Length;
    Console.WriteLine($"   │   └── {imagenesDescargadas} imágenes descargadas");

    var archivosExportados = Directory.GetFiles(carpetaExportacion, "productos_*.*");
    foreach (var archivo in archivosExportados)
    {
        var info = new FileInfo(archivo);
        var tamañoKB = info.Length / 1024.0;
        Console.WriteLine($"   ├── {info.Name} ({tamañoKB:F1} KB)");
    }

    Console.WriteLine($"\nTips:");
    Console.WriteLine($"   • Abre el archivo .html en tu navegador para ver el catálogo visual");
    Console.WriteLine($"   • Importa el .csv en Excel para análisis de datos");
    Console.WriteLine($"   • Las imágenes se guardaron en la subcarpeta 'Imagenes/'");
    Console.WriteLine($"   • El archivo .json contiene todos los datos estructurados");
}
catch (NotSupportedException ex)
{
    Console.WriteLine($"\n ERROR DE CONFIGURACIÓN: {ex.Message}");
    Console.WriteLine("   Asegúrate de que la URL corresponda a un sitio soportado.");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"\n ERROR DE CONEXIÓN: {ex.Message}");
    Console.WriteLine("   Verifica tu conexión a internet o que el sitio esté disponible.");
}
catch (Exception ex)
{
    Console.WriteLine($"\n ERROR INESPERADO: {ex.Message}");
    Console.WriteLine($"   Stack: {ex.StackTrace}");
}

Console.WriteLine($"\n{new string('═', 70)}");
Console.WriteLine("Presiona ENTER para apagar el bot...");
Console.ReadLine();