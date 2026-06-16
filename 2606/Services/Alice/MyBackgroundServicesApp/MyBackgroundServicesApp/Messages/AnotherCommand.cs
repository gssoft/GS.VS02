// Messages/AnotherCommand.cs
record AnotherCommand(Guid CorrelationId, DateTime Timestamp) : ICommand;
