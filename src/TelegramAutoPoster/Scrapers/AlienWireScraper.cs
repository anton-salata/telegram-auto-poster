using HtmlAgilityPack;
using System.Net;
using TelegramAutoPoster.Entities;
using TelegramAutoPoster.Scrapers;

namespace AlienWireBot.Scraper
{
	public class AlienWireScraper : BaseScraper
	{
		public override string Id => "AlienWire";
		public AlienWireScraper(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
		{
		}

		public override async Task<IEnumerable<ScrapedItem>> ScrapeAsync(string url, CancellationToken cancellationToken)
		{
			var html = await _httpClient.GetStringAsync(url);

			var doc = new HtmlDocument();
			doc.LoadHtml(html);

			var items = new List<ScrapedItem>();

			foreach (var article in doc.DocumentNode.SelectNodes("//div[contains(@class,'coast-feed-item')]"))
			{
				try
				{
					var imageNode = article.SelectSingleNode(".//img");
					var imageUrl = imageNode?.GetAttributeValue("data-src", null);

					var linkNode = article.SelectSingleNode(".//a[contains(@class,'item-title')]");
					var title = linkNode?.InnerText.Trim();
					var link = linkNode?.GetAttributeValue("href", null);

					var timeNode = article.SelectSingleNode(".//time");
					var dateTimeAttr = timeNode?.GetAttributeValue("dateTime", null);
					DateTime.TryParse(timeNode?.InnerText.Trim(), out var publishDate);

					var summaryNode = article.SelectSingleNode(".//section[contains(@class,'item-summary')]/span[1]");
					var summary = summaryNode?.InnerText.Trim();

					if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(link))
					{
						items.Add(new ScrapedItem
						{
							Url = link,
							ImageUrl = imageUrl,
							FormattedMessage = $"*{WebUtility.HtmlDecode(title)}*\n\n{WebUtility.HtmlDecode(summary)}\n\n[Read more]({link})",
							PublishDate = publishDate
						});
					}
				}
				catch
				{
					// Optional: log or skip silently
				}
			}

			return items;
		}
	}
}
