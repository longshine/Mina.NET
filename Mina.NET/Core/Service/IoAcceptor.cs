using System;
using System.Collections.Generic;
using System.Net;

namespace Mina.Core.Service
{
    /// <summary>
    /// Accepts incoming connection, communicates with clients, and fires events to <see cref="IoHandler"/>s.
    /// </summary>
    public interface IoAcceptor : IoService
    {
        /// <summary>
        /// Gets or sets a value indicating whether all client sessions are closed
        /// when this acceptor unbinds from all the related local endpoints
        /// (i.e. when the service is deactivated).
        /// The default value is <code>true</code>.
        /// </summary>
        Boolean CloseOnDeactivation { get; set; }
        /// <summary>
        /// Gets the local endpoint which is bound currently.
        /// If more than one endpoint are bound, only one of them will be returned.
        /// </summary>
        EndPoint LocalEndPoint { get; }
        /// <summary>
        /// Gets all the local endpoints which are bound currently.
        /// </summary>
        IEnumerable<EndPoint> LocalEndPoints { get; }
        /// <summary>
        /// Gets or sets the default local endpoint to bind when no
        /// argument is specified in <see cref="Bind()"/> method.
        /// If more than one endpoint are set, only one of them will be returned.
        /// </summary>
        EndPoint DefaultLocalEndPoint { get; set; }
        /// <summary>
        /// Gets or sets the default local endpoints to bind when no
        /// argument is specified in <see cref="Bind()"/> method.
        /// </summary>
        IEnumerable<EndPoint> DefaultLocalEndPoints { get; set; }
        /// <summary>
        /// Binds to the default local endpoint(s) and start to accept incoming connections.
        /// </summary>
        void Bind();
        /// <summary>
        /// Binds to the specified local endpoint and start to accept incoming connections.
        /// </summary>
        /// <param name="localEP">the local endpoint to bind to</param>
        void Bind(EndPoint localEP);
        /// <summary>
        /// Binds to the specified local endpoints and start to accept incoming connections.
        /// If no endpoints is given, bind on the default local endpoint(s).
        /// </summary>
        /// <param name="localEndPoints">the local endpoints to bind to</param>
        void Bind(params EndPoint[] localEndPoints);
        /// <summary>
        /// Binds to the specified local addresses and start to accept incoming connections.
        /// </summary>
        /// <param name="localEndPoints">the local endpoints to bind to</param>
        void Bind(IEnumerable<EndPoint> localEndPoints);
        /// <summary>
        /// Unbinds from all local endpoints that this service is bound to and stops
        /// to accept incoming connections.
        /// All managed connections will be closed if <see cref="CloseOnDeactivation"/> is <code>true</code>.
        /// This method returns silently if no local endpoints is bound yet.
        /// </summary>
        void Unbind();
        /// <summary>
        /// Unbinds from the specified local address and stop to accept incoming connections.
        /// All managed connections will be closed if <see cref="CloseOnDeactivation"/> is <code>true</code>.
        /// This method returns silently if no local endpoints is bound yet.
        /// </summary>
        /// <param name="localEP">the local endpoint to unbind</param>
        void Unbind(EndPoint localEP);
        /// <summary>
        /// Unbinds from the specified local addresses and stop to accept incoming connections.
        /// All managed connections will be closed if <see cref="CloseOnDeactivation"/> is <code>true</code>.
        /// This method returns silently if no local endpoints is bound yet.
        /// </summary>
        /// <param name="localEndPoints">the local endpoints to unbind</param>
        void Unbind(params EndPoint[] localEndPoints);
        /// <summary>
        /// Unbinds from the specified local addresses and stop to accept incoming connections.
        /// All managed connections will be closed if <see cref="CloseOnDeactivation"/> is <code>true</code>.
        /// This method returns silently if no local endpoints is bound yet.
        /// </summary>
        /// <param name="localEndPoints">the local endpoints to unbind</param>
        void Unbind(IEnumerable<EndPoint> localEndPoints);
    }
}
