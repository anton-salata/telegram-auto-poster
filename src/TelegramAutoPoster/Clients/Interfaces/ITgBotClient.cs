using TelegramAutoPoster.Entities;

namespace TelegramAutoPoster.Clients.Interfaces
{
	public interface ITgBotClient
	{
		Task PostAsync(ScrapedItem item, string channelUsername, CancellationToken cancellationToken = default);
	}
}
