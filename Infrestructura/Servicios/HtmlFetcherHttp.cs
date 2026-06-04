using BotScrapper.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotScrapper.Infrestructura.Servicios
{
    public class HtmlFetcherHttp : IHtmlFetcher
    {
        private readonly HttpClient _httpClient;

        public HtmlFetcherHttp(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> FetchHtmlAsync(string url)
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
