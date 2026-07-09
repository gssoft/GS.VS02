using System;

/// <summary>
/// Абстрактный класс-скелет (Abstract Class).
/// Определяет неизменяемый алгоритм (шаблонный метод) и его шаги.
/// </summary>
public abstract class Processor
{
    // Шаблонный метод. 
    // Так как он НЕ помечен ключевым словом virtual,
    // ни один дочерний класс не сможет его переопределить.
    // Это гарантирует целостность структуры алгоритма.
    public void Process()
    {
        Console.WriteLine("=== Начало процесса обработки ===");

        Prepare();
        var data = LoadData();

        if (data > 0)
        {
            Transform(data);
            SaveData(data);
        }

        Cleanup();

        Console.WriteLine("=== Процесс завершен ===\n");
    }

    // Шаг с базовой реализацией (Hook). Наследники могут расширить эту логику.
    protected virtual void Prepare()
    {
        Console.WriteLine("[Базовая подготовка] Инициализация общих ресурсов...");
    }

    // Обязательный шаг (Primitive Operation). 
    // Дочерние классы ОБЯЗАНЫ предоставить свою реализацию загрузки данных.
    protected abstract long LoadData();

    // Optional шаг с реализацией по умолчанию.
    protected virtual void Transform(long data)
    {
        Console.WriteLine($"[Базовая трансформация] Применяем стандартный фильтр к значению {data}...");
    }

    // Еще один обязательный шаг для сохранения результата.
    protected abstract void SaveData(long data);

    // Шаг очистки с базовой реализацией.
    protected virtual void Cleanup()
    {
        Console.WriteLine("[Базовая очистка] Освобождаем общие ресурсы...");
    }
}

/// <summary>
/// Конкретная реализация для работы с файлами.
/// Реализует только абстрактные шаги базового класса.
/// </summary>
public class FileProcessor : Processor
{
    protected override long LoadData()
    {
        Console.WriteLine("[FileProcessor] Чтение данных из файла 'report.txt'...");
        return 123; // Эмулируем ID или размер прочитанных данных
    }

    protected override void SaveData(long data)
    {
        Console.WriteLine($"[FileProcessor] Запись результата ({data}) обратно в файл на диске...");
    }

    // Переопределяем optional-метод, так как для файлов нужна специфичная обработка
    protected override void Transform(long data)
    {
        Console.WriteLine($"[FileProcessor] Специфичная трансформация: конвертируем данные {data} в формат CSV.");
    }

    // Расширяем хук подготовки своими действиями
    protected override void Prepare()
    {
        base.Prepare(); // Выполняем общую логику родителя
        Console.WriteLine("[FileProcessor] Проверка прав доступа к папке с файлами...");
    }

    protected override void Cleanup()
    {
        Console.WriteLine("[FileProcessor] Закрытие дескрипторов открытого файла...");
        base.Cleanup(); // Выполняем общую очистку
    }
}

/// <summary>
/// Вторая конкретная реализация — работа с базой данных.
/// Демонстрирует полиморфизм: использует тот же скелет, но другую внутреннюю логику.
/// </summary>
public class DatabaseProcessor : Processor
{
    protected override long LoadData()
    {
        Console.WriteLine("[DatabaseProcessor] Выполнение SQL-запроса SELECT к таблице Logs...");
        return 987654321;
    }

    protected override void SaveData(long data)
    {
        Console.WriteLine($"[DatabaseProcessor] Обновление записи в таблице Results значением {data} через ORM...");
    }

    // Для базы данных стандартная трансформация подходит, 
    // поэтому мы можем оставить реализацию родительского класса.
    // Явное указание override здесь необязательно, но добавлено для читаемости.
    protected override void Transform(long data)
    {
        base.Transform(data);
    }

    protected override void Prepare()
    {
        base.Prepare();
        Console.WriteLine("[DatabaseProcessor] Открытие соединения с БД...");
    }

    protected override void Cleanup()
    {
        Console.WriteLine("[DatabaseProcessor] Коммит транзакции и закрытие соединения...");
        base.Cleanup();
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Демонстрация паттерна 'Шаблонный метод'\n");

        // Используем массив базового типа для демонстрации полиморфизма.
        // Программа работает со списком процессоров, не зная их конкретного типа.
        Processor[] processors = new Processor[]
        {
            new FileProcessor(),
            new DatabaseProcessor()
        };

        foreach (var processor in processors)
        {
            // Вызываем ОДИН И ТОТ ЖЕ метод Process().
            // Благодаря правилам Template Method, внутри всегда выполняется:
            // Prepare -> LoadData(конкретный) -> Transform -> SaveData(конкретный) -> Cleanup
            processor.Process();
        }

        Console.WriteLine("\nОбработка всех типов данных завершена.");
        Console.WriteLine("Нажмите любую клавишу для выхода...");
        Console.ReadKey();
    }
}
