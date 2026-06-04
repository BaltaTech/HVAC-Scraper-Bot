using CodigoLimpio.Core.DTOs;
using CodigoLimpio.Core.Interfaces;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace CodigoLimpio.Core.Servicios.Exportadores;

public class ExcelExportService : IProductoExportService
{
    public string Formato => "Excel";

    public async Task ExportarAsync(List<ProductoDto> productos, string rutaDestino)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Productos Ryse");

        // Configurar estilos del encabezado
        using (var headerRange = worksheet.Cells["A1:C1"])
        {
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(30, 144, 255));
            headerRange.Style.Font.Color.SetColor(System.Drawing.Color.White);
        }

        // Encabezados
        worksheet.Cells["A1"].Value = "Equipo";
        worksheet.Cells["B1"].Value = "Precio (MXN)";
        worksheet.Cells["C1"].Value = "URL Imagen";

        // Datos
        for (int i = 0; i < productos.Count; i++)
        {
            int row = i + 2;
            var producto = productos[i];

            worksheet.Cells[$"A{row}"].Value = producto.Descripcion;
            worksheet.Cells[$"B{row}"].Value = producto.Precio;
            worksheet.Cells[$"B{row}"].Style.Numberformat.Format = "$#,##0.00";
            worksheet.Cells[$"C{row}"].Value = producto.ImagenUrl;
        }

        // Auto-ajustar columnas
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        // Guardar
        await File.WriteAllBytesAsync(rutaDestino, await package.GetAsByteArrayAsync());
    }
}