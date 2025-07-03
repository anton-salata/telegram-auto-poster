using TelegramAutoPoster.Entities;
using TelegramAutoPoster.Scrapers.Interfaces;

namespace TelegramAutoPoster.Scrapers
{
	public abstract class BaseScraper : IScraper
	{
		protected readonly HttpClient _httpClient;

		public BaseScraper(IHttpClientFactory httpClientFactory)
		{
			_httpClient = httpClientFactory.CreateClient("WithProxy"); //.CreateClient(); //.CreateClient("WithProxy");
		}

		public abstract string Id { get; }

		public abstract Task<IEnumerable<ScrapedItem>> ScrapeAsync(string url, CancellationToken cancellationToken);
	}
}
