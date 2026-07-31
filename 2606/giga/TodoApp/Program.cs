using System.Text.Json;
using System.Text.Json.Serialization;

// Модель задачи
public class TodoItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
}

class Program
{
    private const string FileName = "tasks.json";

    static void Main(string[] args)
    {
        var tasks = LoadTasks();
        
        Console.WriteLine("Добро пожаловать в Менеджер задач!");
        Console.WriteLine("Команды: add | list | complete | save | exit");

        while (true)
        {
            Console.Write("\n> ");
            var command = Console.ReadLine()?.ToLower();

            switch (command)
            {
                case "add":
                    AddTask(tasks);
                    break;
                case "list":
                    ListTasks(tasks);
                    break;
                case "complete":
                    CompleteTask(tasks);
                    break;
                case "save":
                    SaveTasks(tasks);
                    break;
                case "exit":
                    SaveTasks(tasks); 
                    return;
                default:
                    Console.WriteLine("Неизвестная команда.");
                    break;
            }
        }
    }

    private static void AddTask(List<TodoItem> tasks)
    {
        Console.Write("Введите название задачи: ");
        var title = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(title))
        {
            tasks.Add(new TodoItem { Id = Guid.NewGuid(), Title = title, CreatedAt = DateTime.Now });
            Console.WriteLine("Задача добавлена.");
        }
    }

    private static void ListTasks(List<TodoItem> tasks)
    {
        if (!tasks.Any())
        {
            Console.WriteLine("Список пуст.");
            return;
        }

        foreach (var task in tasks)
        {
            var status = task.IsCompleted ? "[Выполнено]" : "[В работе]";
            Console.WriteLine($"{task.Id}: {status} {task.Title}");
        }
    }

    private static void CompleteTask(List<TodoItem> tasks)
    {
        Console.Write("ID задачи для выполнения: ");
        if (Guid.TryParse(Console.ReadLine(), out var id))
        {
            var task = tasks.FirstOrDefault(t => t.Id == id);
            if (task != null && !task.IsCompleted)
            {
                task.IsCompleted = true;
                Console.WriteLine("Отмечено как выполненное.");
            }
            else
            {
                Console.WriteLine("Задача не найдена или уже выполнена.");
            }
        }
        else
        {
            Console.WriteLine("Неверный формат ID.");
        }
    }

    private static void SaveTasks(List<TodoItem> tasks)
    {
        try
        {
            var json = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FileName, json);
            Console.WriteLine($"Сохранено в {FileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка сохранения: {ex.Message}");
        }
    }

    private static List<TodoItem> LoadTasks()
    {
        if (!File.Exists(FileName)) return new List<TodoItem>();
        
        try
        {
            var json = File.ReadAllText(FileName);
            return JsonSerializer.Deserialize<List<TodoItem>>(json) ?? new List<TodoItem>();
        }
        catch
        {
            return new List<TodoItem>();
        }
    }
}

