using HtmlAgilityPack;
using System.Text.RegularExpressions;
using TelegramAutoPoster.Entities;
using TelegramAutoPoster.Scrapers;

namespace CarNewsBot.Scraper
{
	public class CarNewsScraper : BaseScraper
	{
		public override string Id => "CarNews";
		public CarNewsScraper(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
		{
		}

		public override async Task<IEnumerable<ScrapedItem>> ScrapeAsync(string url, CancellationToken cancellationToken)
		{
			var html = await _httpClient.GetStringAsync(url);

			var doc = new HtmlDocument();
			doc.LoadHtml(html);

			var items = new List<ScrapedItem>();

			var newsNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'category-archive-post')]");
			foreach (var node in newsNodes)
			{
				// Image
				var imgNode = node.SelectSingleNode(".//img[contains(@class, 'archive-post-thumb')]");
				var image = imgNode?.GetAttributeValue("src", "");

				// Title and Article URL
				var titleLinkNode = node.SelectSingleNode(".//a[contains(@class, 'card-post-title-link')]");
				var link = titleLinkNode?.GetAttributeValue("href", "");

				var titleSpanNode = titleLinkNode?.SelectSingleNode(".//span[contains(@class, 'desktop')]");
				if (titleSpanNode == null)
				{
					// Fallback to mobile version if desktop not found
					titleSpanNode = titleLinkNode?.SelectSingleNode(".//span[contains(@class, 'mobile')]");
				}
				var title = HtmlEntity.DeEntitize(titleSpanNode?.InnerText.Trim());

				// Author and Author Link
				var authorNode = node.SelectSingleNode(".//div[contains(@class, 'item-wrapper--author')]//a");
				var author = HtmlEntity.DeEntitize(authorNode?.InnerText.Trim());
				var authorLink = authorNode?.GetAttributeValue("href", "");

				// Date Parsing from "Posted X Hours Ago"
				var dateNode = node.SelectSingleNode(".//p[contains(@class, 'card-post-date')]");
				var dateText = dateNode?.InnerText.Trim();

				var parsedDate = DateTime.MinValue;

				if (!string.IsNullOrEmpty(dateText))
				{
					// Case 1: "Posted 11 Hours Ago"
					var matchHours = Regex.Match(dateText, @"Posted\s+(\d+)\s+Hours Ago", RegexOptions.IgnoreCase);
					if (matchHours.Success && int.TryParse(matchHours.Groups[1].Value, out int hoursAgo))
					{
						parsedDate = DateTime.UtcNow.AddHours(-hoursAgo);
					}
					else
					{
						// Case 2: "Posted on May 13, 2025"
						var matchDate = Regex.Match(dateText, @"Posted on (.+)", RegexOptions.IgnoreCase);
						if (matchDate.Success)
						{
							var datePart = matchDate.Groups[1].Value.Trim();
							if (DateTime.TryParse(datePart, out DateTime exactDate))
							{
								parsedDate = exactDate.ToUniversalTime();
							}
						}
						else
						{
							// Case 3: "Posted Yesterday"
							if (dateText.Equals("Posted Yesterday", StringComparison.OrdinalIgnoreCase))
							{
								parsedDate = DateTime.UtcNow.Date.AddDays(-1); // Midnight yesterday
							}
						}
					}
				}

				items.Add(new ScrapedItem
				{
					Url = link,
					Message = $"*{title}*\n\n[Read more]({link})\n\nBy [{author}]({authorLink})",
					ImageUrl = image,
					PublishDate = parsedDate
				});

				//// Output for test
				//Console.WriteLine($"Title: {news.Title}");
				//Console.WriteLine($"Image: {news.Image}");
				//Console.WriteLine($"Article: {news.Url}");
				//Console.WriteLine($"Author: {news.Author}");
				//Console.WriteLine($"Author Link: {news.AuthorLink}");
				//Console.WriteLine($"Posted Date (UTC): {news.PublishDate}");
				//Console.WriteLine("------------------------------------------------------");
			}

			return items;
		}
	}
}
