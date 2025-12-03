namespace Application.Contract
{
    public interface IBackgroundJob
    {
        /// <summary>
        /// Método que Hangfire utilizará para registrar los RecurringJobs.
        /// </summary>
        Task RegisterRecurringJobsAsync(CancellationToken cancellationToken);

    }
    /// <summary>
    /// Interfaz genérica que facilita obtener un Job por tipo T.
    /// </summary>
    public interface IBackgroundJob<TJob> where TJob : IBackgroundJob
    {
        TJob Instance { get; }
    }
}
