// Services/UiUpdateService.cs

using System;
using TradingTerminal.Models; // Используем модели Quote и Trade
using Microsoft.Extensions.Logging; // Для логирования действий UI-сервиса

namespace TradingTerminal.Services
{
    /// <summary>
    /// Сервис, инкапсулирующий логику обновления пользовательского интерфейса.
    /// Принимает рыночные данные (Quote, Trade) и формирует из них сообщения для вывода.
    /// </олнечает только отвественность за представление данных, не хранит состояние.>
    /// </summary>
    public class UiUpdateService
    {
        private readonly ILogger<UiUpdateService> _logger;

        /// <summary>
        /// Инициализирует новый экземпляр класса UiUpdateService.
        /// </summary>
        /// <param name="logger">Интерфейс логгера для записи информации.</param>
        public UiUpdateService(ILogger<UiUpdateService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Обрабатывает входящий объект данных и выводит информацию в консоль.
        /// </summary>
        /// <param name="data">Объект данных, который может быть типа Quote или Trade.</param>
        public Task ProcessDataAsync(object data)
        {
            // Используем паттерн сопоставления с образцом (switch expression)
            // для определения типа объекта и формирования соответствующего сообщения.
            string message = data switch
            {
                Quote q => $"[UI] Котировка: {q.Symbol} @ {q.Price:C2}",
                Trade t => $"[UI] Сделка: {t.Volume} лотов @ {t.Price:C2}",
                _ => "[UI] Неизвестный тип данных"
            };

            // Выводим сформированное сообщение через логгер.
            // Это позволяет централизовать вывод и при необходимости перенаправить его.
            // Console.WriteLine(message);
            _logger.LogInformation(message);

            return Task.CompletedTask;
        }
    }
}