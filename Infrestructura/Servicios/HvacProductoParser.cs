using BotScrapper.Core.Interfaces;
using CodigoLimpio.Core.DTOs;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotScrapper.Infrestructura.Servicios
{
    public class HvacProductoParser : IProductoParser
    {
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
            if (string.IsNullOrEmpty(texto)) return 0m;

            // Eliminar símbolos de moneda y texto no numérico
            string limpio = texto
                .Replace("$", "")
                .Replace("MXN", "")
                .Replace("MX$", "")
                .Replace("USD", "")
                .Replace(" ", "")
                .Trim();

            // Eliminar comas separadoras de miles (formato mexicano)
            limpio = limpio.Replace(",", "");

            // Intentar parsear
            if (decimal.TryParse(limpio, NumberStyles.Any,
                CultureInfo.InvariantCulture, out decimal valor))
            {
                return valor;
            }

            return 0m;
        }
    }
}