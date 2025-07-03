using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using TelegramAutoPoster.Storage.Interfaces;

namespace TelegramAutoPoster.Storage
{
    public class ProcessedItemStore : IProcessedItemStore
	{
		private readonly string _dbPath;
		private readonly ILogger<ProcessedItemStore> _logger;
		private readonly SemaphoreSlim _semaphore = new(1, 1);

		public ProcessedItemStore(ILogger<ProcessedItemStore> logger)
		{
			_logger = logger;

			_dbPath = Path.Combine(AppContext.BaseDirectory, "processed-urls.db");

			InitializeAsync().Wait();
		}

		public async Task InitializeAsync()
		{
			await _semaphore.WaitAsync();
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);

				await using var connection = new SqliteConnection($"Data Source={_dbPath};");
				await connection.OpenAsync();

				var command = connection.CreateCommand();
				command.CommandText = @"
                CREATE TABLE IF NOT EXISTS ProcessedItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Url TEXT NOT NULL UNIQUE,
                    FeedId TEXT
                );
            ";
				await command.ExecuteNonQueryAsync();
			}
			finally
			{
				_semaphore.Release();
			}
		}

		public async Task<bool> IsProcessedAsync(string url)
		{
			await _semaphore.WaitAsync();
			try
			{
				await using var connection = new SqliteConnection($"Data Source={_dbPath};");
				await connection.OpenAsync();

				var command = connection.CreateCommand();
				command.CommandText = "SELECT COUNT(*) FROM ProcessedItems WHERE Url = $url";
				command.Parameters.AddWithValue("$url", url);

				var count = (long?)await command.ExecuteScalarAsync();
				return count > 0;
			}
			finally
			{
				_semaphore.Release();
			}
		}

		public async Task MarkAsProcessedAsync(string url, string? feedId = null)
		{
			await _semaphore.WaitAsync();
			try
			{
				await using var connection = new SqliteConnection($"Data Source={_dbPath};");
				await connection.OpenAsync();

				var command = connection.CreateCommand();
				command.CommandText = "INSERT OR IGNORE INTO ProcessedItems (Url, FeedId) VALUES ($url, $feedId)";
				command.Parameters.AddWithValue("$url", url);
				command.Parameters.AddWithValue("$feedId", feedId ?? (object)DBNull.Value);

				await command.ExecuteNonQueryAsync();
			}
			finally
			{
				_semaphore.Release();
			}
		}
	}
}
