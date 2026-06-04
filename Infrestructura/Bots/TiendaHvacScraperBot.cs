using BotScrapper.Core.Interfaces;
using CodigoLimpio.Core.DTOs;
using CodigoLimpio.Core.Interfaces;
using HtmlAgilityPack;
using System.Globalization;

namespace HvacScraper.Console.Infrastructure.Bots;

public class TiendaHvacScraperBot : IScrapingStrategy
{
    private readonly IHtmlFetcher _fetcher;
    private readonly IProductoParser _parser;

    // Constructor con inyección de dependencias
    public TiendaHvacScraperBot(IHtmlFetcher fetcher, IProductoParser parser)
    {
        _fetcher = fetcher;
        _parser = parser;
    }

    public bool CanHandle(string url)
    {
        return url.Contains("tienda-hvac", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<ProductoDto>> ExtraerAsync(string url)
    {
        // 1. Obtener HTML (desde internet o desde simulación)
        string html = await _fetcher.FetchHtmlAsync(url);

        // 2. Parsear HTML y convertir en productos
        return _parser.ParsearHtml(html);
    }
}