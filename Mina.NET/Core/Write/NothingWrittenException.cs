namespace Mina.Core.Write
{
    /// <summary>
    /// An exception which is thrown when one or more write requests resulted
    /// in no actual write operation.
    /// </summary>
    public class NothingWrittenException : WriteException
    {
        public NothingWrittenException(IWriteRequest request)
            : base(request)
        { }
    }
}
