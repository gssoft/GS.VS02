using System.Threading;
using System.Threading.Tasks;

// Определяем токен отмены
var cancellationSource = new CancellationTokenSource();

// Основной метод, использующий токен
async Task SomeMethod(CancellationToken token)
{
    try
    {
        // Используем токен отмены в Task.Delay
        await Task.Delay(1000, token);

        // Код выполняется только если задержка успешно завершилась
        Console.WriteLine("Задержка выполнена");
    }
    catch (OperationCanceledException)
    {
        // Обрабатываем случай отмены операции
        Console.WriteLine("Операция была отменена");
    }
    finally
    {
        // Выполняется независимо от результата операции
        Console.WriteLine("Завершение метода");
    }
}

// Главный метод для демонстрации отмены
public static void Main()
{
    var task = SomeMethod(cancellationSource.Token);

    // Через полсекунды инициируем отмену
    Thread.Sleep(5000);
    cancellationSource.Cancel(); // Отправляем сигнал отмены

    task.Wait(); // Ждем завершение задачи
}


