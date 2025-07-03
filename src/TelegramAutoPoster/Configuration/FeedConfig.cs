namespace TelegramAutoPoster.Configuration
{
	public class FeedsSettings
	{
		public DateTime? StartDateTime { get; set; }
		public List<FeedConfig> Feeds { get; set; }
	}

	public class FeedConfig
	{
		public string ScraperId { get; set; }
		public string FeedUrl { get; set; }
		public string TelegramChannelId { get; set; }
	}
}
