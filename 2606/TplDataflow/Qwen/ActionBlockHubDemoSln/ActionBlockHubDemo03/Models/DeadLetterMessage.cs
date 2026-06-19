namespace ActionBlockHubDemo.Models
{
    // Универсальный класс для хранения упавшего сообщения
    public class DeadLetterMessage<TMessage>
    {
        public required TMessage OriginalMessage { get; init; }
        public required string Key { get; init; }
        public required string ErrorMessage { get; init; }
        public required string StackTrace { get; init; }
        public DateTime FailedAt { get; init; } = DateTime.UtcNow;

        public override string ToString()
        {
            return $"[{FailedAt:HH:mm:ss}] Key: {Key} | Error: {ErrorMessage} | Message: {OriginalMessage}";
        }
    }
}

