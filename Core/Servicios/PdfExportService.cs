using CodigoLimpio.Core.DTOs;
using CodigoLimpio.Core.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CodigoLimpio.Core.Servicios.Exportadores;

public class PdfExportService : IProductoExportService
{
    public string Formato => "PDF";

    public async Task ExportarAsync(List<ProductoDto> productos, string rutaDestino)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                // Encabezado
                page.Header().Element(CrearEncabezado);

                // Contenido en grid
                page.Content().Element(c => CrearGridProductos(c, productos));

                // Pie de página
                page.Footer().Element(CrearPiePagina);
            });
        });

        await Task.Run(() => documento.GeneratePdf(rutaDestino));
    }

    private void CrearEncabezado(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("RYSE MÉXICO")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Blue.Darken3);

                    col.Item().Text("Catálogo de Aire Acondicionado")
                        .FontSize(14)
                        .FontColor(Colors.Grey.Darken2);
                });

                row.ConstantItem(150).Column(col =>
                {
                    col.Item().AlignRight().Text($"Fecha: {DateTime.Now:dd/MM/yyyy}")
                        .FontSize(9);
                    col.Item().AlignRight().Text("Total: productos")
                        .FontSize(9)
                        .Bold();
                });
            });

            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void CrearGridProductos(IContainer container, List<ProductoDto> productos)
    {
        container.Table(table =>
        {
            // Definir columnas
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3); // Imagen
                columns.RelativeColumn(4); // Descripción
                columns.RelativeColumn(2); // Precio
            });

            // Cabecera de tabla
            table.Header(header =>
            {
                header.Cell().Background(Colors.Blue.Darken3).Padding(5)
                    .Text("Producto").FontColor(Colors.White).Bold();
                header.Cell().Background(Colors.Blue.Darken3).Padding(5)
                    .Text("Descripción").FontColor(Colors.White).Bold();
                header.Cell().Background(Colors.Blue.Darken3).Padding(5)
                    .Text("Precio").FontColor(Colors.White).Bold();
            });

            // Filas de productos
            foreach (var producto in productos)
            {
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(8)
                    .Element(cell =>
                    {
                        if (!string.IsNullOrEmpty(producto.ImagenUrl) && File.Exists(producto.ImagenUrl))
                        {
                            cell.Image(producto.ImagenUrl).FitArea();
                        }
                        else
                        {
                            cell.Background(Colors.Grey.Lighten4)
                                .Padding(20)
                                .AlignCenter()
                                .Text("Sin imagen")
                                .FontColor(Colors.Grey.Medium);
                        }
                    });

                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(8)
                    .Text(producto.Descripcion)
                    .FontSize(11)
                    .LineHeight(1.5f); // CORRECCIÓN 3: 1.5f en lugar de (float?)1.5

                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(8)
                    .AlignRight()
                    .Text($"${producto.Precio:N2} MXN")
                    .FontSize(12)
                    .Bold()
                    .FontColor(Colors.Green.Darken2);
            }
        });
    }

    private void CrearPiePagina(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            column.Item().Row(row =>
            {
                row.RelativeItem().Text("Generado por HVAC Scraper Bot")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Medium);

                row.ConstantItem(100).AlignRight().Text("Página 1")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Medium);
            });
        });
    }
}