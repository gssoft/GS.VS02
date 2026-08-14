using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

class FileSplitterByLines
{
    public static void SplitFileByLines(string inputFile, int parts)
    {
        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"Файл '{inputFile}' не найден.");
            return;
        }

        if (parts < 1)
        {
            Console.WriteLine("Количество частей должно быть >= 1.");
            return;
        }

        // Читаем все строки файла
        string[] lines = File.ReadAllLines(inputFile, Encoding.UTF8);
        int totalLines = lines.Length;

        Console.WriteLine($"Всего строк: {totalLines}");

        // Сколько строк в одной части
        int linesPerPart = totalLines / parts;
        if (linesPerPart == 0) linesPerPart = 1;

        for (int i = 0; i < parts; i++)
        {
            int start = i * linesPerPart;
            int end = (i == parts - 1)
                ? totalLines
                : Math.Min(start + linesPerPart, totalLines);

            string partName = $"{inputFile}_part{i + 1}.txt";

            using (StreamWriter writer = new StreamWriter(partName, false, Encoding.UTF8))
            {
                for (int j = start; j < end; j++)
                {
                    writer.WriteLine(lines[j]);
                }
            }

            Console.WriteLine($"Создан файл: {partName} (строки {start + 1}–{end})");
        }

        Console.WriteLine("Готово.");
    }

    static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Использование: FileSplitterByLines <filename> <N_parts>");
            return;
        }

        string file = args[0];
        int parts = int.Parse(args[1]);

        SplitFileByLines(file, parts);
    }
}

