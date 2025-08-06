namespace TelegramAutoPoster.Clients.Interfaces
{
	public interface IAiChatClient
	{
		Task<string> GetAnswerAsync(string prompt);
	}
}