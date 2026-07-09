using System;

// 1. Базовый класс: разрешаем переопределение основного алгоритма
public abstract class Processor
{
    public virtual void Process()
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

    protected virtual void Prepare()
    {
        Console.WriteLine("[Базовая подготовка] Инициализация ресурсов...");
    }

    protected abstract long LoadData();

    protected virtual void Transform(long data)
    {
        Console.WriteLine($"[Базовая трансформация] Применяем стандартный фильтр к {data}...");
    }

    protected abstract void SaveData(long data);

    protected virtual void Cleanup()
    {
        Console.WriteLine("[Базовая очистка] Освобождаем память...");
    }
}

// 2. Промежуточный класс: делаем его АБСТРАКТНЫМ, чтобы разрешить наследование,
// но "запечатываем" метод Process, чтобы никто ниже не мог его изменить.
public abstract class SealedProcessor : Processor
{
    // sealed override запрещает FileProcessor переопределять этот метод
    public sealed override void Process()
    {
        base.Process();
    }

    // Эти методы ОБЯЗАНЫ остаться абстрактными, иначе дочерний класс 
    // (FileProcessor) не сможет их реализовать.
    protected override abstract long LoadData();
    protected override abstract void SaveData(long data);
}

// 3. Конкретная реализация
public class FileProcessor : SealedProcessor
{
    // Здесь мы реализуем только детали (шаги). Вызов Process() придет от родителя.

    protected override long LoadData()
    {
        Console.WriteLine("[FileProcessor] Чтение данных из файла 'report.txt'...");
        return 123;
    }

    protected override void SaveData(long data)
    {
        Console.WriteLine($"[FileProcessor] Запись результата ({data}) обратно в файл...");
    }

    protected override void Transform(long data)
    {
        Console.WriteLine($"[FileProcessor] Специфичная трансформация: конвертируем данные {data} в формат CSV.");
    }

    protected override void Prepare()
    {
        base.Prepare();
        Console.WriteLine("[FileProcessor] Проверка прав доступа к диску...");
    }

    protected override void Cleanup()
    {
        Console.WriteLine("[FileProcessor] Закрытие дескрипторов файла...");
        base.Cleanup();
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Демонстрация паттерна 'Шаблонный метод'\n");

        // Создаем экземпляр конкретного класса через ссылку на базу
        Processor processor = new FileProcessor();
        processor.Process();

        Console.WriteLine("Нажмите любую клавишу для выхода...");
        Console.ReadKey();
    }
}