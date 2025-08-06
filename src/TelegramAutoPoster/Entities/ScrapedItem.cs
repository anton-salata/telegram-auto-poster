namespace TelegramAutoPoster.Entities
{
	public class ScrapedItem
	{
		public string Url { get; set; }
		public string ImageUrl { get; set; }
		public string FormattedMessage { get; set; }
		public DateTime PublishDate { get; set; }
		public PostFormat Format { get; set; } = PostFormat.SinglePost;
		public List<string> Tags { get; set; } = new List<string>();
		public string AuthorName { get; set; }
		public string AuthorLink { get; set; }
		public string Title { get; set; }
		public string PlainText { get; set; }
		public string PlainDate { get; set; }
		public List<string> Images { get; set; } = new List<string>();
		public List<string> Videos { get; set; } = new List<string>();
	}

	public enum PostFormat
	{
		SinglePost,  // For short posts (caption ≤ 1024 chars)
		MultiViaComments,     // For long posts (media + split text) + rest in comments
		MultiViaPosts,     // For long posts (media + split text) + rest as new text post
	}
}
