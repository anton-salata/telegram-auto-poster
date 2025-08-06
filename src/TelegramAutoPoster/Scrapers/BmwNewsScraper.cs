using HtmlAgilityPack;
using System.Globalization;
using System.Text;
using TelegramAutoPoster.Entities;
using TelegramAutoPoster.Scrapers;
using TelegramAutoPoster.Scrapers.Entities;

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
					if (!DateTime.TryParseExact(dateText, "MMMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var publishDate))
					{
						publishDate = DateTime.Now;
					}

					if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(link))
					{
						var articleData = await GetArticleData(link);

						items.Add(new ScrapedItem
						{
							Url = link,
							FormattedMessage = $"📰 *{HtmlEntity.DeEntitize(title)}*\n\n[Read more]({link})\n🕒 {publishDate:yyyy-MM-dd HH:mm}",
							ImageUrl = imageUrl,
							PublishDate = publishDate,
							Title = title,
							PlainText = articleData.ArticleText,
							Format = PostFormat.MultiViaComments,
							AuthorLink = articleData.AuthorLink,
							AuthorName = articleData.AuthorName,
							Tags = articleData.Tags
						});


						// Output for test
						Console.WriteLine($"Title: {title}");
						Console.WriteLine($"Image: {imageUrl}");
						Console.WriteLine($"Link: {link}");
						Console.WriteLine($"Author: {articleData.AuthorName}");
						Console.WriteLine($"Author Link: {articleData.AuthorLink}");
						Console.WriteLine($"Posted Date: {publishDate}");
						Console.WriteLine($"Plain Text: {articleData.ArticleText}");
						Console.WriteLine($"Images: {string.Join("\n", articleData.Images)}");
						Console.WriteLine($"Videos: {string.Join("\n", articleData.Videos)}");
						Console.WriteLine("\nTags:");
						Console.WriteLine(string.Join(", ", articleData.Tags));
					}
				}
				catch (Exception ex)
				{
					// Optionally log or skip problematic article node
					Console.WriteLine($"Scraper: {Id}\n" + ex.ToString());
				}
			}

			return items;
		}

		private async Task<ArticleData> GetArticleData(string link)
		{
			var html = await _httpClient.GetStringAsync(link);

			var doc = new HtmlDocument();
			doc.LoadHtml(html);

			var articleData = new ArticleData();

			var contentDiv = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'post-content')]");

			if (contentDiv != null)
			{
				// Remove unwanted elements like scripts, video blocks, ads
				var unwantedSelectors = new[] { "script", "style", "div[contains(@class, 'bmwbl-')]" };
				foreach (var selector in unwantedSelectors)
				{
					var nodes = contentDiv.SelectNodes(".//" + selector);
					if (nodes != null)
					{
						foreach (var node in nodes)
							node.Remove();
					}
				}

				// Extract plain text including headings and paragraphs
				var sb = new StringBuilder();

				foreach (var node in contentDiv.Descendants()
											   .Where(n => n.Name == "p" || n.Name == "h3" || n.Name == "h2" || n.Name == "h1"))
				{
					sb.AppendLine(HtmlEntity.DeEntitize(node.InnerText.Trim()));
					sb.AppendLine();
				}

				var articleText = sb.ToString().Trim();

				articleData.ArticleText = articleText;
			}
			else
			{
				Console.WriteLine($"Post content not found: {link}");
			}

			var authorNode = doc.DocumentNode.SelectSingleNode("//p[contains(@class, 'byline')]//span[@itemprop='author']//a");

			if (authorNode != null)
			{
				var authorName = authorNode.SelectSingleNode(".//span[@itemprop='name']")?.InnerText.Trim();
				var authorLink = authorNode.GetAttributeValue("href", string.Empty);

				articleData.AuthorName = authorName;
				articleData.AuthorLink = authorLink;
			}
			else
			{
				Console.WriteLine($"Author not found {link}");
			}

			var tags = doc.DocumentNode.SelectNodes("//div[@class='the-tags']//li[a[@rel='tag']]")
				?.Select(li => li.SelectSingleNode(".//a")?.InnerText.Trim())
				.Where(tag => !string.IsNullOrEmpty(tag)) ?? Enumerable.Empty<string>();

			articleData.Tags = tags.ToList();

			return articleData;
		}
	}
}
