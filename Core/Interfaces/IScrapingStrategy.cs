using CodigoLimpio.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodigoLimpio.Core.Interfaces
{
    public interface IScrapingStrategy
    {
        bool CanHandle(string url);
        Task<List<ProductoDto>> ExtraerAsync(string url);
    }
}
