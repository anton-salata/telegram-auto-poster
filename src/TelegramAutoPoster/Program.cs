//using Microsoft.Extensions.Configuration;
using AlienWireBot.Scraper;
using BmwNewsBot.Scraper;
using CarNewsBot.Scraper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using TelegramAutoPoster.Clients;
using TelegramAutoPoster.Clients.Interfaces;
using TelegramAutoPoster.Configuration;
using TelegramAutoPoster.Scrapers.Interfaces;
using TelegramAutoPoster.Services;
using TelegramAutoPoster.Storage;
using TelegramAutoPoster.Storage.Interfaces;

namespace TelegramAutoPoster
{
	public class Program
	{
		public static void Main(string[] args)
		{
			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureServices((hostContext, services) =>
				{
					var config = hostContext.Configuration;

					services.Configure<TgBotConfig>(config);
					services.Configure<FeedsSettings>(config);
					// Or register as singletons (optional if not using IOptions pattern)
					//services.AddSingleton(config.Get<TgBotSettings>());
					//services.AddSingleton(config.Get<StorageSettings>());
					//services.AddSingleton(config.GetSection("Feeds").Get<FeedsSettings>());


					//services.AddHttpClient();
					// Register a named HttpClient with proxy
					services.AddHttpClient("WithProxy")
								.ConfigurePrimaryHttpMessageHandler(() =>
								{
									var proxy = new WebProxy("{proxy}")
									{
										UseDefaultCredentials = true
									};

									return new HttpClientHandler
									{
										Proxy = proxy,
										UseProxy = true,
										UseDefaultCredentials = true
									};
								});

					// Register Scrapers
					services.AddSingleton<IScraper, AlienWireScraper>();
					services.AddSingleton<IScraper, BmwNewsScraper>();
					services.AddSingleton<IScraper, CarNewsScraper>();

					// Register Storage
					services.AddSingleton<IProcessedItemStore, ProcessedItemStore>();

					// Register Telegram Bot Client
					services.AddSingleton<ITgBotClient, TgBotClient>();

					// Register AutoPosterService as the background service
					services.AddHostedService<AutoPosterService>();
				})
				.ConfigureLogging(logging =>
				{
					logging.ClearProviders();
					logging.AddConsole();
				});
	}
}
