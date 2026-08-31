using System.Text.Json;
using Trading.Core;
using Trading.Storage;
using Microsoft.Extensions.Logging;

namespace Trading.App;

public static class ManifestLoader
{
    public static TradingManifest Load(string path)
    {
        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<TradingManifest>(json, options)
               ?? throw new InvalidOperationException("Failed to deserialize manifest.");
    }

    public static List<object> CreateProcessors(
    TradingManifest manifest,
    IMicroEventBus bus,
    ILoggerFactory loggerFactory,
    InMemoryDatabase db,
    IConfiguration appConfig)
    {
        var processors = new List<object>();
        foreach (var proc in manifest.Processors)
        {
            var instance = ManifestProcessorFactory.CreateProcessor(
                proc.Type, bus, loggerFactory, db, proc.Config, appConfig);
            if (instance != null)
                processors.Add(instance);
        }
        return processors;
    }

    //public static List<object> CreateProcessors(
    //    TradingManifest manifest,
    //    IMicroEventBus bus,
    //    ILoggerFactory loggerFactory,
    //    InMemoryDatabase db)
    //{
    //    var processors = new List<object>();
    //    foreach (var proc in manifest.Processors)
    //    {
    //        var instance = ManifestProcessorFactory.CreateProcessor(proc.Type, bus, loggerFactory, db, proc.Config);
    //        processors.Add(instance);
    //    }
    //    return processors;
    //}
}
