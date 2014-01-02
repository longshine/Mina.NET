using System;
using Common.Logging;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using Mina.Core.Session;
using Mina.Core.Write;
using Mina.Util;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// An {@link IoFilter} which translates binary or protocol specific data into
    /// message objects and vice versa using <see cref="IProtocolCodecFactory"/>,
    /// <see cref="IProtocolEncoder"/>, or <see cref="IProtocolDecoder"/>.
    /// </summary>
    public class ProtocolCodecFilter : IoFilterAdapter
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ProtocolCodecFilter));
        private static readonly IoBuffer EMPTY_BUFFER = IoBuffer.Wrap(new Byte[0]);
        private readonly AttributeKey DECODER_OUT = new AttributeKey(typeof(ProtocolCodecFilter), "decoderOut");
        private readonly AttributeKey ENCODER_OUT = new AttributeKey(typeof(ProtocolCodecFilter), "encoderOut");
        private readonly IProtocolCodecFactory _factory;

        public ProtocolCodecFilter(IProtocolCodecFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException("factory");

            _factory = factory;
        }

        public ProtocolCodecFilter(IProtocolEncoder encoder, IProtocolDecoder decoder)
        {
            if (encoder == null)
                throw new ArgumentNullException("encoder");
            if (decoder == null)
                throw new ArgumentNullException("decoder");

            _factory = new ProtocolCodecFactory(encoder, decoder);
        }

        public override void OnPreAdd(IoFilterChain parent, String name, INextFilter nextFilter)
        {
            if (parent.Contains(this))
                throw new ArgumentException("You can't add the same filter instance more than once.  Create another instance and add it.");
        }

        public override void OnPostRemove(IoFilterChain parent, String name, INextFilter nextFilter)
        {
            // Clean everything
            DisposeCodec(parent.Session);
        }

        public override void MessageReceived(INextFilter nextFilter, IoSession session, Object message)
        {
            //if (log.IsDebugEnabled)
            //    log.DebugFormat("Processing a MESSAGE_RECEIVED for session {0}", session.Id);

            IoBuffer input = message as IoBuffer;
            if (input == null)
            {
                nextFilter.MessageReceived(session, message);
                return;
            }

            IProtocolDecoder decoder = _factory.GetDecoder(session);
            IProtocolDecoderOutput decoderOutput = GetDecoderOut(session, nextFilter);

            // Loop until we don't have anymore byte in the buffer,
            // or until the decoder throws an unrecoverable exception or
            // can't decoder a message, because there are not enough
            // data in the buffer
            while (input.HasRemaining)
            {
                Int32 oldPos = input.Position;
                try
                {
                    // Call the decoder with the read bytes
                    decoder.Decode(session, input, decoderOutput);

                    // Finish decoding if no exception was thrown.
                    decoderOutput.Flush(nextFilter, session);
                }
                catch (Exception ex)
                {
                    ProtocolDecoderException pde = ex as ProtocolDecoderException;
                    if (pde == null)
                        pde = new ProtocolDecoderException(null, ex);
                    if (pde.Hexdump == null)
                    {
                        // Generate a message hex dump
                        Int32 curPos = input.Position;
                        input.Position = oldPos;
                        pde.Hexdump = input.GetHexDump();
                        input.Position = curPos;
                    }

                    decoderOutput.Flush(nextFilter, session);
                    nextFilter.ExceptionCaught(session, pde);

                    // Retry only if the type of the caught exception is
                    // recoverable and the buffer position has changed.
                    // We check buffer position additionally to prevent an
                    // infinite loop.
                    if (!(ex is RecoverableProtocolDecoderException) || input.Position == oldPos)
                        break;
                }
            }
        }

        public override void MessageSent(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            if (writeRequest is EncodedWriteRequest)
                return;

            if (writeRequest is MessageWriteRequest)
            {
                MessageWriteRequest wrappedRequest = (MessageWriteRequest)writeRequest;
                nextFilter.MessageSent(session, wrappedRequest.InnerRequest);
            }
            else
            {
                nextFilter.MessageSent(session, writeRequest);
            }
        }

        public override void FilterWrite(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            Object message = writeRequest.Message;

            // Bypass the encoding if the message is contained in a IoBuffer,
            // as it has already been encoded before
            if (message is IoBuffer)
            {
                nextFilter.FilterWrite(session, writeRequest);
                return;
            }

            // Get the encoder in the session
            IProtocolEncoder encoder = _factory.GetEncoder(session);
            IProtocolEncoderOutput encoderOut = GetEncoderOut(session, nextFilter, writeRequest);

            if (encoder == null)
                throw new ProtocolEncoderException("The encoder is null for the session " + session);
            if (encoderOut == null)
                throw new ProtocolEncoderException("The encoderOut is null for the session " + session);

            try
            {
                encoder.Encode(session, message, encoderOut);
                AbstractProtocolEncoderOutput ape = encoderOut as AbstractProtocolEncoderOutput;
                if (ape != null)
                {
                    // Send it directly
                    IQueue<Object> bufferQueue = ape.MessageQueue;
                    // Write all the encoded messages now
                    while (!bufferQueue.Empty)
                    {
                        Object encodedMessage = bufferQueue.Dequeue();

                        if (encodedMessage == null)
                            break;

                        // Flush only when the buffer has remaining.
                        if (!(encodedMessage is IoBuffer) || ((IoBuffer)encodedMessage).HasRemaining)
                        {
                            IWriteRequest encodedWriteRequest = new EncodedWriteRequest(encodedMessage, null);
                            nextFilter.FilterWrite(session, encodedWriteRequest);
                        }
                    }
                }

                // Call the next filter
                nextFilter.FilterWrite(session, new MessageWriteRequest(writeRequest));
            }
            catch (Exception ex)
            {
                ProtocolEncoderException pee = ex as ProtocolEncoderException;
                if (pee == null)
                    pee = new ProtocolEncoderException(null, ex);
                throw pee;
            }
        }

        public override void SessionClosed(INextFilter nextFilter, IoSession session)
        {
            // Call finishDecode() first when a connection is closed.
            IProtocolDecoder decoder = _factory.GetDecoder(session);
            IProtocolDecoderOutput decoderOut = GetDecoderOut(session, nextFilter);

            try
            {
                decoder.FinishDecode(session, decoderOut);
            }
            catch (Exception ex)
            {
                ProtocolDecoderException pde = ex as ProtocolDecoderException;
                if (pde == null)
                    pde = new ProtocolDecoderException(null, ex);
                throw pde;
            }
            finally
            {
                // Dispose everything
                DisposeCodec(session);
                decoderOut.Flush(nextFilter, session);
            }

            // Call the next filter
            nextFilter.SessionClosed(session);
        }

        private IProtocolDecoderOutput GetDecoderOut(IoSession session, INextFilter nextFilter)
        {
            IProtocolDecoderOutput output = session.GetAttribute<IProtocolDecoderOutput>(DECODER_OUT);

            if (output == null)
            {
                // Create a new instance, and stores it into the session
                output = new ProtocolDecoderOutputImpl();
                session.SetAttribute(DECODER_OUT, output);
            }

            return output;
        }

        private IProtocolEncoderOutput GetEncoderOut(IoSession session, INextFilter nextFilter, IWriteRequest writeRequest)
        {
            IProtocolEncoderOutput output = session.GetAttribute<IProtocolEncoderOutput>(ENCODER_OUT);

            if (output == null)
            {
                // Create a new instance, and stores it into the session
                output = new ProtocolEncoderOutputImpl(session, nextFilter, writeRequest);
                session.SetAttribute(ENCODER_OUT, output);
            }

            return output;
        }

        private void DisposeCodec(IoSession session)
        {
            // We just remove the two instances of encoder/decoder to release resources
            // from the session
            //DisposeEncoder(session);
            //DisposeDecoder(session);

            // We also remove the callback
            DisposeDecoderOut(session);
        }

        private void DisposeDecoderOut(IoSession session)
        {
            session.RemoveAttribute(DECODER_OUT);
        }

        class ProtocolDecoderOutputImpl : AbstractProtocolDecoderOutput
        {
            public override void Flush(INextFilter nextFilter, IoSession session)
            {
                IQueue<Object> messageQueue = MessageQueue;

                while (!messageQueue.Empty)
                {
                    nextFilter.MessageReceived(session, messageQueue.Dequeue());
                }
            }
        }

        class ProtocolEncoderOutputImpl : AbstractProtocolEncoderOutput
        {
            private readonly IoSession _session;
            private readonly INextFilter _nextFilter;

            public ProtocolEncoderOutputImpl(IoSession session, INextFilter nextFilter, IWriteRequest writeRequest)
            {
                _session = session;
                _nextFilter = nextFilter;
            }

            public override IWriteFuture Flush()
            {
                IQueue<Object> bufferQueue = MessageQueue;
                IWriteFuture future = null;

                while (!bufferQueue.Empty)
                {
                    Object encodedMessage = bufferQueue.Dequeue();

                    if (encodedMessage == null)
                        break;

                    // Flush only when the buffer has remaining.
                    if (!(encodedMessage is IoBuffer) || ((IoBuffer)encodedMessage).HasRemaining)
                    {
                        future = new DefaultWriteFuture(_session);
                        _nextFilter.FilterWrite(_session, new EncodedWriteRequest(encodedMessage, future));
                    }
                }

                if (future == null)
                {
                    // Creates an empty writeRequest containing the destination
                    IWriteRequest writeRequest = new DefaultWriteRequest(null, null);
                    future = DefaultWriteFuture.NewNotWrittenFuture(_session, new NothingWrittenException(writeRequest));
                }

                return future;
            }
        }

        class EncodedWriteRequest : DefaultWriteRequest
        {
            public EncodedWriteRequest(Object encodedMessage, IWriteFuture future)
                : base(encodedMessage, future)
            { }

            public override Boolean Encoded
            {
                get { return true; }
            }
        }

        class MessageWriteRequest : WriteRequestWrapper
        {
            public MessageWriteRequest(IWriteRequest writeRequest)
                : base(writeRequest)
            { }

            public override Object Message
            {
                get { return EMPTY_BUFFER; }
            }
        }

        class ProtocolCodecFactory : IProtocolCodecFactory
        {
            private IProtocolEncoder _encoder;
            private IProtocolDecoder _decoder;

            public ProtocolCodecFactory(IProtocolEncoder encoder, IProtocolDecoder decoder)
            {
                _encoder = encoder;
                _decoder = decoder;
            }

            public IProtocolEncoder GetEncoder(IoSession session)
            {
                return _encoder;
            }

            public IProtocolDecoder GetDecoder(IoSession session)
            {
                return _decoder;
            }
        }
    }
}
