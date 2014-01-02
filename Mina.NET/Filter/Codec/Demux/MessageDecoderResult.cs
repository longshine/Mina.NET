namespace Mina.Filter.Codec.Demux
{
    /// <summary>
    /// Represents results from <see cref="IMessageDecoder"/>.
    /// </summary>
    public enum MessageDecoderResult
    {
        OK,
        NeedData,
        NotOK
    }
}
