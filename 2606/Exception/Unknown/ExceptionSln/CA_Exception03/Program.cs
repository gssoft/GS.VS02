// TopLevel statements

// Для этого файла не нужен namespace и явный класс Program.
// Точка входа генерируется компилятором автоматически.

// Асинхронный обработчик
async Task SomeMethodAsync()
{
    try
    {
        await Task.Delay(1000);
        // Логика...
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Возникла ошибка: {ex.Message}");
        // throw;
    }
    finally 
    {
        Console.WriteLine($"Приехали ...");
    }
}

// Точка входа приложения
await SomeMethodAsync();
Console.WriteLine("Hello, World!");


