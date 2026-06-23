// Program.cs

using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using System.Text;

var rootBlock = new FractalBlock<IApplicationEvent>("root");

var producerBlock = new FractalBlock<IApplicationEvent>("producer-1", rootBlock);
var routerBlock = new FractalBlock<IApplicationEvent>("router-1", rootBlock);
var consumerBlock = new FractalBlock<IApplicationEvent>("consumer-1");

// Фрактальная связь
routerBlock.LinkTo(consumerBlock);

// Регистрация обработчиков
consumerBlock.Subscribe<FractalEvent>(async e => {
    Console.WriteLine($"Consumer processing: {e.EventType}");
});

producerBlock.Subscribe<FractalEvent>(async e => {
    Console.WriteLine($"Producer received: {e.EventType}");
});

// Запуск
_ = Task.Run(async () => {
    while (true)
    {
        var evt = new FractalEvent(Guid.NewGuid().ToString(), DateTime.UtcNow,
            "producer-1", "consumer-1", "DataGenerated", new { Data = "test" });
        await producerBlock.SendAsync(evt);
        await Task.Delay(1000);
    }
});
