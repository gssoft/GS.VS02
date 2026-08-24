using MassTransit;
using MicroPlatform.Core.Events;
using MicroPlatform.MassTransitHost.Consumers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<OrderProcessor>();
            x.AddConsumer<InventoryProcessor>();

            x.UsingInMemory((context, cfg) =>
            {
                // ConfigureEndpoints creates one receive endpoint per registered consumer
                // and subscribes it to the message types that consumer's Consume methods
                // accept - this is what makes Publish() below reach both consumers without
                // any manual wiring.
                cfg.ConfigureEndpoints(context);
            });

            // --- Production swap: RabbitMQ instead of in-memory ---
            // x.UsingRabbitMq((context, cfg) =>
            // {
            //     cfg.Host("localhost", "/", h =>
            //     {
            //         h.Username("guest");
            //         h.Password("guest");
            //     });
            //     cfg.UseMessageRetry(r => r.Interval(5, TimeSpan.FromSeconds(2)));
            //     cfg.ConfigureEndpoints(context);
            // });
            // UseMessageRetry retries a failed Consume automatically; once retries are
            // exhausted, MassTransit auto-creates and routes to a "_error" queue per
            // receive endpoint - no hand-rolled x-retry-count headers or manual DLQ
            // wiring needed, unlike the RabbitMQ.Client adapter from earlier in this
            // conversation. Reconnection on a dropped broker connection is likewise
            // handled internally, not something we write ourselves.
        });
    })
    .Build();

// IHostedService (which starts/stops the MassTransit bus) only runs between StartAsync
// and StopAsync, so a one-shot console demo drives that explicitly instead of calling
// host.Run(), which would block forever waiting for Ctrl+C.
await host.StartAsync();

var publishEndpoint = host.Services.GetRequiredService<IPublishEndpoint>();

var orderId = Guid.NewGuid();
Console.WriteLine($"Publishing OrderPaid for order {orderId}...");
await publishEndpoint.Publish(new OrderPaid(orderId, 150.00m));

// Consumers run on background bus workers, not synchronously like our earlier
// InMemoryMicroEventBus.PublishAsync - so the demo waits a moment before exiting.
// The test project uses the test harness's awaitable Consumed/Published collections
// instead of a fixed delay - see MicroPlatform.MassTransit.Tests.
await Task.Delay(TimeSpan.FromMilliseconds(500));

await host.StopAsync();

Console.WriteLine("Done.");
