using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json; // Для работы с JSON

#region Модели данных и Стратегии

/// <summary>
/// Модель данных для генерации отчета.
/// </summary>
public class DataItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

/// <summary>
/// Интерфейс стратегии. Определяет контракт для всех форматов отчетов.
/// </summary>
public interface IReportStrategy
{
    string Generate(IEnumerable<DataItem> data);
}

/// <summary>
/// Конкретная стратегия: Генерация отчета в формате CSV.
/// </summary>
public class CsvReportStrategy : IReportStrategy
{
    public string Generate(IEnumerable<DataItem> data)
    {
        var lines = new List<string>();

        // Добавляем заголовок
        lines.Add("Id,Name,Price");

        // Добавляем данные
        foreach (var item in data)
        {
            // Экранируем запятые внутри названий, если они есть
            string safeName = item.Name.Contains(',') ? $"\"{item.Name}\"" : item.Name;
            lines.Add($"{item.Id},{safeName},{item.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}

/// <summary>
/// Конкретная стратегия: Генерация отчета в формате JSON.
/// Используем встроенный системный сериализатор .NET.
/// </summary>
public class JsonReportStrategy : IReportStrategy
{
    public string Generate(IEnumerable<DataItem> data)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(data, options);
    }
}

/// <summary>
/// Конкретная стратегия: Генерация простого текстового отчета (Plain Text).
/// Демонстрирует расширяемость паттерна.
/// </summary>
public class PlainTextReportStrategy : IReportStrategy
{
    public string Generate(IEnumerable<DataItem> data)
    {
        var report = new List<string>
        {
            "ОТЧЕТ ПО ТОВАРАМ",
            "================"
        };

        foreach (var item in data)
        {
            report.Add($"ID: {item.Id} | Название: {item.Name} | Цена: {item.Price:C}");
        }

        report.Add("================");
        report.Add($"Итого позиций: {data.Count()}");

        return string.Join(Environment.NewLine, report);
    }
}

#endregion

#region Сервис контекста

/// <summary>
/// Контекст (Сервис), который использует стратегию.
/// Ему неважно, какой это формат — он работает только с интерфейсом.
/// </summary>
public class ReportService
{
    private readonly IReportStrategy _strategy;

    public ReportService(IReportStrategy strategy)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    public string CreateReport(IEnumerable<DataItem> data)
    {
        Console.WriteLine($"\n[ReportService] Вызываем генерацию через стратегию {_strategy.GetType().Name}...");
        return _strategy.Generate(data);
    }
}

#endregion

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("Демонстрация паттерна 'Стратегия' (Strategy Pattern)\n");

        // 1. Подготовка тестовых данных
        var sampleData = GetSampleData();

        // 2. Создаем контекст (сервис) со стратегией CSV
        IReportStrategy csvStrategy = new CsvReportStrategy();
        var reportServiceCsv = new ReportService(csvStrategy);

        string csvResult = reportServiceCsv.CreateReport(sampleData);
        Console.WriteLine("\n--- Результат CSV ---");
        Console.WriteLine(csvResult);

        // 3. Меняем стратегию на лету (в DI-контейнерах это делается одной строчкой в настройках)
        IReportStrategy jsonStrategy = new JsonReportStrategy();
        var reportServiceJson = new ReportService(jsonStrategy);

        string jsonResult = reportServiceJson.CreateReport(sampleData);
        Console.WriteLine("\n--- Результат JSON ---");
        Console.WriteLine(jsonResult);

        // 4. Пример использования третьей стратегии
        IReportStrategy textStrategy = new PlainTextReportStrategy();
        var reportServiceText = new ReportService(textStrategy);

        string textResult = reportServiceText.CreateReport(sampleData);
        Console.WriteLine("\n--- Результат TXT ---");
        Console.WriteLine(textResult);

        Console.WriteLine("\nНажмите любую клавишу для выхода...");
        Console.ReadKey();
    }

    /// <summary>
    /// Метод для создания набора демонстрационных данных.
    /// </summary>
    private static IEnumerable<DataItem> GetSampleData()
    {
        return new List<DataItem>
        {
            new DataItem { Id = 1, Name = "Ноутбук Pro", Price = 150000 },
            new DataItem { Id = 2, Name = "Мышь беспроводная", Price = 2500 },
            new DataItem { Id = 3, Name = "Клавиатура механическая", Price = 8900 }
        };
    }
}
