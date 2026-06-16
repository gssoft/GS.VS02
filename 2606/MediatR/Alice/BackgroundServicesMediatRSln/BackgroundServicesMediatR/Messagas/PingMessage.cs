using MediatR;

public record PingMessage(int Counter) : INotification;


