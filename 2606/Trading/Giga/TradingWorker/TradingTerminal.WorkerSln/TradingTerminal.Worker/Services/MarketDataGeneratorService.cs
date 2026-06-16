// Services/MarketDataGeneratorService.cs

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;
using TradingTerminal.Providers; // ВАЖНО: Убедитесь, что namespace верный
using Microsoft.Extensions.Hosting; // Добавляем using для IHostApplicationLifetime
using System.Threading;

namespace TradingTerminal.Worker.Services
{
    /// <summary>
    /// Фоновый сервис-генератор рыночных данных.
    /// Является источником данных для конвейера TPL Dataflow.
    /// </summary>
    public class MarketDataGeneratorService : BackgroundService
    {
        private readonly ILogger<MarketDataGeneratorService> _logger;
        private readonly BufferBlock<object> _dataBufferBlock;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        /// <summary>
        /// Конструктор принимает ссылку на блок-источник данных и менеджер времени жизни хоста.
        /// </summary>
        public MarketDataGeneratorService(
            ILogger<MarketDataGeneratorService> logger,
            BufferBlock<object> dataBufferBlock,
            IHostApplicationLifetime hostApplicationLifetime)
        {
            _logger = logger;
            _dataBufferBlock = dataBufferBlock;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        /// <summary>
        /// Основной метод выполнения сервиса.
        /// Запускает генерацию данных и отправляет их в конвейер.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Сервис генерации рыночных данных запущен.");

            try
            {
                // Используем ApplicationStopping вместо параметра stoppingToken,
                // чтобы генератор мог корректно завершить цикл после получения сигнала о завершении.
                await foreach (var marketData in MarketDataProvider.GetMarketDataAsync(_hostApplicationLifetime.ApplicationStopping))
                {
                    if (!_dataBufferBlock.Post(marketData))
                    {
                        // Если буфер переполнен, немного подождем и попробуем снова.
                        await Task.Delay(50, _hostApplicationLifetime.ApplicationStopping);
                        _dataBufferBlock.Post(marketData);
                    }
                }

                // После завершения цикла помечаем источник данных как завершенный.
                _dataBufferBlock.Complete();
            }
            catch (OperationCanceledException)
            {
                // Это ожидаемое исключение при отмене операции.
                _logger.LogInformation("Генерация рыночных данных была отменена.");
                _dataBufferBlock.Complete(); // Все равно помечаем блок как завершенный.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла ошибка в сервисе генерации данных.");
                throw;
            }

            _logger.LogInformation("Сервис генерации рыночных данных остановлен.");
        }
    }
}
