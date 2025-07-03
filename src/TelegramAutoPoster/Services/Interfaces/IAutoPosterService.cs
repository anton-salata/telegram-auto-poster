namespace TelegramAutoPoster.Services.Interfaces
{
    public interface IAutoPosterService
    {
        Task Run(CancellationToken cancellationToken);
    }
}