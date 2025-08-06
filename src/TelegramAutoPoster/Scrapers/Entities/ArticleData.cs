namespace TelegramAutoPoster.Scrapers.Entities
{
	public class ArticleData
	{
		public string MainImageUrl { get; set; }
		public string Category { get; set; }
		public string PostDate { get; set; }
		public string AuthorName { get; set; }
		public string AuthorLink { get; set; }
		public string Title { get; set; }
		public string ArticleText { get; set; }
		public List<string> Tags { get; set; } = new List<string>();
		public List<string> Images { get; set; } = new List<string>();
		public List<string> Videos { get; set; } = new List<string>();
	}
}
