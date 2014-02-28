using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Mina.Core.Service;
using Mina.Core.Session;

namespace Mina.Handler.Demux
{
    /// <summary>
    /// A <see cref="IoHandler"/> that demuxes <code>MessageReceived</code> events
    /// to the appropriate <see cref="IMessageHandler"/>.
    /// </summary>
    public class DemuxingIoHandler : IoHandlerAdapter
    {
        private readonly ConcurrentDictionary<Type, IMessageHandler> _receivedMessageHandlers
            = new ConcurrentDictionary<Type, IMessageHandler>();
        private readonly ConcurrentDictionary<Type, IMessageHandler> _receivedMessageHandlerCache
            = new ConcurrentDictionary<Type, IMessageHandler>();

        private readonly ConcurrentDictionary<Type, IMessageHandler> _sentMessageHandlers
            = new ConcurrentDictionary<Type, IMessageHandler>();
        private readonly ConcurrentDictionary<Type, IMessageHandler> _sentMessageHandlerCache
            = new ConcurrentDictionary<Type, IMessageHandler>();

        private readonly ConcurrentDictionary<Type, IExceptionHandler> _exceptionHandlers
            = new ConcurrentDictionary<Type, IExceptionHandler>();
        private readonly ConcurrentDictionary<Type, IExceptionHandler> _exceptionHandlerCache
            = new ConcurrentDictionary<Type, IExceptionHandler>();

        /// <summary>
        /// Registers a <see cref="IMessageHandler&lt;T&gt;"/> that handles the received messages of
        /// the specified <typeparamref name="T"/>.
        /// </summary>
        /// <returns>the old handler if there is already a registered handler for the specified type</returns>
        public IMessageHandler<T> AddReceivedMessageHandler<T>(IMessageHandler<T> handler)
        {
            return (IMessageHandler<T>)AddHandler(_receivedMessageHandlers, _receivedMessageHandlerCache, typeof(T), handler);
        }

        /// <summary>
        /// Deregisters a <see cref="IMessageHandler&lt;T&gt;"/> that handles the received messages of
        /// the specified <typeparamref name="T"/>.
        /// </summary>
        /// <returns>the removed handler if successfully removed, null otherwise</returns>
        public IMessageHandler<T> RemoveReceivedMessageHandler<T>(IMessageHandler<T> handler)
        {
            return (IMessageHandler<T>)RemoveHandler(_receivedMessageHandlers, _receivedMessageHandlerCache, typeof(T));
        }

        /// <summary>
        /// Registers a <see cref="IMessageHandler&lt;T&gt;"/> that handles the sent messages of
        /// the specified <typeparamref name="T"/>.
        /// </summary>
        /// <returns>the old handler if there is already a registered handler for the specified type</returns>
        public IMessageHandler<T> AddSentMessageHandler<T>(IMessageHandler<T> handler)
        {
            return (IMessageHandler<T>)AddHandler(_sentMessageHandlers, _sentMessageHandlerCache, typeof(T), handler);
        }

        /// <summary>
        /// Deregisters a <see cref="IMessageHandler&lt;T&gt;"/> that handles the sent messages of
        /// the specified <typeparamref name="T"/>.
        /// </summary>
        /// <returns>the removed handler if successfully removed, null otherwise</returns>
        public IMessageHandler<T> RemoveSentMessageHandler<T>(IMessageHandler<T> handler)
        {
            return (IMessageHandler<T>)RemoveHandler(_sentMessageHandlers, _sentMessageHandlerCache, typeof(T));
        }

        /// <summary>
        /// Registers a <see cref="IMessageHandler&lt;T&gt;"/> that handles exceptions of
        /// the specified <typeparamref name="E"/>.
        /// </summary>
        /// <returns>the old handler if there is already a registered handler for the specified type</returns>
        public IExceptionHandler<E> AddExceptionHandler<E>(IExceptionHandler<E> handler)
            where E : Exception
        {
            return (IExceptionHandler<E>)AddHandler(_exceptionHandlers, _exceptionHandlerCache, typeof(E), handler);
        }

        /// <summary>
        /// Deregisters a <see cref="IMessageHandler&lt;T&gt;"/> that handles exceptions of
        /// the specified <typeparamref name="E"/>.
        /// </summary>
        /// <returns>the removed handler if successfully removed, null otherwise</returns>
        public IExceptionHandler<E> RemoveExceptionHandler<E>()
            where E : Exception
        {
            return (IExceptionHandler<E>)RemoveHandler(_exceptionHandlers, _exceptionHandlerCache, typeof(E));
        }

        /// <inheritdoc/>
        public override void MessageReceived(IoSession session, Object message)
        {
            IMessageHandler handler = FindReceivedMessageHandler(message.GetType());
            if (handler == null)
                throw new UnknownMessageTypeException("No message handler found for message type: "
                    + message.GetType().Name);
            else
                handler.HandleMessage(session, message);
        }

        /// <inheritdoc/>
        public override void MessageSent(IoSession session, Object message)
        {
            IMessageHandler handler = FindSentMessageHandler(message.GetType());
            if (handler == null)
                throw new UnknownMessageTypeException("No message handler found for message type: "
                    + message.GetType().Name);
            else
                handler.HandleMessage(session, message);
        }

        /// <inheritdoc/>
        public override void ExceptionCaught(IoSession session, Exception cause)
        {
            IExceptionHandler handler = FindExceptionHandler(cause.GetType());
            if (handler == null)
                throw new UnknownMessageTypeException("No handler found for exception type: "
                    + cause.GetType().Name);
            else
                handler.ExceptionCaught(session, cause);
        }

        protected IMessageHandler FindReceivedMessageHandler(Type type)
        {
            return FindHandler(_receivedMessageHandlers, _receivedMessageHandlerCache, type, null);
        }

        protected IMessageHandler FindSentMessageHandler(Type type)
        {
            return FindHandler(_sentMessageHandlers, _sentMessageHandlerCache, type, null);
        }

        protected IExceptionHandler FindExceptionHandler(Type type)
        {
            return FindHandler(_exceptionHandlers, _exceptionHandlerCache, type, null);
        }

        private static T FindHandler<T>(IDictionary<Type, T> handlers, IDictionary<Type, T> handlerCache, Type type, HashSet<Type> triedClasses)
        {
            T handler = default(T);

            if (triedClasses != null && triedClasses.Contains(type))
                return default(T);

            // Try the cache first.
            if (handlerCache.TryGetValue(type, out handler))
                return handler;

            // Try the registered handlers for an immediate match.
            handlers.TryGetValue(type, out handler);

            if (handler == null)
            {
                // No immediate match could be found. Search the type's interfaces.

                if (triedClasses == null)
                    triedClasses = new HashSet<Type>();
                triedClasses.Add(type);

                foreach (Type ifc in type.GetInterfaces())
                {
                    handler = FindHandler(handlers, handlerCache, ifc, triedClasses);
                    if (handler != null)
                        break;
                }
            }

            if (handler == null)
            {
                // No match in type's interfaces could be found. Search the superclass.
                Type baseType = type.BaseType;
                if (baseType != null)
                    handler = FindHandler(handlers, handlerCache, baseType, null);
            }

            /*
             * Make sure the handler is added to the cache. By updating the cache
             * here all the types (superclasses and interfaces) in the path which
             * led to a match will be cached along with the immediate message type.
             */
            if (handler != null)
            {
                handlerCache[type] = handler;
            }

            return handler;
        }

        private static T AddHandler<T>(ConcurrentDictionary<Type, T> handlers, ConcurrentDictionary<Type, T> handlerCache, Type type, T handler)
        {
            handlerCache.Clear();

            T old = default(T);
            handlers.AddOrUpdate(type, handler, (t, h) =>
            {
                old = h;
                return handler;
            });
            return old;
        }

        private static T RemoveHandler<T>(ConcurrentDictionary<Type, T> handlers, ConcurrentDictionary<Type, T> handlerCache, Type type)
        {
            handlerCache.Clear();
            T old;
            return handlers.TryRemove(type, out old) ? old : default(T);
        }
    }
}
