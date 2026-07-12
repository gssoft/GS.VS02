using System.Threading.Channels;

public sealed class BToHubPipe
{
    public readonly ChannelWriter<string> Writer;
    public readonly ChannelReader<string> Reader;

    public BToHubPipe(ChannelWriter<string> w, ChannelReader<string> r)
    {
        Writer = w;
        Reader = r;
    }
}
