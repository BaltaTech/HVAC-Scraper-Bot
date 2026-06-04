using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotScrapper.Core.Interfaces
{
    public interface IHtmlFetcher
    {
        Task<string> FetchHtmlAsync(string url);
    }
}
