using CodigoLimpio.Core.DTOs;

namespace CodigoLimpio.Core.Interfaces;

public interface IProductoExportService
{
    Task ExportarAsync(List<ProductoDto> productos, string rutaDestino);
    string Formato { get; }
}