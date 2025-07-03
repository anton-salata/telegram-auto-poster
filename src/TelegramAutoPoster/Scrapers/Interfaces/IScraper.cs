using TelegramAutoPoster.Entities;

namespace TelegramAutoPoster.Scrapers.Interfaces
{
	public interface IScraper
	{
		string Id { get; }
		Task<IEnumerable<ScrapedItem>> ScrapeAsync(string url, CancellationToken cancellationToken);
	}
}
