namespace Application.Contracts
{
    public interface ILoggerManager<TService> where TService : class
    {
        void LogDebug(string message, object? data = null);
        void LogError(string message, object? data = null);
        void LogInfo(string message, object? data = null);
        void LogWarn(string message, object? data = null);
    }
}
