var bus = new Messaging.ChannelsBus<string>(
    onSyncTopicFault: (k, ex) => Console.WriteLine($"SYNC topic '{k}' fault: {ex.Message}"),
    onAsyncTopicFault: (k, ex) => Console.WriteLine($"ASYNC topic '{k}' fault: {ex.Message}")
);

bus.Sync.Subscribe<int>("SyncTopic", x => Console.WriteLine($"sync {x}"));
bus.Async.Subscribe<int>("AsyncTopic", async x => { await Task.Delay(100); Console.WriteLine($"async {x}"); });

bus.Sync.Publish("SyncTopic", 123);
await bus.Async.PublishAsync("AsyncTopic", 456);
