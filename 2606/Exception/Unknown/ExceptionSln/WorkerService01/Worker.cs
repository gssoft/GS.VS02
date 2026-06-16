using System.Diagnostics;

namespace WorkerService01
{
    public class Worker(ILogger<Worker> logger) : BackgroundService
    {
        private int _eventCounter = 1;
        DateTime _startTime = DateTime.MinValue;
        DateTime  _currentTime = DateTime.MinValue;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // DateTime time = DateTime.MinValue;
            try
            {
                DateTime _currentTime = DateTime.MinValue;
                while (!stoppingToken.IsCancellationRequested)
                {
                    _eventCounter++;

                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation($"Worker running at: {_currentTime}", DateTimeOffset.Now);
                    }
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Логируем, если была отменена сама операция публикации.
                logger.LogWarning($"OperationCanceledException {_currentTime}");
            }
            catch (Exception)
            {
                logger.LogError ("Publishing of event {EventId} was cancelled.", _eventCounter);
                // throw;
            }
            finally
            {
                logger.LogInformation($"Worker stopped at: {_currentTime}", _eventCounter);
            }          
        }
    }
}
