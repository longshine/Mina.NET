﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Common.Logging;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.Ssl
{
    /// <summary>
    /// An SSL filter that encrypts and decrypts the data exchanged in the session.
    /// Adding this filter triggers SSL handshake procedure immediately.
    /// </summary>
    public class SslFilter : IoFilterAdapter
    {
        static readonly ILog log = LogManager.GetLogger(typeof(SslFilter));

        private static readonly AttributeKey NEXT_FILTER = new AttributeKey(typeof(SslFilter), "nextFilter");
        private static readonly AttributeKey SSL_HANDLER = new AttributeKey(typeof(SslFilter), "handler");

        private readonly X509Certificate _serverCertificate = null;
#if !NETCore
        SslProtocols _sslProtocol = SslProtocols.Default;
#else
        SslProtocols _sslProtocol = SslProtocols.None;
#endif

        /// <summary>
        /// Creates a new SSL filter using the specified PKCS7 signed file.
        /// </summary>
        /// <param name="certFile">the path of the PKCS7 signed file from which to create the X.509 certificate</param>
        public SslFilter(String certFile)
            : this(X509Certificate.CreateFromCertFile(certFile))
        { }

        /// <summary>
        /// Creates a new SSL filter using the specified certificate.
        /// </summary>
        /// <param name="cert">the <see cref="X509Certificate"/> to use</param>
        public SslFilter(X509Certificate cert)
        {
            _serverCertificate = cert;
            CheckCertificateRevocation = true;
        }

        /// <summary>
        /// Creates a new SSL filter to a server.
        /// </summary>
        /// <param name="targetHost">the name of the server that shares this SSL connection</param>
        /// <param name="clientCertificates">the <see cref="X509CertificateCollection"/> containing client certificates</param>
        public SslFilter(String targetHost, X509CertificateCollection clientCertificates)
        {
            TargetHost = targetHost;
            ClientCertificates = clientCertificates;
            UseClientMode = true;
            CheckCertificateRevocation = false;
        }

        /// <summary>
        /// Gets or sets the protocol used for authentication.
        /// </summary>
        public SslProtocols SslProtocol
        {
            get { return _sslProtocol; }
            set { _sslProtocol = value; }
        }

        /// <summary>
        /// Gets the X.509 certificate.
        /// </summary>
        public X509Certificate Certificate
        {
            get { return _serverCertificate; }
        }

        /// <summary>
        /// Gets or sets a <see cref="Boolean"/> value that specifies
        /// whether the client must supply a certificate.
        /// The default value is <code>false</code>.
        /// </summary>
        public Boolean ClientCertificateRequired { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="Boolean"/> value that specifies
        /// whether the certificate revocation list is checked during authentication.
        /// The default value is <code>true</code> in server mode,
        /// <code>false</code> in client mode.
        /// </summary>
        public Boolean CheckCertificateRevocation { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="Boolean"/> value that specifies
        /// whether to use client (or server) mode when handshaking.
        /// The default value is <code>false</code> (server mode).
        /// </summary>
        public Boolean UseClientMode { get; set; }

        /// <summary>
        /// Gets or sets the name of the server that shares this SSL connection.
        /// </summary>
        public String TargetHost { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="X509CertificateCollection"/> containing client certificates
        /// </summary>
        public X509CertificateCollection ClientCertificates { get; set; }

        /// <summary>
        /// Returns <code>true</code> if and only if the specified session is
        /// encrypted/decrypted over SSL/TLS currently.
        /// </summary>
        public Boolean IsSslStarted(IoSession session)
        {
            SslHandler handler = session.GetAttribute<SslHandler>(SSL_HANDLER);
            return handler != null && handler.Authenticated;
        }

        /// <inheritdoc/>
        public override void OnPreAdd(IoFilterChain parent, String name, INextFilter nextFilter)
        {
            if (parent.Contains<SslFilter>())
                throw new InvalidOperationException("Only one SSL filter is permitted in a chain.");

            IoSession session = parent.Session;
            session.SetAttribute(NEXT_FILTER, nextFilter);
            // Create a SSL handler and start handshake.
            SslHandler handler = new SslHandler(this, session);
            session.SetAttribute(SSL_HANDLER, handler);
        }

        /// <inheritdoc/>
        public override void OnPostAdd(IoFilterChain parent, String name, INextFilter nextFilter)
        {
            SslHandler handler = GetSslSessionHandler(parent.Session);
            handler.Handshake(nextFilter);
        }

        /// <inheritdoc/>
        public override void OnPreRemove(IoFilterChain parent, String name, INextFilter nextFilter)
        {
            IoSession session = parent.Session;
            session.RemoveAttribute(NEXT_FILTER);
            session.RemoveAttribute(SSL_HANDLER);
        }

        /// <inheritdoc/>
        public override void SessionClosed(INextFilter nextFilter, IoSession session)
        {
            SslHandler handler = GetSslSessionHandler(session);
            try
            {
                // release resources
                handler.Destroy();
            }
            finally
            {
                // notify closed session
                base.SessionClosed(nextFilter, session);
            }
        }

        /// <inheritdoc/>
        public override void MessageReceived(INextFilter nextFilter, IoSession session, Object message)
        {
            IoBuffer buf = (IoBuffer)message;
            SslHandler handler = GetSslSessionHandler(session);
            // forward read encrypted data to SSL handler
            handler.MessageReceived(nextFilter, buf);
        }

        /// <inheritdoc/>
        public override void MessageSent(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            if (!(writeRequest is EncryptedWriteRequest encryptedWriteRequest))
            {
                // ignore extra buffers used for handshaking
            }
            else
            {
                base.MessageSent(nextFilter, session, encryptedWriteRequest.InnerRequest);
            }
        }

        /// <inheritdoc/>
        public override void ExceptionCaught(INextFilter nextFilter, IoSession session, Exception cause)
        {
            base.ExceptionCaught(nextFilter, session, cause);
        }

        /// <inheritdoc/>
        public override void FilterWrite(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            SslHandler handler = GetSslSessionHandler(session);
            handler.ScheduleFilterWrite(nextFilter, writeRequest);
        }

        /// <inheritdoc/>
        public override void FilterClose(INextFilter nextFilter, IoSession session)
        {
            SslHandler handler = session.GetAttribute<SslHandler>(SSL_HANDLER);
            if (handler == null)
            {
                // The connection might already have closed, or
                // SSL might have not started yet.
                base.FilterClose(nextFilter, session);
                return;
            }

            IWriteFuture future = null;
            try
            {
                future = InitiateClosure(handler, session);
                future.Complete += (s, e) => base.FilterClose(nextFilter, session);
            }
            finally
            {
                if (future == null)
                    base.FilterClose(nextFilter, session);
            }
        }

        private IWriteFuture InitiateClosure(SslHandler handler, IoSession session)
        {
            IWriteFuture future = DefaultWriteFuture.NewWrittenFuture(session);
            handler.Destroy();
            return future;
        }

        private SslHandler GetSslSessionHandler(IoSession session)
        {
            SslHandler handler = session.GetAttribute<SslHandler>(SSL_HANDLER);

            if (handler == null)
                throw new InvalidOperationException();

            if (handler.SslFilter != this)
                throw new ArgumentException("Not managed by this filter.");

            return handler;
        }

        public static void DisplaySecurityLevel(SslStream stream)
        {
            log.DebugFormat("Cipher: {0} strength {1}", stream.CipherAlgorithm, stream.CipherStrength);
            log.DebugFormat("Hash: {0} strength {1}", stream.HashAlgorithm, stream.HashStrength);
            log.DebugFormat("Key exchange: {0} strength {1}", stream.KeyExchangeAlgorithm, stream.KeyExchangeStrength);
            log.DebugFormat("Protocol: {0}", stream.SslProtocol);
        }

        public static void DisplaySecurityServices(SslStream stream)
        {
            log.DebugFormat("Is authenticated: {0} as server? {1}", stream.IsAuthenticated, stream.IsServer);
            log.DebugFormat("IsSigned: {0}", stream.IsSigned);
            log.DebugFormat("Is Encrypted: {0}", stream.IsEncrypted);
        }

        public static void DisplayStreamProperties(SslStream stream)
        {
            log.DebugFormat("Can read: {0}, write {1}", stream.CanRead, stream.CanWrite);
            log.DebugFormat("Can timeout: {0}", stream.CanTimeout);
        }

        public static void DisplayCertificateInformation(SslStream stream)
        {
            log.DebugFormat("Certificate revocation list checked: {0}", stream.CheckCertRevocationStatus);

            X509Certificate localCertificate = stream.LocalCertificate;
            if (stream.LocalCertificate != null)
            {
                log.DebugFormat("Local cert was issued to {0} and is valid from {1} until {2}.",
                    localCertificate.Subject,
                    localCertificate.GetEffectiveDateString(),
                    localCertificate.GetExpirationDateString());
            }
            else
            {
                log.DebugFormat("Local certificate is null.");
            }
            // Display the properties of the client's certificate.
            X509Certificate remoteCertificate = stream.RemoteCertificate;
            if (stream.RemoteCertificate != null)
            {
                log.DebugFormat("Remote cert was issued to {0} and is valid from {1} until {2}.",
                    remoteCertificate.Subject,
                    remoteCertificate.GetEffectiveDateString(),
                    remoteCertificate.GetExpirationDateString());
            }
            else
            {
                log.DebugFormat("Remote certificate is null.");
            }
        }

        internal class EncryptedWriteRequest : WriteRequestWrapper
        {
            private readonly IoBuffer _encryptedMessage;

            public EncryptedWriteRequest(IWriteRequest writeRequest, IoBuffer encryptedMessage)
                : base(writeRequest)
            {
                _encryptedMessage = encryptedMessage;
            }

            public override Object Message
            {
                get { return _encryptedMessage; }
            }
        }
    }

    class SslHandler : IDisposable
    {
        static readonly ILog log = LogManager.GetLogger(typeof(SslFilter));

        private readonly SslFilter _sslFilter;
        private readonly IoSession _session;
        private readonly IoSessionStream _sessionStream;
        private readonly SslStream _sslStream;
        private volatile Boolean _authenticated;
        private readonly ConcurrentQueue<IoFilterEvent> _preHandshakeEventQueue = new ConcurrentQueue<IoFilterEvent>();
        private INextFilter _currentNextFilter;
        private IWriteRequest _currentWriteRequest;

        public SslHandler(SslFilter sslFilter, IoSession session)
        {
            _sslFilter = sslFilter;
            _session = session;
            _sessionStream = new IoSessionStream(this);
            _sslStream = new SslStream(_sessionStream, false);
        }

        public SslFilter SslFilter
        {
            get { return _sslFilter; }
        }

        public Boolean Authenticated
        {
            get { return _authenticated; }
            private set
            {
                _authenticated = value;
                if (value)
                    FlushPreHandshakeEvents();
            }
        }

        public void Dispose()
        {
            _sessionStream.Dispose();
            _sslStream.Dispose();
        }

        public void Handshake(INextFilter nextFilter)
        {
            lock (this)
            {
                _currentNextFilter = nextFilter;

                if (_sslFilter.UseClientMode)
                {
                    _sslStream.BeginAuthenticateAsClient(_sslFilter.TargetHost,
                        _sslFilter.ClientCertificates, _sslFilter.SslProtocol,
                        _sslFilter.CheckCertificateRevocation, AuthenticateAsClientCallback, null);
                }
                else
                {
                    _sslStream.BeginAuthenticateAsServer(_sslFilter.Certificate,
                        _sslFilter.ClientCertificateRequired, _sslFilter.SslProtocol,
                        _sslFilter.CheckCertificateRevocation, AuthenticateCallback, null);
                }
            }
        }

        private void AuthenticateAsClientCallback(IAsyncResult ar)
        {
            try
            {
                _sslStream.EndAuthenticateAsClient(ar);
            }
            catch (AuthenticationException e)
            {
                _sslFilter.ExceptionCaught(_currentNextFilter, _session, e);
                return;
            }
            catch (Exception e)
            {
                _sslFilter.ExceptionCaught(_currentNextFilter, _session, e);
                return;
            }

            Authenticated = true;

            if (log.IsDebugEnabled)
            {
                // Display the properties and settings for the authenticated stream.
                SslFilter.DisplaySecurityLevel(_sslStream);
                SslFilter.DisplaySecurityServices(_sslStream);
                SslFilter.DisplayCertificateInformation(_sslStream);
                SslFilter.DisplayStreamProperties(_sslStream);
            }
        }

        private void AuthenticateCallback(IAsyncResult ar)
        {
            try
            {
                _sslStream.EndAuthenticateAsServer(ar);
            }
            catch (AuthenticationException e)
            {
                _sslFilter.ExceptionCaught(_currentNextFilter, _session, e);
                return;
            }
            catch (IOException e)
            {
                _sslFilter.ExceptionCaught(_currentNextFilter, _session, e);
                return;
            }

            Authenticated = true;

            if (log.IsDebugEnabled)
            {
                // Display the properties and settings for the authenticated stream.
                SslFilter.DisplaySecurityLevel(_sslStream);
                SslFilter.DisplaySecurityServices(_sslStream);
                SslFilter.DisplayCertificateInformation(_sslStream);
                SslFilter.DisplayStreamProperties(_sslStream);
            }
        }

        public void ScheduleFilterWrite(INextFilter nextFilter, IWriteRequest writeRequest)
        {
            if (!_authenticated)
            {
                if (_session.Connected)
                {
                    // Handshake not complete yet.
                    _preHandshakeEventQueue.Enqueue(new IoFilterEvent(nextFilter, IoEventType.Write, _session, writeRequest));
                }
                return;
            }

            IoBuffer buf = (IoBuffer)writeRequest.Message;
            if (!buf.HasRemaining)
                // empty message will break this SSL stream?
                return;
            lock (this)
            {
                ArraySegment<Byte> array = buf.GetRemaining();
                _currentNextFilter = nextFilter;
                _currentWriteRequest = writeRequest;
                // SSL encrypt
                _sslStream.Write(array.Array, array.Offset, array.Count);
            }
        }

        public void MessageReceived(INextFilter nextFilter, IoBuffer buf)
        {
            lock (this)
            {
                _currentNextFilter = nextFilter;
                _sessionStream.Write(buf);
                if (_authenticated)
                {
                    IoBuffer readBuffer = ReadBuffer();
                    nextFilter.MessageReceived(_session, readBuffer);
                }
            }
        }

        public void Destroy()
        {
            _sslStream.Close();
            while (_preHandshakeEventQueue.TryDequeue(out _))
            { }
        }

        private void FlushPreHandshakeEvents()
        {
            lock (this)
            {
                while (_preHandshakeEventQueue.TryDequeue(out IoFilterEvent scheduledWrite))
                {
                    _sslFilter.FilterWrite(scheduledWrite.NextFilter, scheduledWrite.Session, (IWriteRequest)scheduledWrite.Parameter);
                }
            }
        }

        private void WriteBuffer(IoBuffer buf)
        {
            IWriteRequest writeRequest;
            if (_authenticated)
                writeRequest = new SslFilter.EncryptedWriteRequest(_currentWriteRequest, buf);
            else
                writeRequest = new DefaultWriteRequest(buf);
            _currentNextFilter.FilterWrite(_session, writeRequest);
        }

        private IoBuffer ReadBuffer()
        {
            IoBuffer buf = IoBuffer.Allocate(_sessionStream.Remaining);

            while (true)
            {
                ArraySegment<Byte> array = buf.GetRemaining();
                Int32 bytesRead = _sslStream.Read(array.Array, array.Offset, array.Count);
                if (bytesRead <= 0)
                    break;
                buf.Position += bytesRead;

                if (_sessionStream.Remaining == 0)
                    break;
                else
                {
                    // We have to grow the target buffer, it's too small.
                    buf.Capacity <<= 1;
                    buf.Limit = buf.Capacity;
                }
            }

            buf.Flip();
            return buf;
        }

        class IoSessionStream : System.IO.Stream
        {
            readonly Object _syncRoot = new Byte[0];
            readonly SslHandler _sslHandler;
            readonly IoBuffer _buf;
            volatile Boolean _closed;
            volatile Boolean _released;
            IOException _exception;

            public IoSessionStream(SslHandler sslHandler)
            {
                _sslHandler = sslHandler;
                _buf = IoBuffer.Allocate(16);
                _buf.AutoExpand = true;
                _buf.Limit = 0;
            }

            public override Int32 ReadByte()
            {
                lock (_syncRoot)
                {
                    if (!WaitForData())
                        return 0;
                    return _buf.Get() & 0xff;
                }
            }

            public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count)
            {
                lock (_syncRoot)
                {
                    if (!WaitForData())
                        return 0;

                    Int32 readBytes = Math.Min(count, _buf.Remaining);
                    _buf.Get(buffer, offset, readBytes);
                    return readBytes;
                }
            }

            public override void Close()
            {
                base.Close();

                if (_closed)
                    return;

                lock (_syncRoot)
                {
                    _closed = true;
                    ReleaseBuffer();
                    Monitor.PulseAll(_syncRoot);
                }
            }

            public override void Write(Byte[] buffer, Int32 offset, Int32 count)
            {
                _sslHandler.WriteBuffer(IoBuffer.Wrap((Byte[])buffer.Clone(), offset, count));
            }

            public override void WriteByte(Byte value)
            {
                IoBuffer buf = IoBuffer.Allocate(1);
                buf.Put(value);
                buf.Flip();
                _sslHandler.WriteBuffer(buf);
            }

            public override void Flush()
            { }

            public void Write(IoBuffer buf)
            {
                if (_closed)
                    return;

                lock (_syncRoot)
                {
                    if (_buf.HasRemaining)
                    {
                        _buf.Compact().Put(buf).Flip();
                    }
                    else
                    {
                        _buf.Clear().Put(buf).Flip();
                        Monitor.PulseAll(_syncRoot);
                    }
                }
            }

            private Boolean WaitForData()
            {
                if (_released)
                    return false;

                lock (_syncRoot)
                {
                    while (!_released && _buf.Remaining == 0 && _exception == null)
                    {
                        try
                        {
                            Monitor.Wait(_syncRoot);
                        }
                        catch (ThreadInterruptedException e)
                        {
                            throw new IOException("Interrupted while waiting for more data", e);
                        }
                    }
                }

                if (_exception != null)
                {
                    ReleaseBuffer();
                    throw _exception;
                }

                if (_closed && _buf.Remaining == 0)
                {
                    ReleaseBuffer();
                    return false;
                }

                return true;
            }

            private void ReleaseBuffer()
            {
                if (_released)
                    return;
                _released = true;
            }

            public IOException Exception
            {
                set
                {
                    if (_exception == null)
                    {
                        lock (_syncRoot)
                        {
                            _exception = value;
                            Monitor.PulseAll(_syncRoot);
                        }
                    }
                }
            }

            public Int32 Remaining
            {
                get { return _buf.Remaining; }
            }

            public override Boolean CanRead
            {
                get { return true; }
            }

            public override Boolean CanSeek
            {
                get { return false; }
            }

            public override Boolean CanWrite
            {
                get { return true; }
            }

            public override Int64 Length
            {
                get { throw new NotSupportedException(); }
            }

            public override Int64 Position
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }

            public override Int64 Seek(Int64 offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(Int64 value)
            {
                throw new NotSupportedException();
            }
        }
    }
}
