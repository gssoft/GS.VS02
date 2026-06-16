namespace CA_Exception02
{
    internal class Program
    {

        public static async Task SomeMethodAsync()
        {
            try
            {
                await Task.Delay(1000); // задержка на 1 секунду

                // Здесь можете разместить любую дополнительную логику
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Возникла ошибка: {ex.Message}");
                throw; // бросаем исключение дальше вверх по стеку, если нужно распространять ошибку
            }
        }

        static async Task Main(string[] args)
        {
            await SomeMethodAsync();

            Console.WriteLine("Hello, World!");
        }
    }
}
