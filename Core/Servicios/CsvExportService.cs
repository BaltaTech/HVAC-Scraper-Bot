using System.Globalization;
using System.Text;
using CodigoLimpio.Core.DTOs;
using CodigoLimpio.Core.Interfaces;

namespace CodigoLimpio.Core.Servicios.Exportadores;

public class CsvExportService : IProductoExportService
{
    public string Formato => "CSV";

    public async Task ExportarAsync(List<ProductoDto> productos, string rutaDestino)
    {
        var csv = new StringBuilder();

        // Cabecera
        csv.AppendLine("Equipo,Precio,Imagen_URL");

        // Datos
        foreach (var producto in productos)
        {
            var precioFormateado = producto.Precio.ToString("F2", CultureInfo.InvariantCulture);
            var descripcionEscapada = EscaparCampo(producto.Descripcion);
            var imagenEscapada = EscaparCampo(producto.ImagenUrl);

            csv.AppendLine($"{descripcionEscapada},{precioFormateado},{imagenEscapada}");
        }

        await File.WriteAllTextAsync(rutaDestino, csv.ToString(), Encoding.UTF8);
    }

    private string EscaparCampo(string campo)
    {
        if (string.IsNullOrEmpty(campo)) return "\"\"";

        if (campo.Contains(",") || campo.Contains("\""))
        {
            return $"\"{campo.Replace("\"", "\"\"")}\"";
        }
        return campo;
    }
}