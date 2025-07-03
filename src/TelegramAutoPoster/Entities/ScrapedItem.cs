namespace TelegramAutoPoster.Entities
{
	public class ScrapedItem
	{
		public string Url { get; set; }
		public string ImageUrl { get; set; }
		public string Message { get; set; }
		public DateTime PublishDate { get; set; }
	}
}
