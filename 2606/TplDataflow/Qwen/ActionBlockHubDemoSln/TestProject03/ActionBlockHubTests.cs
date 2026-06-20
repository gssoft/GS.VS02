using ActionBlockHubDemo.Models;
using ActionBlockHubDemo.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading.Tasks.Dataflow;
using Xunit;

namespace ActionBlockHubDemo.Tests
{
    public class ActionBlockHubTests
    {
        private readonly Mock<ILogger<ActionBlockHub<string, MyDataType>>> _loggerMock;
        private readonly InMemoryDeadLetterQueue<MyDataType> _dlq;

        public ActionBlockHubTests()
        {
            _loggerMock = new Mock<ILogger<ActionBlockHub<string, MyDataType>>>();
            _dlq = new InMemoryDeadLetterQueue<MyDataType>(new Mock<ILogger<InMemoryDeadLetterQueue<MyDataType>>>().Object);
        }

        [Fact]
        public async Task PublishAsync_MessageProcessed_IncrementsSuccessCounter()
        {
            // 1. Arrange (Подготовка)
            var keys = new List<string> { "A" };
            var processedMessages = new List<MyDataType>();

            // Создаем простой обработчик, который просто складывает сообщения в список
            Func<string, Func<MyDataType, Task>> handlerFactory = key => async msg =>
            {
                await Task.Delay(10); // Имитация работы
                processedMessages.Add(msg);
            };

            var hub = new ActionBlockHub<string, MyDataType>(keys, handlerFactory, _loggerMock.Object, _dlq);

            var testMessage = new MyDataType { Id = 1, Key = "A", Source = "Test" };

            // 2. Act (Действие)
            await hub.PublishAsync("A", testMessage);

            // Ждем, пока блок обработает все сообщения и завершит работу
            hub.Complete();
            await hub.Completion;

            // 3. Assert (Проверка)
            processedMessages.Should().HaveCount(1);
            processedMessages[0].Id.Should().Be(1);
            hub.GetProcessedCount("A").Should().Be(1);
            hub.GetErrorCount("A").Should().Be(0);
        }

        [Fact]
        public async Task PublishAsync_HandlerThrowsException_MessageGoesToDLQ()
        {
            // 1. Arrange
            var keys = new List<string> { "B" };

            // Обработчик, который намеренно падает с ошибкой
            Func<string, Func<MyDataType, Task>> handlerFactory = key => msg =>
            {
                throw new InvalidOperationException("Тестовая ошибка!");
            };

            var hub = new ActionBlockHub<string, MyDataType>(keys, handlerFactory, _loggerMock.Object, _dlq);
            var testMessage = new MyDataType { Id = 99, Key = "B", Source = "Test" };

            // 2. Act
            await hub.PublishAsync("B", testMessage);

            hub.Complete();
            await hub.Completion;

            // 3. Assert
            hub.GetProcessedCount("B").Should().Be(0);
            hub.GetErrorCount("B").Should().Be(1);

            var deadLetters = _dlq.GetAll();
            deadLetters.Should().HaveCount(1);
            deadLetters[0].OriginalMessage.Id.Should().Be(99);
            deadLetters[0].ErrorMessage.Should().Contain("Тестовая ошибка!");
        }

        [Fact]
        public async Task PublishAsync_InvalidKey_ThrowsKeyNotFoundException() // 1. Изменили void на async Task
        {
            // 1. Arrange
            var keys = new List<string> { "A" };
            Func<string, Func<MyDataType, Task>> handlerFactory = key => msg => Task.CompletedTask;
            var hub = new ActionBlockHub<string, MyDataType>(keys, handlerFactory, _loggerMock.Object, _dlq);

            // 2. Act & Assert
            var act = async () =>
            {
                // 2. Добавили Source = "Test", чтобы удовлетворить требованию 'required'
                await hub.PublishAsync("INVALID_KEY", message: new MyDataType { Id = 1, Key = "A", Source = "Test" });
            };

            // 3. Добавили await перед Should(), чтобы корректно проверить асинхронное исключение
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }
    }
}