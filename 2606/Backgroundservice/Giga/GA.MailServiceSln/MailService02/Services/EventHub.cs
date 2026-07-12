using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

public class EventHub : BackgroundService
{
    private readonly ChannelReader<string> _fromA;
    private readonly ChannelWriter<string> _toA;
    private readonly ChannelReader<string> _fromB;
    private readonly ChannelWriter<string> _toB;

    public EventHub(AToHubPipe aIn, HubToAPipe aOut, BToHubPipe bIn, HubToBPipe bOut)
    {
        _fromA = aIn.Reader;
        _toA = aOut.Writer;
        _fromB = bIn.Reader;
        _toB = bOut.Writer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_fromA.TryRead(out var msgA))
                {
                    Console.WriteLine($"[HUB] Relay A -> B: {msgA}");
                    await _toB.WriteAsync(msgA, stoppingToken);
                }
                if (_fromB.TryRead(out var msgB))
                {
                    Console.WriteLine($"[HUB] Relay B -> A: {msgB}");
                    await _toA.WriteAsync(msgB, stoppingToken);
                }
                await Task.Delay(10, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[HUB] Graceful shutdown: Cancellation requested.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HUB] Unexpected error: {ex.Message}");
        }
    }
}

