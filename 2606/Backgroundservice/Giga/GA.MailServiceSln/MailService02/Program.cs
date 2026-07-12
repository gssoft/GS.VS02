using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Channels;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Создаем физические каналы
                var aToHubChan = Channel.CreateUnbounded<string>();
                var hubToAChan = Channel.CreateUnbounded<string>();
                var bToHubChan = Channel.CreateUnbounded<string>();
                var hubToBChan = Channel.CreateUnbounded<string>();

                // РЕГИСТРАЦИЯ УНИКАЛЬНЫХ ТИПОВ (Больше нет конфликта!)
                services.AddSingleton(new AToHubPipe(aToHubChan.Writer, aToHubChan.Reader));
                services.AddSingleton(new HubToAPipe(hubToAChan.Writer, hubToAChan.Reader));

                services.AddSingleton(new BToHubPipe(bToHubChan.Writer, bToHubChan.Reader));
                services.AddSingleton(new HubToBPipe(hubToBChan.Writer, hubToBChan.Reader));

                // Регистрируем воркеры
                services.AddHostedService<EventHub>();
                services.AddHostedService<ClientA>();
                services.AddHostedService<ClientB>();
            })
            .Build();

        await host.RunAsync();
    }
}
