using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramAutoPoster.Clients.Interfaces;
using TelegramAutoPoster.Configuration;
using TelegramAutoPoster.Entities;

namespace TelegramAutoPoster.Clients
{
	public class TgBotClient : ITgBotClient
	{
		private readonly HttpClient _httpClient;
		private readonly ITelegramBotClient _bot;

		public TgBotClient(IHttpClientFactory httpClientFactory, IOptions<TgBotConfig> tgBotSettings)
		{
			_httpClient = httpClientFactory.CreateClient("WithProxy");//.CreateClient(); //.CreateClient("WithProxy");
			_bot = new TelegramBotClient(tgBotSettings.Value.TelegramBotToken, _httpClient);
		}

		public async Task PostAsync(ScrapedItem item, string channelUsername, CancellationToken cancellationToken = default)
		{
			var request = new SendPhotoRequest
			{
				ChatId = new ChatId(channelUsername),
				Photo = InputFile.FromUri(item.ImageUrl.Split('?')[0]), // or use FromStream if uploading
				Caption = item.Message,
				ParseMode = ParseMode.Markdown
			};

			await _bot.SendRequest(request, cancellationToken);
		}
	}
}
