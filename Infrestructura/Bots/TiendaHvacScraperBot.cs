using CodigoLimpio.Core.DTOs;
using CodigoLimpio.Core.Interfaces;
using HtmlAgilityPack;
using System.Globalization;

namespace HvacScraper.Console.Infrastructure.Bots;

public class TiendaHvacScraperBot : IScrapingStrategy
{
    public bool CanHandle(string url)
    {
        return url.Contains("tienda-hvac", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<ProductoDto>> ExtraerAsync(string url)
    {
        // SIMULACIÓN: HTML crudo (esto se mantiene igual)
        string htmlCrudo = @"
            <html>
                <body>
                    <div class='product-card'>
                        <img src='https://imagenes.com/minisplit-1ton.jpg' />
                        <h2>Minisplit Inverter Daikin 1 Tonelada</h2>
                        <span class='price'>$ 12,499.00</span>
                    </div>
                    <div class='product-card'>
                        <img src='https://imagenes.com/minisplit-2ton.jpg' />
                        <h2>Minisplit Carrier 2 Toneladas Alto Rendimiento</h2>
                        <span class='price'>$ 18,950.50</span>
                    </div>
                </body>
            </html>";

        return ParsearHtml(htmlCrudo);
    }

  
    public List<ProductoDto> ParsearHtml(string html)
    {
        var documentoDom = new HtmlDocument();
        documentoDom.LoadHtml(html);

        var resultados = new List<ProductoDto>();
        var nodosTarjetas = documentoDom.DocumentNode
            .SelectNodes("//div[contains(@class, 'product-card')]");

        if (nodosTarjetas == null) return resultados;

        foreach (var nodo in nodosTarjetas)
        {
            string imgUrl = nodo.SelectSingleNode(".//img")
                ?.GetAttributeValue("src", string.Empty) ?? string.Empty;
            string descripcion = nodo.SelectSingleNode(".//h2")
                ?.InnerText?.Trim() ?? string.Empty;
            string precioTexto = nodo.SelectSingleNode(".//span[@class='price']")
                ?.InnerText ?? "0";

            decimal precioDecimal = LimpiarPrecio(precioTexto);
            resultados.Add(new ProductoDto(imgUrl, precioDecimal, descripcion));
        }

        return resultados;
    }

    private decimal LimpiarPrecio(string texto)
    {
        string limpio = texto.Replace("$", "").Replace(",", "").Trim();
        return decimal.TryParse(limpio, CultureInfo.InvariantCulture, out decimal valor) ? valor : 0m;
    }
}