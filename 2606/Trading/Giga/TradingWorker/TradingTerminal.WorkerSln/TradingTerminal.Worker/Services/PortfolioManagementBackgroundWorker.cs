// Servcies/PortfolioManagementBackgroundWorker.cs

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;
using System.Threading.Tasks;
using System.Threading;
using TradingTerminal.Services; // Для доступа к OrderExecutionService

namespace TradingTerminal.Worker.Services
{
    /// <summary>
    /// Фоновый сервис, который управляет портфелем сделок.
    /// Обрабатывает входящие сделки (Trade) через ActionBlock.
    /// </summary>
    public class PortfolioManagementBackgroundWorker : BackgroundService
    {
        private readonly ILogger<PortfolioManagementBackgroundWorker> _logger;
        private readonly PortfolioManagementService _portfolioManagementService;
        private readonly ActionBlock<object> _portfolioManagerBlock;

        /// <summary>
        /// Конструктор с внедрением зависимостей.
        /// </summary>
        /// <param name="logger">Логгер сервиса.</param>
        /// <param name="portfolioManagementService">Бизнес-логика управления портфелем.</param>
        /// <param name="portfolioManagerBlock">Блок обработки данных из TPL Dataflow.</param>
        public PortfolioManagementBackgroundWorker(
            ILogger<PortfolioManagementBackgroundWorker> logger,
            PortfolioManagementService portfolioManagementService,
            ActionBlock<object> portfolioManagerBlock)
        {
            _logger = logger;
            _portfolioManagementService = portfolioManagementService;
            _portfolioManagerBlock = portfolioManagerBlock;
        }

        /// <summary>
        /// Основной метод выполнения фонового сервиса.
        /// Запускается при старте приложения.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Сервис управления портфелем запущен.");

            try
            {
                // Ожидаем завершения блока обработки.
                // Этот код будет выполняться до тех пор, пока блок не получит сигнал о завершении (.Complete())
                await _portfolioManagerBlock.Completion;
            }
            catch (Exception ex)
            {
                // Логируем ошибку, если она произошла в цепочке блоков Dataflow
                _logger.LogError(ex, "Ошибка в сервисе управления портфелем.");
                throw;
            }

            _logger.LogInformation("Сервис управления портфелем остановлен.");
        }
    }
}

