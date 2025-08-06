using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelegramAutoPoster.Clients.Interfaces;
using TelegramAutoPoster.Configuration;
using TelegramAutoPoster.Scrapers.Interfaces;
using TelegramAutoPoster.Services.Interfaces;
using TelegramAutoPoster.Storage.Interfaces;
using TelegramAutoPoster.Utilities;

namespace TelegramAutoPoster.Services
{
	public class AutoPosterService : BackgroundService, IAutoPosterService
	{
		private readonly Dictionary<string, IScraper> _scrapers; // Key = ScraperId
		private readonly ITgBotClient _tgBotClient;
		private readonly IProcessedItemStore _processedItemStore;
		private readonly FeedsSettings _feedSettings;
		private readonly ILogger<AutoPosterService> _logger;

		public AutoPosterService(
			IEnumerable<IScraper> scrapers,
			ITgBotClient tgBotClient,
			IProcessedItemStore processedItemStore,
			IOptions<FeedsSettings> feedSettings,
			ILogger<AutoPosterService> logger)
		{
			_scrapers = scrapers.ToDictionary(s => s.Id);
			_tgBotClient = tgBotClient;
			_processedItemStore = processedItemStore;
			_feedSettings = feedSettings.Value;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("AutoPosterService started.");

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					await Run(stoppingToken);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error occurred during AutoPosterService.Run()");
				}

				_logger.LogInformation("Waiting 1 hour until next run...");
				await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
				_logger.LogInformation("Woke up. Starting next scraping cycle...");
			}
		}

		public async Task Run(CancellationToken cancellationToken)
		{
			foreach (var feed in _feedSettings.Feeds)
			{
				if (_scrapers.TryGetValue(feed.ScraperId, out var scraper))
				{
					var items = await scraper.ScrapeAsync(feed.FeedUrl, cancellationToken);

					//if (_feedSettings.StartDateTime is DateTime startDate)
					//{
					//	items = items.Where(i => i.PublishDate > startDate).ToList();
					//}

					items = await items
									.ToAsyncEnumerable()
									.WhereAwait(async i => !await _processedItemStore.IsProcessedAsync(i.Url))
									.ToListAsync();

					items = items.Where(i => !string.IsNullOrEmpty(i.ImageUrl)).ToList();

					foreach (var item in items)
					{
						try
						{
							await _tgBotClient.PostAsync(item, feed.TelegramChannelId, cancellationToken);

							await _processedItemStore.MarkAsProcessedAsync(item.Url, feed.ScraperId);
						}
						catch (Exception ex) 
						{
							_logger.LogError(ex, "Failed to process item");
						}

						await DelayUtilities.DelayRandomAsync(cancellationToken);
					}
				}
				else
				{
					_logger.LogWarning("No scraper found for ID: {ScraperId}", feed.ScraperId);
				}
			}
		}
	}
}
