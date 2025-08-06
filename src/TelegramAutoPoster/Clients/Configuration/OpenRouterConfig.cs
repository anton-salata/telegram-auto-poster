namespace TelegramAutoPoster.Clients.Configuration
{
	public class OpenRouterConfig
	{
		public int RequestsLimitPerDay { get; set; } = 10;
		public string ApiKey { get; set; }
		public string ApiUrl { get; set; } = "https://openrouter.ai/api/v1/chat/completions";
		public string Model { get; set; } = "google/gemma-3n-e4b-it:free";
		public string ProxyUrl { get; set; }
	}
}
