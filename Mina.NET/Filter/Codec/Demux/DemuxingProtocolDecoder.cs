using System;
using Mina.Core.Buffer;
using Mina.Core.Session;

namespace Mina.Filter.Codec.Demux
{
    /// <summary>
    /// A composite <see cref="IProtocolDecoder"/> that demultiplexes incoming <see cref="IoBuffer"/>
    /// decoding requests into an appropriate <see cref="IMessageDecoder"/>.
    /// </summary>
    public class DemuxingProtocolDecoder : CumulativeProtocolDecoder
    {
        private readonly AttributeKey STATE;
        private IMessageDecoderFactory[] _decoderFactories = new IMessageDecoderFactory[0];

        public DemuxingProtocolDecoder()
        {
            STATE = new AttributeKey(GetType(), "state");
        }

        public void AddMessageDecoder<TDecoder>() where TDecoder : IMessageDecoder
        {
            Type decoderType = typeof(TDecoder);

            if (decoderType.GetConstructor(DemuxingProtocolCodecFactory.EmptyParams) == null)
                throw new ArgumentException("The specified class doesn't have a public default constructor.");

            AddMessageDecoder(new DefaultConstructorMessageDecoderFactory(decoderType));
        }

        public void AddMessageDecoder(IMessageDecoder decoder)
        {
            AddMessageDecoder(new SingletonMessageDecoderFactory(decoder));
        }

        public void AddMessageDecoder(IMessageDecoderFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException("factory");

            IMessageDecoderFactory[] decoderFactories = _decoderFactories;
            IMessageDecoderFactory[] newDecoderFactories = new IMessageDecoderFactory[decoderFactories.Length + 1];
            Array.Copy(decoderFactories, 0, newDecoderFactories, 0, decoderFactories.Length);
            newDecoderFactories[decoderFactories.Length] = factory;
            _decoderFactories = newDecoderFactories;
        }

        /// <inheritdoc/>
        public override void Dispose(IoSession session)
        {
            base.Dispose(session);
            session.RemoveAttribute(STATE);
        }

        /// <inheritdoc/>
        public override void FinishDecode(IoSession session, IProtocolDecoderOutput output)
        {
            base.FinishDecode(session, output);
            State state = GetState(session);
            IMessageDecoder currentDecoder = state.currentDecoder;
            if (currentDecoder == null)
                return;
            currentDecoder.FinishDecode(session, output);
        }

        /// <inheritdoc/>
        protected override bool DoDecode(IoSession session, IoBuffer input, IProtocolDecoderOutput output)
        {
            State state = GetState(session);

            if (state.currentDecoder == null)
            {
                IMessageDecoder[] decoders = state.decoders;
                int undecodables = 0;

                for (int i = decoders.Length - 1; i >= 0; i--)
                {
                    IMessageDecoder decoder = decoders[i];
                    int limit = input.Limit;
                    int pos = input.Position;

                    MessageDecoderResult result;

                    try
                    {
                        result = decoder.Decodable(session, input);
                    }
                    finally
                    {
                        input.Position = pos;
                        input.Limit = limit;
                    }

                    if (result == MessageDecoderResult.OK)
                    {
                        state.currentDecoder = decoder;
                        break;
                    }
                    else if (result == MessageDecoderResult.NotOK)
                    {
                        undecodables++;
                    }
                    else if (result != MessageDecoderResult.NeedData)
                    {
                        throw new InvalidOperationException("Unexpected decode result (see your decodable()): " + result);
                    }
                }

                if (undecodables == decoders.Length)
                {
                    // Throw an exception if all decoders cannot decode data.
                    String dump = input.GetHexDump();
                    input.Position = input.Limit; // Skip data
                    ProtocolDecoderException e = new ProtocolDecoderException("No appropriate message decoder: " + dump);
                    e.Hexdump = dump;
                    throw e;
                }

                if (state.currentDecoder == null)
                {
                    // Decoder is not determined yet (i.e. we need more data)
                    return false;
                }
            }

            try
            {
                MessageDecoderResult result = state.currentDecoder.Decode(session, input, output);
                if (result == MessageDecoderResult.OK)
                {
                    state.currentDecoder = null;
                    return true;
                }
                else if (result == MessageDecoderResult.NeedData)
                {
                    return false;
                }
                else if (result == MessageDecoderResult.NotOK)
                {
                    state.currentDecoder = null;
                    throw new ProtocolDecoderException("Message decoder returned NOT_OK.");
                }
                else
                {
                    state.currentDecoder = null;
                    throw new InvalidOperationException("Unexpected decode result (see your decode()): " + result);
                }
            }
            catch (Exception)
            {
                state.currentDecoder = null;
                throw;
            }
        }

        private State GetState(IoSession session)
        {
            State state = session.GetAttribute<State>(STATE);

            if (state == null)
            {
                state = new State(_decoderFactories);
                State oldState = (State)session.SetAttributeIfAbsent(STATE, state);

                if (oldState != null)
                {
                    state = oldState;
                }
            }

            return state;
        }

        class State
        {
            public readonly IMessageDecoder[] decoders;
            public IMessageDecoder currentDecoder;

            public State(IMessageDecoderFactory[] decoderFactories)
            {
                decoders = new IMessageDecoder[decoderFactories.Length];
                for (Int32 i = decoderFactories.Length - 1; i >= 0; i--)
                {
                    decoders[i] = decoderFactories[i].GetDecoder();
                }
            }
        }

        class SingletonMessageDecoderFactory : IMessageDecoderFactory
        {
            private readonly IMessageDecoder decoder;

            public SingletonMessageDecoderFactory(IMessageDecoder decoder)
            {
                if (decoder == null)
                    throw new ArgumentNullException("decoder");
                this.decoder = decoder;
            }

            public IMessageDecoder GetDecoder()
            {
                return decoder;
            }
        }

        class DefaultConstructorMessageDecoderFactory : IMessageDecoderFactory
        {
            private readonly Type decoderType;

            public DefaultConstructorMessageDecoderFactory(Type decoderType)
            {
                this.decoderType = decoderType;
            }

            public IMessageDecoder GetDecoder()
            {
                return (IMessageDecoder)Activator.CreateInstance(decoderType);
            }
        }
    }
}
