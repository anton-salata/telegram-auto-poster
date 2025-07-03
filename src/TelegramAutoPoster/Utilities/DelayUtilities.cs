namespace TelegramAutoPoster.Utilities
{
	public static class DelayUtilities
	{
		private static readonly Random _random = new();

		public static async Task DelayRandomAsync(CancellationToken cancellationToken = default)
		{
			var delaySeconds = 0.5 + (_random.NextDouble() * 3.0); // 0.5 to 3.5
			var delayMilliseconds = (int)(delaySeconds * 1000);
			await Task.Delay(delayMilliseconds, cancellationToken);
		}
	}
}
