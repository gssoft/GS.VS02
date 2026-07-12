using System.Threading.Channels;

public sealed class HubToAPipe
{
    public readonly ChannelWriter<string> Writer;
    public readonly ChannelReader<string> Reader;

    public HubToAPipe(ChannelWriter<string> w, ChannelReader<string> r)
    {
        Writer = w;
        Reader = r;
    }
}

