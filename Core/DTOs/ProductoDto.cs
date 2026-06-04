using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodigoLimpio.Core.DTOs
{
    public record ProductoDto(string ImagenUrl, decimal Precio, string Descripcion);
}
