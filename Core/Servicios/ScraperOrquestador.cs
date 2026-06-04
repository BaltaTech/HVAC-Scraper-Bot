using CodigoLimpio.Core.DTOs;
using CodigoLimpio.Core.Interfaces;
using System;


namespace CodigoLimpio.Core.Servicios
{
    public class ScraperOrquestador
    {
        private readonly IEnumerable<IScrapingStrategy> _estrategias;

        //Recibe la lista de bots por ID

        public ScraperOrquestador(IEnumerable<IScrapingStrategy> estrategias)
        {
            _estrategias = estrategias;
        }

        public async Task<List<ProductoDto>> EjecutarAsync(string url)
        {
            var botAdecuado = _estrategias.FirstOrDefault(b => b.CanHandle(url));

            if (botAdecuado ==null)
            {
                throw new NotSupportedException($"Arquitectura: No existe un bot entrenado para el DOM de: {url}");
            }

            return await botAdecuado.ExtraerAsync(url);
        
        }
    }
}
