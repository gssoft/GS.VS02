using System.Threading.Channels;

public sealed class HubToBPipe
{
    public readonly ChannelWriter<string> Writer;
    public readonly ChannelReader<string> Reader;

    public HubToBPipe(ChannelWriter<string> w, ChannelReader<string> r)
    {
        Writer = w;
        Reader = r;
    }
}
