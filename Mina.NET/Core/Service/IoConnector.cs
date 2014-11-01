using System;
using System.Net;
using Mina.Core.Future;
using Mina.Core.Session;

namespace Mina.Core.Service
{
    /// <summary>
    /// Connects to endpoint, communicates with the server, and fires events to <see cref="IoHandler"/>s.
    /// </summary>
    public interface IoConnector : IoService
    {
        /// <summary>
        /// Gets or sets connect timeout in seconds. The default value is 1 minute.
        /// <seealso cref="ConnectTimeoutInMillis"/>
        /// </summary>
        Int32 ConnectTimeout { get; set; }
        /// <summary>
        /// Gets or sets connect timeout in milliseconds. The default value is 1 minute.
        /// </summary>
        Int64 ConnectTimeoutInMillis { get; set; }
        /// <summary>
        /// Gets or sets the default remote endpoint to connect to when no argument
        /// is specified in <see cref="Connect()"/> method.
        /// </summary>
        EndPoint DefaultRemoteEndPoint { get; set; }
        /// <summary>
        /// Gets or sets the default local endpoint.
        /// </summary>
        EndPoint DefaultLocalEndPoint { get; set; }
        /// <summary>
        /// Connects to the <see cref="DefaultRemoteEndPoint"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">if no default remoted address is set</exception>
        IConnectFuture Connect();
        /// <summary>
        /// Connects to the <see cref="DefaultRemoteEndPoint"/> and invokes the <code>ioSessionInitializer</code>
        /// when the IoSession is created but before <code>SessionCreated</code> is fired.
        /// </summary>
        /// <exception cref="InvalidOperationException">if no default remoted address is set</exception>
        IConnectFuture Connect(Action<IoSession, IConnectFuture> sessionInitializer);
        /// <summary>
        /// Connects to the specified remote endpoint.
        /// </summary>
        /// <exception cref="InvalidOperationException">if no default remoted address is set</exception>
        IConnectFuture Connect(EndPoint remoteEP);
        /// <summary>
        /// Connects to the specified remote endpoint and invokes the <code>ioSessionInitializer</code>
        /// when the IoSession is created but before <code>SessionCreated</code> is fired.
        /// </summary>
        /// <exception cref="InvalidOperationException">if no default remoted address is set</exception>
        IConnectFuture Connect(EndPoint remoteEP, Action<IoSession, IConnectFuture> sessionInitializer);
        /// <summary>
        /// Connects to the specified remote endpoint binding to the specified local endpoint.
        /// </summary>
        /// <exception cref="InvalidOperationException">if no default remoted address is set</exception>
        IConnectFuture Connect(EndPoint remoteEP, EndPoint localEP);
        /// <summary>
        /// Connects to the specified remote endpoint binding to the specified local endpoint,
        /// and invokes the <code>ioSessionInitializer</code>
        /// when the IoSession is created but before <code>SessionCreated</code> is fired.
        /// </summary>
        /// <exception cref="InvalidOperationException">if no default remoted address is set</exception>
        IConnectFuture Connect(EndPoint remoteEP, EndPoint localEP, Action<IoSession, IConnectFuture> sessionInitializer);
    }
}
