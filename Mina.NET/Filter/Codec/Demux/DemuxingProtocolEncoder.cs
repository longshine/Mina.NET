using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Mina.Core.Session;

namespace Mina.Filter.Codec.Demux
{
    /// <summary>
    /// A composite <see cref="IProtocolEncoder"/> that demultiplexes incoming message
    /// encoding requests into an appropriate <see cref="IMessageEncoder"/>.
    /// </summary>
    public class DemuxingProtocolEncoder : IProtocolEncoder
    {
        private readonly AttributeKey STATE;
        private readonly Dictionary<Type, IMessageEncoderFactory> _type2encoderFactory
             = new Dictionary<Type, IMessageEncoderFactory>();

        public DemuxingProtocolEncoder()
        {
            STATE = new AttributeKey(GetType(), "state");
        }

        public void AddMessageEncoder<TMessage, TEncoder>() where TEncoder : IMessageEncoder
        {
            Type encoderType = typeof(TEncoder);

            if (encoderType.GetConstructor(DemuxingProtocolCodecFactory.EmptyParams) == null)
                throw new ArgumentException("The specified class doesn't have a public default constructor.");

            AddMessageEncoder<TMessage>(new DefaultConstructorMessageEncoderFactory<TMessage>(encoderType));
        }

        public void AddMessageEncoder<TMessage>(IMessageEncoder<TMessage> encoder)
        {
            AddMessageEncoder<TMessage>(new SingletonMessageEncoderFactory<TMessage>(encoder));
        }

        public void AddMessageEncoder<TMessage>(IMessageEncoderFactory<TMessage> factory)
        {
            if (factory == null)
                throw new ArgumentNullException("factory");

            Type messageType = typeof(TMessage);
            lock (_type2encoderFactory)
            {
                if (_type2encoderFactory.ContainsKey(messageType))
                    throw new InvalidOperationException("The specified message type (" + messageType.Name
                        + ") is registered already.");
                _type2encoderFactory[messageType] = factory;
            }
        }

        public void Encode(IoSession session, Object message, IProtocolEncoderOutput output)
        {
            State state = GetState(session);
            IMessageEncoder encoder = FindEncoder(state, message.GetType());
            if (encoder == null)
                throw new UnknownMessageTypeException("No message encoder found for message: " + message);
            else
                encoder.Encode(session, message, output);
        }

        public void Dispose(IoSession session)
        {
            session.RemoveAttribute(STATE);
        }

        private State GetState(IoSession session)
        {
            State state = session.GetAttribute<State>(STATE);
            if (state == null)
            {
                state = new State(_type2encoderFactory);
                State oldState = (State)session.SetAttributeIfAbsent(STATE, state);
                if (oldState != null)
                {
                    state = oldState;
                }
            }
            return state;
        }

        private IMessageEncoder FindEncoder(State state, Type type)
        {
            return FindEncoder(state, type, null);
        }

        private IMessageEncoder FindEncoder(State state, Type type, HashSet<Type> triedClasses)
        {
            IMessageEncoder encoder = null;

            if (triedClasses != null && triedClasses.Contains(type))
                return null;

            // Try the cache first.
            if (state.findEncoderCache.TryGetValue(type, out encoder))
                return encoder;

            // Try the registered encoders for an immediate match.
            state.type2encoder.TryGetValue(type, out encoder);

            if (encoder == null)
            {
                // No immediate match could be found. Search the type's interfaces.
                if (triedClasses == null)
                    triedClasses = new HashSet<Type>();
                triedClasses.Add(type);

                foreach (Type ifc in type.GetInterfaces())
                {
                    encoder = FindEncoder(state, ifc, triedClasses);
                    if (encoder != null)
                        break;
                }
            }

            if (encoder == null)
            {
                // No match in type's interfaces could be found. Search the superclass.
                Type baseType = type.BaseType;
                if (baseType != null)
                    encoder = FindEncoder(state, baseType);
            }

            /*
             * Make sure the encoder is added to the cache. By updating the cache
             * here all the types (superclasses and interfaces) in the path which
             * led to a match will be cached along with the immediate message type.
             */
            if (encoder != null)
            {
                encoder = state.findEncoderCache.GetOrAdd(type, encoder);
            }

            return encoder;
        }

        class State
        {
            public readonly ConcurrentDictionary<Type, IMessageEncoder> findEncoderCache
                = new ConcurrentDictionary<Type, IMessageEncoder>();
            public ConcurrentDictionary<Type, IMessageEncoder> type2encoder
                = new ConcurrentDictionary<Type, IMessageEncoder>();

            public State(IDictionary<Type, IMessageEncoderFactory> type2encoderFactory)
            {
                foreach (KeyValuePair<Type, IMessageEncoderFactory> pair in type2encoderFactory)
                {
                    type2encoder[pair.Key] = pair.Value.GetEncoder();
                }
            }
        }

        class SingletonMessageEncoderFactory<T> : IMessageEncoderFactory<T>
        {
            private readonly IMessageEncoder<T> encoder;

            public SingletonMessageEncoderFactory(IMessageEncoder<T> encoder)
            {
                if (encoder == null)
                    throw new ArgumentNullException("encoder");
                this.encoder = encoder;
            }

            public IMessageEncoder<T> GetEncoder()
            {
                return encoder;
            }

            IMessageEncoder IMessageEncoderFactory.GetEncoder()
            {
                return encoder;
            }
        }

        class DefaultConstructorMessageEncoderFactory<T> : IMessageEncoderFactory<T>
        {
            private readonly Type encoderType;

            public DefaultConstructorMessageEncoderFactory(Type encoderType)
            {
                this.encoderType = encoderType;
            }

            public IMessageEncoder<T> GetEncoder()
            {
                return (IMessageEncoder<T>)Activator.CreateInstance(encoderType);
            }

            IMessageEncoder IMessageEncoderFactory.GetEncoder()
            {
                return GetEncoder();
            }
        }
    }
}
