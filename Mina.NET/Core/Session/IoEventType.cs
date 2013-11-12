using System;

namespace Mina.Core.Session
{
    /// <summary>
    /// Represents the type of I/O events and requests.
    /// It is usually used by internal components to store I/O events.
    /// </summary>
    [Flags]
    public enum IoEventType
    {
        SessionCreated,
        SessionOpened,
        SessionClosed,
        MessageReceived,
        MessageSent,
        SessionIdle,
        ExceptionCaught,
        Write,
        Close
    }
}
