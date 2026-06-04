using System.Globalization;
using System.Text.Json;
using HtmlAgilityPack;
using CodigoLimpio.Core.Interfaces;
using CodigoLimpio.Core.DTOs;
using PuppeteerSharp;

namespace HvacScraper.Console.Infrastructure.Bots;

public class RyseScraperBot : IScrapingStrategy
{
    private readonly HttpClient _httpClient;

    public RyseScraperBot(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool CanHandle(string url)
    {
        return url.Contains("rysemexico", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<ProductoDto>> ExtraerAsync(string url)
    {
        var resultados = new List<ProductoDto>();

        try
        {
            System.Console.WriteLine("[Bot Ryse] Iniciando navegador headless para renderizar JavaScript...");

            // Descargar Chromium si no existe (solo primera vez)
            await new BrowserFetcher().DownloadAsync();

            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            });

            using var page = await browser.NewPageAsync();

            // Configurar viewport y user agent
            await page.SetViewportAsync(new ViewPortOptions { Width = 1920, Height = 1080 });
            await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            // Navegar y esperar a que cargue la red completamente
            await page.GoToAsync(url, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle2 },
                Timeout = 30000
            });

            // Esperar específicamente por los productos (selector de Shopify)
            try
            {
                await page.WaitForSelectorAsync(".grid__item, .product-grid .grid__item, #product-grid li",
                    new WaitForSelectorOptions { Timeout = 10000 });
            }
            catch (WaitTaskTimeoutException)
            {
                System.Console.WriteLine("[Bot Ryse] Timeout esperando selectores, continuando con HTML disponible...");
            }

            // Hacer scroll para cargar lazy loading
            await page.EvaluateExpressionAsync("window.scrollTo(0, document.body.scrollHeight)");
            await Task.Delay(2000); // Esperar 2 segundos para carga lazy

            // Obtener el HTML renderizado
            string htmlRenderizado = await page.GetContentAsync();

            // Procesar con HtmlAgilityPack
            var documentoDom = new HtmlDocument();
            documentoDom.LoadHtml(htmlRenderizado);

            // Intentar múltiples selectores comunes de Shopify
            var nodosTarjetas = documentoDom.DocumentNode.SelectNodes("//li[contains(@class, 'grid__item')]")
                             ?? documentoDom.DocumentNode.SelectNodes("//div[contains(@class, 'product-grid')]//li")
                             ?? documentoDom.DocumentNode.SelectNodes("//div[contains(@class, 'card-wrapper')]")
                             ?? documentoDom.DocumentNode.SelectNodes("//ul[contains(@class, 'product-grid')]//li")
                             ?? documentoDom.DocumentNode.SelectNodes("//div[contains(@class, 'product') and contains(@class, 'grid')]//li");

            if (nodosTarjetas != null && nodosTarjetas.Any())
            {
                System.Console.WriteLine($"[Bot Ryse] Encontradas {nodosTarjetas.Count} tarjetas de productos.");

                foreach (var nodo in nodosTarjetas)
                {
                    try
                    {
                        var producto = ExtraerProductoDeNodo(nodo);
                        if (producto != null && !string.IsNullOrEmpty(producto.Descripcion))
                        {
                            // Evitar duplicados
                            if (!resultados.Any(p => p.Descripcion.Equals(producto.Descripcion, StringComparison.OrdinalIgnoreCase)))
                            {
                                resultados.Add(producto);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"[Bot Ryse] Error procesando tarjeta: {ex.Message}");
                        continue;
                    }
                }
            }
            else
            {
                System.Console.WriteLine("[Bot Ryse] No se encontraron tarjetas con selectores principales, usando API fallback...");
                resultados = await ExtraerDesdeShopifyAPI(url);
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Bot Ryse] Error en Puppeteer: {ex.Message}");

            // Método 2: Fallback - Intentar con la API de Shopify directamente
            System.Console.WriteLine("[Bot Ryse] Cambiando a método de API de Shopify...");
            resultados = await ExtraerDesdeShopifyAPI(url);
        }

        System.Console.WriteLine($"[Bot Ryse] Se extrajeron {resultados.Count} productos exitosamente.");
        return resultados;
    }

    private ProductoDto? ExtraerProductoDeNodo(HtmlNode nodo)
    {
        // COORDENADA TÍTULO
        var nodoTitulo = nodo.SelectSingleNode(".//h3[contains(@class, 'card__heading')]//a")
                     ?? nodo.SelectSingleNode(".//h3//a")
                     ?? nodo.SelectSingleNode(".//a[contains(@class, 'full-unstyled-link')]");

        string descripcion = nodoTitulo?.InnerText?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(descripcion) || descripcion.Length < 5) return null;

        // COORDENADA PRECIO
        var nodoPrecio = nodo.SelectSingleNode(".//span[contains(@class, 'price-item--sale')]")
                     ?? nodo.SelectSingleNode(".//span[contains(@class, 'price-item--regular')]")
                     ?? nodo.SelectSingleNode(".//span[contains(@class, 'price-item')]")
                     ?? nodo.SelectSingleNode(".//span[contains(@class, 'money')]");

        string precioTexto = nodoPrecio?.InnerText ?? "0";

        // COORDENADA IMAGEN
        var nodoImg = nodo.SelectSingleNode(".//img[contains(@class, 'card__media')]")
                     ?? nodo.SelectSingleNode(".//img[contains(@src, 'product')]")
                     ?? nodo.SelectSingleNode(".//img");

        string imgUrl = nodoImg?.GetAttributeValue("src", string.Empty) ?? string.Empty;
        if (string.IsNullOrEmpty(imgUrl))
        {
            imgUrl = nodoImg?.GetAttributeValue("data-src", string.Empty) ?? string.Empty;
        }
        if (imgUrl.StartsWith("//")) imgUrl = "https:" + imgUrl;

        decimal precioDecimal = LimpiarPrecio(precioTexto);
        return new ProductoDto(imgUrl, precioDecimal, descripcion);
    }

    private async Task<List<ProductoDto>> ExtraerDesdeShopifyAPI(string urlBase)
    {
        var resultados = new List<ProductoDto>();

        try
        {
            System.Console.WriteLine("[Bot Ryse] Intentando extraer desde API de productos de Shopify...");

            // Shopify expone sus productos en /products.json
            string apiUrl = "https://www.rysemexico.com/products.json?limit=250";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");

            var response = await _httpClient.GetStringAsync(apiUrl);
            System.Console.WriteLine("[Bot Ryse] Respuesta de API obtenida, procesando JSON...");

            // Parsear el JSON usando System.Text.Json
            using JsonDocument document = JsonDocument.Parse(response);
            JsonElement root = document.RootElement;

            if (root.TryGetProperty("products", out JsonElement products))
            {
                foreach (JsonElement product in products.EnumerateArray())
                {
                    try
                    {
                        // Extraer título
                        string titulo = product.GetProperty("title").GetString() ?? string.Empty;

                        if (string.IsNullOrEmpty(titulo)) continue;

                        // Extraer precio del primer variant
                        decimal precio = 0;
                        if (product.TryGetProperty("variants", out JsonElement variants) &&
                            variants.GetArrayLength() > 0)
                        {
                            string? precioStr = variants[0].GetProperty("price").GetString();
                            if (!string.IsNullOrEmpty(precioStr))
                            {
                                decimal.TryParse(precioStr, NumberStyles.Any, CultureInfo.InvariantCulture, out precio);
                            }
                        }

                        // Extraer imagen principal
                        string imagenUrl = string.Empty;
                        if (product.TryGetProperty("images", out JsonElement images) &&
                            images.GetArrayLength() > 0)
                        {
                            imagenUrl = images[0].GetProperty("src").GetString() ?? string.Empty;

                            // Corregir URLs que empiezan con //
                            if (imagenUrl.StartsWith("//"))
                            {
                                imagenUrl = "https:" + imagenUrl;
                            }
                        }

                        // Verificar si el producto pertenece a la colección de aire acondicionado
                        // (opcional, si quieres filtrar por colección)
                        bool esAireAcondicionado = true; // Por defecto true si no podemos verificar

                        // Crear DTO y agregar a resultados
                        if (!string.IsNullOrEmpty(titulo) && precio > 0)
                        {
                            // Evitar duplicados
                            if (!resultados.Any(p => p.Descripcion.Equals(titulo, StringComparison.OrdinalIgnoreCase)))
                            {
                                resultados.Add(new ProductoDto(imagenUrl, precio, titulo));
                                System.Console.WriteLine($"[Bot Ryse] Producto API: {titulo} - ${precio}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"[Bot Ryse] Error procesando producto JSON: {ex.Message}");
                        continue;
                    }
                }

                System.Console.WriteLine($"[Bot Ryse] API: {resultados.Count} productos procesados del JSON.");
            }
            else
            {
                System.Console.WriteLine("[Bot Ryse] No se encontró la propiedad 'products' en el JSON.");
            }
        }
        catch (HttpRequestException ex)
        {
            System.Console.WriteLine($"[Bot Ryse] Error HTTP accediendo a API: {ex.Message}");
        }
        catch (JsonException ex)
        {
            System.Console.WriteLine($"[Bot Ryse] Error parseando JSON: {ex.Message}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Bot Ryse] Error inesperado en API: {ex.Message}");
        }

        return resultados;
    }

    private decimal LimpiarPrecio(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return 0m;

        string limpio = texto.Replace("$", "")
                             .Replace("MXN", "")
                             .Replace(",", "")
                             .Replace("\n", "")
                             .Replace("\r", "")
                             .Replace(" ", "")
                             .Trim();

        return decimal.TryParse(limpio, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal valor) ? valor : 0m;
    }
}