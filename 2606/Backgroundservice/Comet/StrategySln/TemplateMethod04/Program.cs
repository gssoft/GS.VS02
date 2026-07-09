using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Абстрактный класс-скелет конвейера обработки данных.
/// T — тип источника (строка с содержимым файла).
/// </summary>
public abstract class DataPipeline<T>
{
    public void Run()
    {
        Console.WriteLine("=== Запуск конвейера обработки ===");

        var source = LoadSource();
        if (source == null)
        {
            Console.WriteLine("Загрузка источника вернула пустое значение. Работа прервана.");
            return;
        }

        var items = Parse(source);
        if (!items.Any())
        {
            Console.WriteLine("Предупреждение: после парсинга не получено ни одного элемента.");
        }

        items = Transform(items);
        Save(items);

        Console.WriteLine("=== Конвейер успешно завершил работу ===\n");
    }

    protected abstract T LoadSource();
    protected abstract IEnumerable<string> Parse(T source);

    // Optional шаг (хук). По умолчанию возвращает данные без изменений.
    protected virtual IEnumerable<string> Transform(IEnumerable<string> items) => items;

    protected abstract void Save(IEnumerable<string> items);
}

/// <summary>
/// Конкретная реализация для обработки текстовых файлов.
/// </summary>
public class FilePipeline : DataPipeline<string>
{
    private readonly string _inputPath;
    private readonly string _outputPath;

    public FilePipeline(string inputPath, string outputPath)
    {
        _inputPath = inputPath;
        _outputPath = outputPath;
    }

    protected override string LoadSource()
    {
        try
        {
            Console.WriteLine($"[Шаг 1/4] Загрузка данных из файла '{_inputPath}'...");

            // Проверяем существование перед чтением
            if (!File.Exists(_inputPath))
            {
                throw new FileNotFoundException($"Файл {_inputPath} не найден.");
            }

            return File.ReadAllText(_inputPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при чтении файла: {ex.Message}");
            return null!;
        }
    }

    protected override IEnumerable<string> Parse(string source)
    {
        Console.WriteLine("[Шаг 2/4] Парсинг содержимого на строки...");
        return source.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(line => line.Trim());
    }

    protected override void Save(IEnumerable<string> items)
    {
        try
        {
            Console.WriteLine($"[Шаг 4/4] Сохранение результата в файл '{_outputPath}'...");

            // *** ИСПРАВЛЕНИЕ ОШИБКИ ***
            // Получаем ПОЛНЫЙ путь к директории выходного файла.
            // Path.GetDirectoryName может вернуть null для простых имен вроде "file.txt",
            // поэтому используем GetFullPath или комбинируем с директорией запуска.
            string directoryPath = Path.GetDirectoryName(Path.GetFullPath(_outputPath));

            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath); // Создаем папки, если их нет
            }

            // Используем UTF8 без BOM для совместимости
            File.WriteAllLines(_outputPath, items, System.Text.Encoding.UTF8);
            Console.WriteLine($"Сохранено {items.Count()} строк.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при записи файла: {ex.Message}");
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Определяем пути относительно папки, где лежит исполняемый файл (.exe)
        string baseDir = AppContext.BaseDirectory;
        string inputFile = Path.Combine(baseDir, "input.txt");
        string outputFile = Path.Combine(baseDir, "output.txt");

        // Проверка наличия входного файла до запуска логики
        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"Файл \"{inputFile}\" не найден. Создаю пример файла...");
            CreateSampleInputFile(inputFile);
        }

        // Создание экземпляра конкретного конвейера
        var pipeline = new FilePipeline(inputFile, outputFile);

        // Вызов шаблонного метода
        pipeline.Run();

        DisplayOutputPreview(outputFile);

        Console.WriteLine("\nНажмите любую клавишу для выхода...");
        Console.ReadKey();
    }

    /// <summary>
    /// Создает пример входного файла с русским текстом.
    /// </summary>
    private static void CreateSampleInputFile(string path)
    {
        var lines = new List<string>
        {
            "Первая строка данных",
            "Вторая строка данных",
            "Третья строка данных"
        };
        File.WriteAllLines(path, lines, System.Text.Encoding.UTF8);
        Console.WriteLine($"Создан пример файла \"{path}\".");
    }

    /// <summary>
    /// Выводит первые несколько строк созданного выходного файла в консоль.
    /// </summary>
    private static void DisplayOutputPreview(string path)
    {
        if (File.Exists(path))
        {
            Console.WriteLine("\n--- Содержимое выходного файла ---");
            var allLines = File.ReadAllLines(path);
            int previewCount = Math.Min(allLines.Length, 10);

            for (int i = 0; i < previewCount; i++)
            {
                Console.WriteLine(allLines[i]);
            }

            if (allLines.Length > 10)
            {
                Console.WriteLine($"... и еще {allLines.Length - 10} строк(и)");
            }
            Console.WriteLine("--------------------------------");
        }
        else
        {
            Console.WriteLine("\nВыходной файл не был создан.");
        }
    }
}
