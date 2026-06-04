using System.Globalization;
using HtmlAgilityPack;
using CodigoLimpio.Core.Interfaces;
using CodigoLimpio.Core.DTOs;

namespace HvacScraper.Console.Infrastructure.Bots;

public class AireyClimaScraperBot : IScrapingStrategy
{
    private readonly HttpClient _httpClient;

    public AireyClimaScraperBot(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool CanHandle(string url)
    {
        return url.Contains("aireyclimaespecializado", StringComparison.OrdinalIgnoreCase) ||
               url.Contains("aireyclima", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<ProductoDto>> ExtraerAsync(string url)
    {
        var resultados = new List<ProductoDto>();

        // 1. Configurar headers realistas
        ConfigurarHeaders();

        // 2. Lista de URLs comunes donde podría haber catálogo
        var urlsExplorar = new[]
        {
            url,
            "https://aireyclimaespecializado.com.mx/tienda",
            "https://aireyclimaespecializado.com.mx/productos",
            "https://aireyclimaespecializado.com.mx/catalogo",
            "https://aireyclimaespecializado.com.mx/equipos",
            "https://aireyclimaespecializado.com.mx/minisplits",
            "https://aireyclimaespecializado.com.mx/shop",
            "https://aireyclimaespecializado.com.mx/store",
        };

        foreach (var urlExplorar in urlsExplorar)
        {
            try
            {
                System.Console.WriteLine($"[Bot AireyClima] Explorando: {urlExplorar}");
                var productosPagina = await ExtraerDePagina(urlExplorar);

                if (productosPagina.Any())
                {
                    System.Console.WriteLine($"[Bot AireyClima] ¡ÉXITO! {productosPagina.Count} productos en {urlExplorar}");
                    resultados.AddRange(productosPagina);
                    break; 
                }
            }
            catch (HttpRequestException)
            {
                System.Console.WriteLine($"[Bot AireyClima] URL no accesible: {urlExplorar}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[Bot AireyClima] Error: {ex.Message}");
            }
        }

        // 3. Si no hay productos, intentar con el sitemap
        if (!resultados.Any())
        {
            System.Console.WriteLine("[Bot AireyClima] Intentando descubrir páginas vía sitemap...");
            resultados = await ExplorarSitemap();
        }

        // 4. Último recurso: buscar en Google Cache
        if (!resultados.Any())
        {
            System.Console.WriteLine("[Bot AireyClima] Sitio SIN productos públicos escrapeables");
            System.Console.WriteLine("[Bot AireyClima] Es un sitio B2B, los precios son por cotización");
        }

        return resultados;
    }

    private void ConfigurarHeaders()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "es-MX,es;q=0.9");
    }

    private async Task<List<ProductoDto>> ExtraerDePagina(string url)
    {
        var resultados = new List<ProductoDto>();

        string html = await _httpClient.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Buscar contenedores de productos con múltiples selectores
        var selectores = new[]
        {
            "//div[contains(@class, 'product')]",
            "//li[contains(@class, 'product')]",
            "//article[contains(@class, 'product')]",
            "//div[contains(@class, 'card-product')]",
            "//div[contains(@class, 'item-product')]",
            "//figure[contains(@class, 'product')]",
            "//div[contains(@data-id, 'product')]",
            "//a[contains(@href, 'product')]/..",
        };

        foreach (var selector in selectores)
        {
            var nodos = doc.DocumentNode.SelectNodes(selector);
            if (nodos == null || !nodos.Any()) continue;

            foreach (var nodo in nodos)
            {
                var producto = ExtraerProductoDeNodo(nodo);
                if (producto != null && !string.IsNullOrEmpty(producto.Descripcion) && producto.Descripcion.Length > 5)
                {
                    resultados.Add(producto);
                }
            }

            if (resultados.Any()) break;
        }

        return resultados;
    }

    private ProductoDto? ExtraerProductoDeNodo(HtmlNode nodo)
    {
        // Título
        var titulo = nodo.SelectSingleNode(".//h2")?.InnerText?.Trim() ??
                    nodo.SelectSingleNode(".//h3")?.InnerText?.Trim() ??
                    nodo.SelectSingleNode(".//h4")?.InnerText?.Trim() ??
                    nodo.SelectSingleNode(".//a[contains(@class, 'title')]")?.InnerText?.Trim() ??
                    nodo.SelectSingleNode(".//a[contains(@class, 'name')]")?.InnerText?.Trim() ?? "";

        if (string.IsNullOrEmpty(titulo) || titulo.Length < 5) return null;

        // Precio
        var precioNodo = nodo.SelectSingleNode(".//span[contains(@class, 'price')]") ??
                        nodo.SelectSingleNode(".//span[contains(@class, 'amount')]") ??
                        nodo.SelectSingleNode(".//ins//span") ??
                        nodo.SelectSingleNode(".//bdi") ??
                        nodo.SelectSingleNode(".//span[contains(@class, 'woocommerce-Price-amount')]");

        string precioTexto = precioNodo?.InnerText ?? "0";
        decimal precio = LimpiarPrecio(precioTexto);

        // Imagen
        var imgNodo = nodo.SelectSingleNode(".//img");
        string imgUrl = imgNodo?.GetAttributeValue("src", "") ?? "";
        if (string.IsNullOrEmpty(imgUrl))
            imgUrl = imgNodo?.GetAttributeValue("data-src", "") ?? "";
        if (imgUrl.StartsWith("//")) imgUrl = "https:" + imgUrl;

        return new ProductoDto(imgUrl, precio, titulo);
    }

    private async Task<List<ProductoDto>> ExplorarSitemap()
    {
        var resultados = new List<ProductoDto>();

        try
        {
            string sitemapUrl = "https://aireyclimaespecializado.com.mx/sitemap.xml";
            string xml = await _httpClient.GetStringAsync(sitemapUrl);

            var doc = new HtmlDocument();
            doc.LoadHtml(xml);

            // Extraer URLs del sitemap
            var urls = doc.DocumentNode.SelectNodes("//loc");
            if (urls != null)
            {
                foreach (var urlNode in urls)
                {
                    var urlEncontrada = urlNode.InnerText.Trim();
                    // Solo explorar URLs que parezcan de productos
                    if (urlEncontrada.Contains("product") ||
                        urlEncontrada.Contains("equipo") ||
                        urlEncontrada.Contains("minisplit") ||
                        urlEncontrada.Contains("trane"))
                    {
                        try
                        {
                            var productos = await ExtraerDePagina(urlEncontrada);
                            resultados.AddRange(productos);
                        }
                        catch { continue; }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Bot AireyClima] Sitemap no disponible: {ex.Message}");
        }          

        return resultados;
    }

    private decimal LimpiarPrecio(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return 0m;

        string limpio = texto.Replace("$", "")
                             .Replace("MXN", "")
                             .Replace("MX$", "")
                             .Replace(",", "")
                             .Replace("\n", "")
                             .Replace("\r", "")
                             .Replace(" ", "")
                             .Trim();

        return decimal.TryParse(limpio, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal valor) ? valor : 0m;
    }
}