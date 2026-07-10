namespace Worker.Core.Abstractions;

public interface ILivenessStrategy
{
    void Pulse();
}
