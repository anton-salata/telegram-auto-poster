using HtmlAgilityPack;
using System.Globalization;
using TelegramAutoPoster.Entities;
using TelegramAutoPoster.Scrapers;

namespace BmwNewsBot.Scraper
{
	public class BmwNewsScraper : BaseScraper
	{
		public override string Id => "BmwNews";

		public BmwNewsScraper(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
		{
		}

		public override async Task<IEnumerable<ScrapedItem>> ScrapeAsync(string url, CancellationToken cancellationToken)
		{
			var html = await _httpClient.GetStringAsync(url);

			var doc = new HtmlDocument();
			doc.LoadHtml(html);

			var articles = doc.DocumentNode.SelectNodes("//article");

			var items = new List<ScrapedItem>();

			if (articles == null)
			{
				return items;
			}

			foreach (var article in articles)
			{
				try
				{
					// Title
					var titleNode = article.SelectSingleNode(".//h3[@class='post-title']/a");
					string title = titleNode?.InnerText.Trim();

					// Article URL
					string link = titleNode?.GetAttributeValue("href", string.Empty);

					// Image
					var imgNode = article.SelectSingleNode(".//div[@class='post-image']//img");
					string imageUrl = imgNode?.GetAttributeValue("src", string.Empty);

					// Date
					var dateNode = article.SelectSingleNode(".//div[@class='post-meta']/span[@class='post-date']");
					string dateText = dateNode?.InnerText.Trim();
					DateTime publishDate = DateTime.ParseExact(dateText, "MMMM d, yyyy", CultureInfo.InvariantCulture);

					if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(link))
					{
						items.Add(new ScrapedItem
						{
							Url = link,
							Message = $"📰 *{HtmlEntity.DeEntitize(title)}*\n\n[Read more]({link})\n🕒 {publishDate:yyyy-MM-dd HH:mm}",
							ImageUrl = imageUrl,
							PublishDate = publishDate
						});
					}
				}
				catch
				{
					// Optionally log or skip problematic article node
				}
			}

			return items;
		}
	}
}
