// Messages/SomeEvent.cs
record SomeEvent(string EventType, DateTime OccurredAt) : IEvent;
