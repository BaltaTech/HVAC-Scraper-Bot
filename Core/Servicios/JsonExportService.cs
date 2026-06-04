using System.Text.Json;
using CodigoLimpio.Core.DTOs;
using CodigoLimpio.Core.Interfaces;

namespace CodigoLimpio.Core.Servicios.Exportadores;

public class JsonExportService : IProductoExportService
{
    public string Formato => "JSON";

    public async Task ExportarAsync(List<ProductoDto> productos, string rutaDestino)
    {
        var opciones = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(productos, opciones);
        await File.WriteAllTextAsync(rutaDestino, json, System.Text.Encoding.UTF8);
    }
}