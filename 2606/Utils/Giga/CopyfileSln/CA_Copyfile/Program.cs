using System;
using System.Collections.Generic;
using System.IO;

class Program
{
    // Строка-разделитель между содержимым разных файлов
    private const string Separator = "-----------------------------------------------------------------";

    static void Main(string[] args)
    {
        try
        {
            // Получаем путь к текущему рабочему каталогу
            string currentDir = Directory.GetCurrentDirectory();

            // Имя выходного файла совпадает с названием текущего каталога
            string dirName = new DirectoryInfo(currentDir).Name;
            string outputFilePath = Path.Combine(currentDir, dirName + ".txt");

            using (StreamWriter outfile = new StreamWriter(outputFilePath))
            {
                WalkAndCollect(currentDir, currentDir, outfile);
            }

            Console.WriteLine($"Сборка завершена. Результат сохранён в файл: {outputFilePath}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Ошибка при обработке: " + ex.Message);
        }
    }

    /// <summary>
    /// Рекурсивно обходит все подкаталоги и обрабатывает файлы с разрешёнными расширениями.
    /// </summary>
    private static void WalkAndCollect(string rootDir, string currentDir, TextWriter outfile)
    {
        var allowedExtensions = GetAllowedExtensions(); // Список расширений для обработки

        foreach (string filePath in Directory.EnumerateFiles(currentDir))
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                continue; // Пропускаем файлы с неразрешёнными расширениями

            string relPath = Path.GetRelativePath(rootDir, filePath);
            try
            {
                outfile.WriteLine($"--- {relPath} ---");

                // Читаем весь файл как текст (UTF-8)
                string content = File.ReadAllText(filePath);
                outfile.Write(content);

                // Если контент не заканчивается переводом строки — добавляем его,
                // чтобы следующий разделитель был на новой строке
                if (!content.EndsWith(Environment.NewLine) && !string.IsNullOrEmpty(content))
                    outfile.Write(Environment.NewLine);

                // Разделитель между файлами
                outfile.WriteLine(Separator);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Ошибка обработки файла '{filePath}': {e.Message}");
            }
        }

        // Рекурсивный обход вложенных каталогов
        foreach (string subdir in Directory.EnumerateDirectories(currentDir))
        {
            WalkAndCollect(rootDir, subdir, outfile);
        }
    }

    /// <summary>
    /// Возвращает список расширений файлов, которые будут обработаны.
    /// Добавляйте сюда любые нужные вам расширения через запятую.
    /// </summary>
    private static List<string> GetAllowedExtensions()
    {
        return new List<string>
        {
            ".cs",   // Исходный код C#
            ".txt",  // Текстовые файлы
            ".json", // JSON-файлы
            ".xml",  // XML-файлы
            ".html", // HTML
            ".css",  // CSS-стили
            ".js",   // JavaScript
            ".config", // Файлы конфигурации
            ".md"    // Markdown
            // При необходимости добавьте свои расширения здесь
        };
    }
}



