using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramAutoPoster.Clients.Interfaces;
using TelegramAutoPoster.Configuration;
using TelegramAutoPoster.Entities;
using TelegramAutoPoster.Utilities;

namespace TelegramAutoPoster.Clients
{
	public class TgBotClient : ITgBotClient
	{
		private const int MaxCaptionLength = 1024;

		private readonly HttpClient _httpClient;
		private readonly ITelegramBotClient _bot;

		public TgBotClient(IHttpClientFactory httpClientFactory, IOptions<TgBotConfig> tgBotSettings)
		{
			_httpClient = httpClientFactory.CreateClient();//.CreateClient(); //.CreateClient("WithProxy");
			_bot = new TelegramBotClient(tgBotSettings.Value.TelegramBotToken, _httpClient);
		}

		//public async Task PostAsync(ScrapedItem item, string channelUsername, CancellationToken cancellationToken = default)
		//{
		//	var request = new SendPhotoRequest
		//	{
		//		ChatId = new ChatId(channelUsername),
		//		Photo = InputFile.FromUri(item.ImageUrl.Split('?')[0]), // or use FromStream if uploading
		//		Caption = item.Message,				
		//		ParseMode = ParseMode.Markdown
		//	};

		//	await _bot.SendRequest(request, cancellationToken);
		//}

		public async Task PostAsync(ScrapedItem item, string channelUsername, CancellationToken cancellationToken = default)
		{
			var cleanedMessage = CleanTextForMarkdown(item.FormattedMessage);

			if (item.Format == PostFormat.SinglePost)
			{
				// Short post (single photo with caption)
				var caption = cleanedMessage.Length > MaxCaptionLength
					? cleanedMessage.Substring(0, MaxCaptionLength - 4) + "..."
					: cleanedMessage;

				await _bot.SendPhoto(
					chatId: channelUsername,
					photo: InputFile.FromUri(item.ImageUrl.Split('?')[0]),
					caption: caption,
					parseMode: ParseMode.Markdown,
					cancellationToken: cancellationToken);
			}
			else if (item.Format == PostFormat.MultiViaPosts)
			{
				// Escape HTML and split content
				var (firstPart, remainingParts) = TelegramMessageHelper.SplitFormattedText(cleanedMessage);

				// First message: Image + Title + First part of text
				await _bot.SendPhoto(
					chatId: channelUsername,
					photo: InputFile.FromUri(item.ImageUrl.Split('?')[0]),
					caption: firstPart,
					parseMode: ParseMode.Markdown,
					cancellationToken: cancellationToken
				);

				// Subsequent messages (full Markdown formatting)
				foreach (var part in remainingParts)
				{
					await _bot.SendMessage(
						chatId: channelUsername,
						text: part,
						parseMode: ParseMode.Markdown,
						linkPreviewOptions: new LinkPreviewOptions()
						{
							IsDisabled = true
						},
						cancellationToken: cancellationToken
					);
					await Task.Delay(500); // Avoid rate limits
				}
			}
			else if (item.Format == PostFormat.MultiViaComments)
			{
				// 1. Prepare caption components
				var title = $"*{item.Title}*"; // Bold title from ScrapedItem
				var byline = $"\n\nBy [{item.AuthorName}]({item.AuthorLink})";
				//var tags = item.Tags.Any()
				//					? $"\n\n{string.Join(" ", item.Tags.Select(t => $"\\#{t.Replace(" ", "_")}"))}"
				//					: "";
				var tags = item.Tags.Any()
									? $"\n\n{string.Join(" ", item.Tags.Select(t => $"#{t.Replace(" ", "").Replace("-", "")}"))}"
									: "";

				// 2. Split the PlainText (article body only) into parts
				var (captionPart, commentParts) = TelegramMessageHelper.SplitFormattedText(CleanTextForMarkdown(item.PlainText));

				var continueNote = commentParts.Any()
								? "\n\n_Continue reading in comments_"
								: "";

				// 3. Send photo with title + truncated article
				var photoMessage = await _bot.SendPhoto(
					chatId: channelUsername,
					photo: InputFile.FromUri(item.ImageUrl.Split('?')[0]),
					caption: $"{title}\n\n{captionPart}{continueNote}{byline}{tags}",
					parseMode: ParseMode.Markdown,
					cancellationToken: cancellationToken
				);

				// 4. Post comments (continue where caption left off)
				var discussionChatId = await GetDiscussionChatId(channelUsername);

				await Task.Delay(5000);
				//var threadId = await GetLatestThreadIdInDiscussionGroup(discussionChatId);

				var messageToReplyTo = await GetLatestMessageIdInDiscussionGroup(discussionChatId);

				if (commentParts.Count > 0)
				{
					Message firstComment = null;
					Message prevComment = null;

					for (int i = 0; i < commentParts.Count; i++)
					{
						var isLastComment = (i == commentParts.Count - 1);
						var commentText = new StringBuilder();

						//// Add title only to the first comment
						//if (i == 0)
						//	commentText.AppendLine($"*{EscapeMarkdownV2(item.Title)}*");

						commentText.AppendLine(commentParts[i]);

						// Add author/date only to the last comment
						if (isLastComment)
						{
							commentText.AppendLine($"\n\nBy [{item.AuthorName}]({item.AuthorLink})");

							if (!string.IsNullOrEmpty(item.PlainDate))
							{
								commentText.AppendLine($"\nPosted: {item.PlainDate}");
							}

							commentText.AppendLine($"{tags}");
						}

						var comment = await _bot.SendMessage(
							chatId: discussionChatId,
							text: commentText.ToString(),
							parseMode: ParseMode.Markdown,
							replyParameters: new ReplyParameters()
							{
								MessageId = i == 0 ? messageToReplyTo.Value : prevComment.MessageId
							},
							linkPreviewOptions: new LinkPreviewOptions()
							{
								IsDisabled = true
							},
							//replyParameters: new ReplyParameters()
							//{
							//	MessageId = i == 0 ? photoMessage.MessageId : firstComment.MessageId
							//},							
							//messageThreadId: threadId.Value,
							cancellationToken: cancellationToken
						);

						prevComment = comment;

						if (i == 0) firstComment = comment;
						await Task.Delay(500); // Avoid rate limits
					}

					//// 5. Add "Read More" button linking to the first comment
					////var messageLink = $"https://t.me/c/{channelUsername.Substring(4)}/{photoMessage.MessageId}?thread={firstComment.MessageId}";

					//var messageLink = GetPrivateDiscussionMessageLink(discussionChatId, firstComment.Id);

					//await _bot.EditMessageReplyMarkup(
					//	chatId: channelUsername,
					//	messageId: photoMessage.MessageId,
					//	replyMarkup: new InlineKeyboardMarkup(
					//		InlineKeyboardButton.WithUrl("📖 Continue reading", messageLink)
					//	),
					//	cancellationToken: cancellationToken
					//);
				}
			}
		}

		private static string CleanTextForMarkdown(string text)
		{
			// 1. Decode HTML entities (e.g., &#8220; → “)
			string decodedText = HttpUtility.HtmlDecode(text);

			// 2. Fix edge cases (optional)
			decodedText = decodedText.Replace(" &nbsp;", " "); // Replace non-breaking spaces

			// 3. Preserve Markdown syntax (don't escape * _ [ ] etc.)
			return decodedText;
		}

		private async Task<long> GetDiscussionChatId(string channelUsername)
		{
			var chat = await _bot.GetChat(channelUsername);
			if (chat.LinkedChatId == null)
				throw new Exception("No discussion group linked to channel!");
			return chat.LinkedChatId.Value;
		}

		private async Task<int?> GetLatestMessageIdInDiscussionGroup(long discussionChatId)
		{
			var updates = await _bot.GetUpdates();

			// Get the most recent message in the linked group from the bot
			var latestGroupMessage = updates
				.Select(u => u.Message)
				.Where(m => m != null)
				.OrderByDescending(m => m.Date)
				.FirstOrDefault();

			return latestGroupMessage?.Id;
		}

		private string GetPrivateDiscussionMessageLink(long discussionChatId, int messageId)
		{
			// Telegram private chat IDs start with -100 prefix
			// Remove the "-100" prefix to create the link part
			var idPart = discussionChatId.ToString();

			if (idPart.StartsWith("-100"))
				idPart = idPart.Substring(4); // Remove first 4 characters "-100"

			return $"https://t.me/c/{idPart}/{messageId}";
		}

	}
}
