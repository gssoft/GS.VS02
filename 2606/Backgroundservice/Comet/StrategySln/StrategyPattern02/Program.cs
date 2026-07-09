using System;
using System.Text.RegularExpressions;

/// <summary>
/// Контекст: класс, который выполняет работу.
/// Он не знает, КАК именно будет преобразована строка, он просто вызывает делегат.
/// </summary>
public class TextProcessor
{
    private readonly Func<string, string> _transform;

    public TextProcessor(Func<string, string> transform)
    {
        // Защита от передачи пустой функции
        _transform = transform ?? throw new ArgumentNullException(nameof(transform));
    }

    /// <summary>
    /// Выполняет обработку входной строки через зашитую стратегию.
    /// </summary>
    public string Process(string input)
    {
        Console.WriteLine($"[TextProcessor] Применяем трансформацию к строке: \"{input}\"");
        return _transform(input);
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("Демонстрация Стратегии на основе делегатов (Func<,>)\n");

        const string sampleInput = "Привет, Мир! Паттерн Стратегия C#!";

        // 1. СТРАТЕГИЯ №1: Перевод в верхний регистр
        // Мы создаем экземпляр процессора и "впечатываем" в него алгоритм s => s.ToUpper()
        var upperProcessor = new TextProcessor(s => s.ToUpper());

        string resultUpper = upperProcessor.Process(sampleInput);
        Console.WriteLine($"Результат: {resultUpper}\n");


        // 2. СТРАТЕГИЯ №2: Перевод в нижний регистр
        // Тот же самый класс TextProcessor, но поведение изменено под капотом
        var lowerProcessor = new TextProcessor(s => s.ToLower());

        string resultLower = lowerProcessor.Process(sampleInput);
        Console.WriteLine($"Результат: {resultLower}\n");


        // 3. СТРАТЕГИЯ №3: Сложная логика — удаление всех гласных
        // Здесь стратегия — это полноценный блок кода внутри лямбды
        var vowelRemover = new TextProcessor(RemoveVowels);
        // Или вариант через анонимную лямбду:
        // var vowelRemover = new TextProcessor(s => Regex.Replace(s, "[АаЕеЁёИиОоУуЫыЭэЮюЯя]", ""));

        string resultNoVowels = vowelRemover.Process(sampleInput);
        Console.WriteLine($"Результат: {resultNoVowels}\n");


        // 4. ДИНАМИЧЕСКАЯ СМЕНА СТРАТЕГИИ В RUNTIME
        Console.WriteLine("--- Демонстрация смены стратегии во время выполнения ---");

        // Создаем процессор с одной логикой по умолчанию
        TextProcessor dynamicProcessor = new TextProcessor(s => $"[Default]: {s}");
        Console.WriteLine(dynamicProcessor.Process("Тестовое сообщение"));

        // Меняем поведение без пересоздания объекта или наследования.
        // В реальном приложении здесь могла бы быть проверка config-файла или выбор пользователя.
        bool shouldReverse = true;

        if (shouldReverse)
        {
            // Переключаем "мозг" объекта на другую функцию
            typeof(TextProcessor)
                .GetField("_transform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .SetValue(dynamicProcessor, new Func<string, string>(ReverseString));

            // ВАЖНО: Прямое изменение приватных полей через рефлексию — это хак для демонстрации.
            // Правильный способ динамической смены — внедрить фабрику или DI-контейнер.
            Console.WriteLine("\n[Смена контекста] Стратегия обновлена на 'Реверс':");
        }

        Console.WriteLine(dynamicProcessor.Process("Динамическая смена"));

        Console.WriteLine("\nНажмите любую клавишу для выхода...");
        Console.ReadKey();
    }

    #region Вспомогательные методы-алгоритмы (наши стратегии)

    /// <summary>
    /// Алгоритм удаления русских и английских гласных букв.
    /// </summary>
    private static string RemoveVowels(string input)
    {
        char[] vowels = { 'a', 'e', 'i', 'o', 'u', 'y', 'A', 'E', 'I', 'O', 'U', 'Y',
                          'а', 'е', 'ё', 'и', 'о', 'у', 'ы', 'э', 'ю', 'я', 'А', 'Е', 'Ё', 'И', 'О', 'У', 'Ы', 'Э', 'Ю', 'Я' };
        var result = input.Where(c => !vowels.Contains(c)).ToArray();
        return new string(result);
    }

    /// <summary>
    /// Алгоритм разворота строки задом наперед.
    /// </summary>
    private static string ReverseString(string input)
    {
        char[] chars = input.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    #endregion
}
