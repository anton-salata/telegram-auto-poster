using HtmlAgilityPack;
using TelegramAutoPoster.Entities;
using TelegramAutoPoster.Scrapers.Entities;

namespace TelegramAutoPoster.Scrapers
{
	public class TechCrunchScraper : BaseScraper
	{
		public TechCrunchScraper(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
		{
		}

		public override string Id => "TechCrunch";

		public override async Task<IEnumerable<ScrapedItem>> ScrapeAsync(string url, CancellationToken cancellationToken)
		{
			var html = await _httpClient.GetStringAsync(url);

			var doc = new HtmlDocument();
			doc.LoadHtml(html);

			var items = new List<ScrapedItem>();

			// Select all article links
			var articleLinks = new List<string>();
			var articleNodes = doc.DocumentNode.SelectNodes("//a[contains(@class, 'loop-card__title-link')]");

			if (articleNodes != null)
			{
				foreach (var node in articleNodes)
				{
					string href = node.GetAttributeValue("href", "");
					if (!string.IsNullOrEmpty(href))
					{
						articleLinks.Add(href);
						Console.WriteLine(href);
					}
				}
			}

			foreach (var articleLink in articleLinks.Distinct())
			{
				var articleHtml = await _httpClient.GetStringAsync(articleLink);

				var articleDoc = new HtmlDocument();
				articleDoc.LoadHtml(articleHtml);

				var article = new ArticleData();

				// Check for "In Brief" specific elements				
				if (articleDoc.DocumentNode.SelectSingleNode("//span[contains(@class, 'loop-card__cat') and contains(text(), 'In Brief')]") != null)
				{
					Console.WriteLine("Scraping in brief article type");

					article = await ScrapeInBriefArticeType(articleDoc, cancellationToken);
				}
				// Check for full article specific elements
				else if (articleDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'article-hero')]") != null)
				{
					Console.WriteLine("Scraping full article type");

					article = await ScrapeFullArticeType(articleDoc, cancellationToken);
				}
				else
				{
					Console.WriteLine("Cannot determine article type");
					continue;
				}

				var scrapedItem = new ScrapedItem()
				{
					Url = articleLink,
					FormattedMessage = $"*{article.Title}*\n\n{article.ArticleText}\n\nBy [{article.AuthorName}]({article.AuthorLink})",
					ImageUrl = article.MainImageUrl,
					Format = PostFormat.MultiViaComments, //article.ArticleText.Length > 3500 ? PostFormat.MultiViaPosts : PostFormat.SinglePost,
					PublishDate = DateTime.Now,
					PlainText = article.ArticleText,
					Tags = article.Tags,
					AuthorLink = article.AuthorLink,
					AuthorName = article.AuthorName,
					PlainDate = article.PostDate,
					Title = article.Title
				};

				items.Add(scrapedItem);
			}

			return items;
		}


		private async Task<ArticleData> ScrapeFullArticeType(HtmlDocument articleDoc, CancellationToken cancellationToken)
		{
			var article = new ArticleData();

			// Extract main image URL from article-hero section
			var featuredImage = articleDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'article-hero')]//figure//img");
			if (featuredImage != null)
			{
				article.MainImageUrl = featuredImage.GetAttributeValue("src", "");
			}

			// Extract category
			var categoryNode = articleDoc.DocumentNode.SelectSingleNode("//a[contains(@class, 'wp-block-tenup-post-primary-term')]");
			if (categoryNode != null)
			{
				article.Category = categoryNode.InnerText.Trim();
			}

			// Extract post date
			var dateNode = articleDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'article-hero__date')]//time");
			if (dateNode != null)
			{
				article.PostDate = dateNode.InnerText.Trim();
			}

			// Extract author name and link from article-hero section
			var authorNode = articleDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'article-hero__authors')]//a");
			if (authorNode != null)
			{
				article.AuthorName = authorNode.InnerText.Trim();
				article.AuthorLink = authorNode.GetAttributeValue("href", "");
			}

			// Extract title
			var titleNode = articleDoc.DocumentNode.SelectSingleNode("//h1[contains(@class, 'article-hero__title')]");
			if (titleNode != null)
			{
				article.Title = titleNode.InnerText.Trim();
			}

			// Extract article text (combining all paragraphs from entry-content)
			var contentNode = articleDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'entry-content')]");
			if (contentNode != null)
			{
				// Remove any ads or unwanted elements
				foreach (var ad in contentNode.SelectNodes(".//div[contains(@class, 'ad-unit')]") ?? Enumerable.Empty<HtmlNode>())
				{
					ad.Remove();
				}

				// Remove inline CTAs
				foreach (var cta in contentNode.SelectNodes(".//div[contains(@class, 'inline-cta')]") ?? Enumerable.Empty<HtmlNode>())
				{
					cta.Remove();
				}

				article.ArticleText = string.Join("\n\n",
					contentNode.SelectNodes(".//p")?
						.Select(p => p.InnerText.Trim()) ?? Enumerable.Empty<string>());
			}

			// Extract tags from relevant terms section
			var tagsContainer = articleDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'tc23-post-relevant-terms__terms')]");
			if (tagsContainer != null)
			{
				var tagNodes = tagsContainer.SelectNodes(".//a");
				if (tagNodes != null)
				{
					foreach (var tagNode in tagNodes)
					{
						article.Tags.Add(tagNode.InnerText.Trim());
					}
				}
			}

			// Output the extracted data
			Console.WriteLine($"Main Image: {article.MainImageUrl}");
			Console.WriteLine($"Category: {article.Category}");
			Console.WriteLine($"Post Date: {article.PostDate}");
			Console.WriteLine($"Author: {article.AuthorName} ({article.AuthorLink})");
			Console.WriteLine($"Title: {article.Title}");
			Console.WriteLine("\nArticle Text:");
			Console.WriteLine(article.ArticleText);
			Console.WriteLine("\nTags:");
			Console.WriteLine(string.Join(", ", article.Tags));

			return article;
		}

		private async Task<ArticleData> ScrapeInBriefArticeType(HtmlDocument articleDoc, CancellationToken cancellationToken)
		{
			var article = new ArticleData();

			// Extract main image URL
			var featuredImage = articleDoc.DocumentNode.SelectSingleNode("//figure[contains(@class, 'wp-block-post-featured-image')]/img");
			if (featuredImage != null)
			{
				article.MainImageUrl = featuredImage.GetAttributeValue("src", "");
			}

			// Extract category
			var categoryNode = articleDoc.DocumentNode.SelectSingleNode("//span[contains(@class, 'wp-block-tenup-post-primary-term')]");
			if (categoryNode != null)
			{
				article.Category = categoryNode.InnerText.Trim();
			}

			// Extract post date
			var dateNode = articleDoc.DocumentNode.SelectSingleNode("//time");
			if (dateNode != null)
			{
				article.PostDate = dateNode.InnerText.Trim();
			}

			// Extract author name and link
			var authorNode = articleDoc.DocumentNode.SelectSingleNode("//a[contains(@class, 'post-authors-list__author')]");
			if (authorNode != null)
			{
				article.AuthorName = authorNode.InnerText.Trim();
				article.AuthorLink = authorNode.GetAttributeValue("href", "");
			}

			// Extract title
			var titleNode = articleDoc.DocumentNode.SelectSingleNode("//h1[contains(@class, 'wp-block-post-title')]");
			if (titleNode != null)
			{
				article.Title = titleNode.InnerText.Trim();
			}

			// Extract article text (combining all paragraphs)
			var contentNode = articleDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'entry-content')]");
			if (contentNode != null)
			{
				// Remove any ads or unwanted elements
				foreach (var ad in contentNode.SelectNodes(".//div[contains(@class, 'ad-unit')]") ?? Enumerable.Empty<HtmlNode>())
				{
					ad.Remove();
				}

				article.ArticleText = string.Join("\n\n",
					contentNode.SelectNodes(".//p")?
						.Select(p => p.InnerText.Trim()) ?? Enumerable.Empty<string>());
			}

			// Extract tags
			var tagsContainer = articleDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'tc23-post-relevant-terms__terms')]");
			if (tagsContainer != null)
			{
				// Get all anchor tags that don't contain region links
				var tagNodes = tagsContainer.SelectNodes(".//a[not(contains(@href, '/region/'))]");
				if (tagNodes != null)
				{
					foreach (var tagNode in tagNodes)
					{
						article.Tags.Add(tagNode.InnerText.Trim());
					}
				}
			}

			Console.WriteLine($"Main Image: {article.MainImageUrl}");
			Console.WriteLine($"Category: {article.Category}");
			Console.WriteLine($"Post Date: {article.PostDate}");
			Console.WriteLine($"Author: {article.AuthorName} ({article.AuthorLink})");
			Console.WriteLine($"Title: {article.Title}");
			Console.WriteLine("\nArticle Text:");
			Console.WriteLine(article.ArticleText);
			Console.WriteLine("\nTags:");
			Console.WriteLine(string.Join(", ", article.Tags));

			return article;
		}
	}
}