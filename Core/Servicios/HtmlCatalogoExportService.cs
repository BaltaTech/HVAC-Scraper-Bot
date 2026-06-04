using System.Text;
using CodigoLimpio.Core.DTOs;
using CodigoLimpio.Core.Interfaces;

namespace CodigoLimpio.Core.Servicios.Exportadores;

public class HtmlCatalogoExportService : IProductoExportService
{
    public string Formato => "HTML";

    public async Task ExportarAsync(List<ProductoDto> productos, string rutaDestino)
    {
        var html = new StringBuilder();

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang='es'>");
        AgregarHead(html);
        html.AppendLine("<body>");

        AgregarBarraNavegacion(html);
        AgregarHeroBanner(html, productos.Count);
        AgregarEstadisticas(html, productos);
        AgregarGridProductos(html, productos);
        AgregarVentanaModal(html);
        AgregarScripts(html);

        html.AppendLine("</body>");
        html.AppendLine("</html>");

        await File.WriteAllTextAsync(rutaDestino, html.ToString(), Encoding.UTF8);
    }

    private void AgregarHead(StringBuilder html)
    {
        html.AppendLine(@"<head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>🏪 Catálogo Ryse México - Aire Acondicionado</title>
            <link href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css' rel='stylesheet'>
            <style>
                * { margin: 0; padding: 0; box-sizing: border-box; }
                body { font-family: 'Segoe UI', system-ui, sans-serif; background: #f0f2f5; }
                
                /* Navbar */
                .navbar { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 1rem 2rem; position: sticky; top: 0; z-index: 100; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
                .navbar-content { max-width: 1400px; margin: 0 auto; display: flex; justify-content: space-between; align-items: center; }
                .logo { font-size: 1.5rem; font-weight: bold; }
                .nav-stats { display: flex; gap: 2rem; }
                .nav-stat { text-align: center; }
                .nav-stat-number { font-size: 1.5rem; font-weight: bold; }
                .nav-stat-label { font-size: 0.8rem; opacity: 0.9; }
                
                /* Hero */
                .hero { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 3rem 2rem; margin-bottom: 2rem; }
                .hero-content { max-width: 1400px; margin: 0 auto; }
                .hero h1 { font-size: 2.5rem; margin-bottom: 0.5rem; }
                .hero p { font-size: 1.2rem; opacity: 0.9; }
                
                /* Filtros */
                .filters { max-width: 1400px; margin: -1rem auto 2rem; padding: 1rem 2rem; background: white; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); display: flex; gap: 1rem; align-items: center; flex-wrap: wrap; }
                .search-box { flex: 1; min-width: 250px; padding: 0.8rem; border: 2px solid #e0e0e0; border-radius: 8px; font-size: 1rem; }
                .price-filter { display: flex; gap: 0.5rem; align-items: center; }
                .price-filter input { width: 120px; padding: 0.8rem; border: 2px solid #e0e0e0; border-radius: 8px; }
                .btn { padding: 0.8rem 1.5rem; border: none; border-radius: 8px; cursor: pointer; font-weight: bold; transition: all 0.3s; }
                .btn-primary { background: #667eea; color: white; }
                .btn-primary:hover { background: #764ba2; transform: translateY(-2px); }
                
                /* Grid */
                .product-grid { max-width: 1400px; margin: 0 auto; padding: 0 2rem; display: grid; grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); gap: 1.5rem; }
                .product-card { background: white; border-radius: 15px; overflow: hidden; box-shadow: 0 3px 15px rgba(0,0,0,0.1); transition: all 0.3s; cursor: pointer; }
                .product-card:hover { transform: translateY(-5px); box-shadow: 0 5px 30px rgba(0,0,0,0.2); }
                .product-image-container { position: relative; height: 250px; background: #f5f5f5; overflow: hidden; }
                .product-image { width: 100%; height: 100%; object-fit: cover; transition: transform 0.3s; }
                .product-card:hover .product-image { transform: scale(1.05); }
                .product-badge { position: absolute; top: 10px; right: 10px; background: #667eea; color: white; padding: 5px 15px; border-radius: 20px; font-size: 0.9rem; }
                .product-info { padding: 1.5rem; }
                .product-name { font-size: 1.1rem; font-weight: 600; color: #333; margin-bottom: 0.5rem; line-height: 1.4; }
                .product-price-container { display: flex; justify-content: space-between; align-items: center; margin-top: 1rem; }
                .product-price { font-size: 1.8rem; font-weight: bold; color: #667eea; }
                .product-price-currency { font-size: 1rem; color: #666; }
                .btn-view { background: #667eea; color: white; padding: 0.5rem 1rem; border-radius: 5px; text-decoration: none; font-size: 0.9rem; }
                
                /* Modal */
                .modal { display: none; position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.8); z-index: 1000; justify-content: center; align-items: center; }
                .modal.active { display: flex; }
                .modal-content { background: white; max-width: 900px; width: 90%; border-radius: 20px; overflow: hidden; display: grid; grid-template-columns: 1fr 1fr; max-height: 90vh; }
                .modal-image { width: 100%; height: 100%; object-fit: cover; min-height: 400px; }
                .modal-info { padding: 2rem; overflow-y: auto; }
                .modal-close { position: absolute; top: 20px; right: 20px; color: white; font-size: 2rem; cursor: pointer; }
                
                /* Responsive */
                @media (max-width: 768px) {
                    .modal-content { grid-template-columns: 1fr; }
                    .filters { flex-direction: column; }
                    .hero h1 { font-size: 1.8rem; }
                }
                
                .no-image { display: flex; align-items: center; justify-content: center; background: #f0f0f0; color: #999; font-size: 3rem; }
                .footer { text-align: center; padding: 2rem; color: #666; margin-top: 3rem; background: white; }
            </style>
        </head>");
    }

    private void AgregarBarraNavegacion(StringBuilder html)
    {
        html.AppendLine(@"<nav class='navbar'>
            <div class='navbar-content'>
                <div class='logo'> Ryse México</div>
                <div class='nav-stats'>
                    <div class='nav-stat'>
                        <div class='nav-stat-number' id='totalProducts'>0</div>
                        <div class='nav-stat-label'>Productos</div>
                    </div>
                    <div class='nav-stat'>
                        <div class='nav-stat-number' id='avgPrice'>$0</div>
                        <div class='nav-stat-label'>Precio Promedio</div>
                    </div>
                </div>
            </div>
        </nav>");
    }

    private void AgregarHeroBanner(StringBuilder html, int total)
    {
        html.AppendLine($@"<div class='hero'>
            <div class='hero-content'>
                <h1>Catálogo de Aire Acondicionado</h1>
                <p>Encuentra el equipo perfecto para climatizar tu espacio. {total} productos disponibles.</p>
            </div>
        </div>");
    }

    private void AgregarEstadisticas(StringBuilder html, List<ProductoDto> productos)
    {
        var precioPromedio = productos.Any() ? productos.Average(p => p.Precio) : 0;
        var precioMin = productos.Any() ? productos.Min(p => p.Precio) : 0;
        var precioMax = productos.Any() ? productos.Max(p => p.Precio) : 0;

        html.AppendLine($@"<div class='filters'>
            <input type='text' class='search-box' placeholder=' Buscar equipo...' id='searchInput' onkeyup='filtrarProductos()'>
            <div class='price-filter'>
                <span> </span>
                <input type='number' placeholder='Precio min' id='priceMin' value='{precioMin:F0}' onchange='filtrarProductos()'>
                <span>-</span>
                <input type='number' placeholder='Precio max' id='priceMax' value='{precioMax:F0}' onchange='filtrarProductos()'>
            </div>
            <button class='btn btn-primary' onclick='resetFiltros()'> Limpiar Filtros</button>
        </div>");
    }

    private void AgregarGridProductos(StringBuilder html, List<ProductoDto> productos)
    {
        html.AppendLine("<div class='product-grid' id='productGrid'>");

        foreach (var producto in productos)
        {
            var imagenHtml = ObtenerImagenHtml(producto);

            html.AppendLine($@"
            <div class='product-card' data-name='{producto.Descripcion.ToLower()}' data-price='{producto.Precio}' onclick='mostrarDetalle(this)'>
                <div class='product-image-container'>
                    {imagenHtml}
                    <div class='product-badge'>Nuevo</div>
                </div>
                <div class='product-info'>
                    <div class='product-name'>{producto.Descripcion}</div>
                    <div class='product-price-container'>
                        <div>
                            <div class='product-price'>${producto.Precio:N0}</div>
                            <div class='product-price-currency'>MXN</div>
                        </div>
                        <button class='btn-view'>Ver detalles</button>
                    </div>
                </div>
            </div>");
        }

        html.AppendLine("</div>");
    }

    private string ObtenerImagenHtml(ProductoDto producto)
    {
        if (!string.IsNullOrEmpty(producto.ImagenUrl))
        {
            if (File.Exists(producto.ImagenUrl))
            {
                // Convertir imagen a Base64 para incrustarla
                var bytes = File.ReadAllBytes(producto.ImagenUrl);
                var base64 = Convert.ToBase64String(bytes);
                var extension = Path.GetExtension(producto.ImagenUrl).ToLower().TrimStart('.');
                return $"<img src='data:image/{extension};base64,{base64}' class='product-image' alt='{producto.Descripcion}'>";
            }
            else if (producto.ImagenUrl.StartsWith("http"))
            {
                return $"<img src='{producto.ImagenUrl}' class='product-image' alt='{producto.Descripcion}' onerror=\"this.parentElement.innerHTML='<div class=\\'no-image\\'></div>'\">";
            }
        }

        return "<div class='no-image'></div>";
    }

    private void AgregarVentanaModal(StringBuilder html)
    {
        html.AppendLine(@"
        <div class='modal' id='productModal'>
            <span class='modal-close' onclick='cerrarModal()'>&times;</span>
            <div class='modal-content'>
                <img id='modalImage' class='modal-image' src='' alt=''>
                <div class='modal-info'>
                    <h2 id='modalName'></h2>
                    <div style='margin: 1rem 0;'>
                        <span style='font-size: 2rem; color: #667eea; font-weight: bold;' id='modalPrice'></span>
                        <span style='color: #666;'>MXN</span>
                    </div>
                    <hr style='margin: 1rem 0;'>
                    <h3>Características:</h3>
                    <ul style='list-style: none; padding: 0;'>
                        <li> Alta eficiencia energética</li>
                        <li> Tecnología Inverter</li>
                        <li> Bajo nivel de ruido</li>
                        <li> Garantía incluida</li>
                    </ul>
                </div>
            </div>
        </div>");
    }

    private void AgregarScripts(StringBuilder html)
    {
        html.AppendLine(@"<script>
            function filtrarProductos() {
                const searchTerm = document.getElementById('searchInput').value.toLowerCase();
                const priceMin = parseFloat(document.getElementById('priceMin').value) || 0;
                const priceMax = parseFloat(document.getElementById('priceMax').value) || Infinity;
                const cards = document.querySelectorAll('.product-card');
                
                cards.forEach(card => {
                    const name = card.dataset.name;
                    const price = parseFloat(card.dataset.price);
                    const matchesSearch = name.includes(searchTerm);
                    const matchesPrice = price >= priceMin && price <= priceMax;
                    
                    card.style.display = (matchesSearch && matchesPrice) ? '' : 'none';
                });
                
                actualizarEstadisticas();
            }
            
            function resetFiltros() {
                document.getElementById('searchInput').value = '';
                document.getElementById('priceMin').value = '';
                document.getElementById('priceMax').value = '';
                filtrarProductos();
            }
            
            function mostrarDetalle(card) {
                const modal = document.getElementById('productModal');
                const img = card.querySelector('.product-image').src;
                const name = card.querySelector('.product-name').textContent;
                const price = card.querySelector('.product-price').textContent;
                
                document.getElementById('modalImage').src = img;
                document.getElementById('modalName').textContent = name;
                document.getElementById('modalPrice').textContent = price;
                
                modal.classList.add('active');
            }
            
            function cerrarModal() {
                document.getElementById('productModal').classList.remove('active');
            }
            
            function actualizarEstadisticas() {
                const cards = Array.from(document.querySelectorAll('.product-card'))
                    .filter(c => c.style.display !== 'none');
                
                document.getElementById('totalProducts').textContent = cards.length;
                
                const avgPrice = cards.reduce((sum, c) => sum + parseFloat(c.dataset.price), 0) / cards.length;
                document.getElementById('avgPrice').textContent = '$' + Math.round(avgPrice).toLocaleString();
            }
            
            // Cerrar modal con ESC
            document.addEventListener('keydown', (e) => {
                if (e.key === 'Escape') cerrarModal();
            });
            
            // Inicializar estadísticas
            actualizarEstadisticas();
        </script>");
    }
}