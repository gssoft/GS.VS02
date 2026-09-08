using BusLibrary;

namespace BusLibrary.Tests;

public record TestMessage : MessageBase
{
    public TestMessage() : base("test:message") { }
}

public record AnotherTestMessage : MessageBase
{
    public AnotherTestMessage() : base("another:message") { }
}
