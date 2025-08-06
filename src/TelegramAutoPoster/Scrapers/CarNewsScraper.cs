using HtmlAgilityPack;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using TelegramAutoPoster.Entities;
using TelegramAutoPoster.Scrapers;
using TelegramAutoPoster.Scrapers.Entities;

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

				var articleData = await GetArticleData(link);

				items.Add(new ScrapedItem
				{
					Url = link,
					FormattedMessage = $"*{title}*\n\n[Read more]({link})\n\nBy [{author}]({authorLink})",
					PlainText = articleData.ArticleText,
					ImageUrl = image,
					PublishDate = parsedDate,
					AuthorLink = authorLink,
					AuthorName = author,
					Title = title,
					Format = PostFormat.MultiViaComments,
					Images = articleData.Images,
					Videos = articleData.Videos
				});

				// Output for test
				Console.WriteLine($"Title: {title}");
				Console.WriteLine($"Image: {image}");
				Console.WriteLine($"Link: {link}");
				Console.WriteLine($"Author: {author}");
				Console.WriteLine($"Author Link: {authorLink}");
				Console.WriteLine($"Posted Date: {parsedDate}");
				Console.WriteLine($"Plain Text: {articleData.ArticleText}");
				Console.WriteLine($"Images: {string.Join("\n", articleData.Images)}");
				Console.WriteLine($"Videos: {string.Join("\n", articleData.Videos)}");
			}

			return items;
		}

		private async Task<ArticleData> GetArticleData(string link)
		{
			var html = await _httpClient.GetStringAsync(link);

			var doc = new HtmlDocument();
			doc.LoadHtml(html);

			var articleData = new ArticleData();

			// Select all article paragraphs (excluding those with 'skip' class if needed)
			var articleNodes = doc.DocumentNode.SelectNodes("//p[contains(@class, 'article-paragraph')]");

			// Extract YouTube video URLs from any iframe inside .article-content
			var videoUrls = doc.DocumentNode.SelectNodes("//*[contains(@class, 'article-content')]//iframe[contains(@src, 'youtube.com/embed')]")?.Select(node => node.GetAttributeValue("src", "")).ToList() ?? new List<string>();

			// Extract image URLs from any img inside .article-content, but skip logos/ads by checking for data attributes or alt text if needed
			var imageUrls = doc.DocumentNode.SelectNodes("//*[contains(@class, 'article-content')]//img[not(contains(@class, 'logo')) and not(contains(@src, 'sprite'))]")?.Select(node => node.GetAttributeValue("src", "")).ToList() ?? new List<string>();



			articleData.Images = imageUrls;
			articleData.Videos = videoUrls;

			if (articleNodes != null && articleNodes.Any())
			{
				var textNodes = articleNodes.Skip(3).ToList();
				textNodes = textNodes.Take(textNodes.Count - 1).ToList();

				// Join all paragraphs with newlines
				var articleText = string.Join("\n\n", textNodes
					.Select(node => HtmlEntity.DeEntitize(node.InnerText.Trim()))
					.Where(text => !string.IsNullOrWhiteSpace(text)));

				articleData.ArticleText = articleText;
			}

			return articleData;
		}
	}
}
