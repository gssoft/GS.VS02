using MediatR;

public record PongMessage(int Counter) : INotification;

