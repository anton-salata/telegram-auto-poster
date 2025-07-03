namespace TelegramAutoPoster.Storage.Interfaces
{
    public interface IProcessedItemStore
    {
        Task<bool> IsProcessedAsync(string url);
        Task MarkAsProcessedAsync(string url, string? feedId = null);
    }
}