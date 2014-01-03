using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Mina.Core.Session;

namespace Mina.Core.Service
{
    public abstract class AbstractIoAcceptor : AbstractIoService, IoAcceptor
    {
        private readonly List<EndPoint> _defaultLocalEndPoints = new List<EndPoint>();
        private readonly List<EndPoint> _boundEndPoints = new List<EndPoint>();
        private Boolean _disconnectOnUnbind = true;

        public AbstractIoAcceptor(IoSessionConfig sessionConfig)
            : base(sessionConfig)
        { }

        public Boolean CloseOnDeactivation
        {
            get { return _disconnectOnUnbind; }
            set { _disconnectOnUnbind = value; }
        }

        public IEnumerable<EndPoint> DefaultLocalEndPoints
        {
            get { return _defaultLocalEndPoints; }
            set
            {
                lock (((ICollection)_boundEndPoints).SyncRoot)
                {
                    if (_boundEndPoints.Count > 0)
                        throw new InvalidOperationException("LocalEndPoints can't be set while the acceptor is bound.");

                    _defaultLocalEndPoints.Clear();
                    _defaultLocalEndPoints.AddRange(value);
                }
            }
        }

        public EndPoint DefaultLocalEndPoint
        {
            get { return _defaultLocalEndPoints.Count == 0 ? null : _defaultLocalEndPoints[0]; }
            set
            {
                lock (((ICollection)_boundEndPoints).SyncRoot)
                {
                    if (_boundEndPoints.Count > 0)
                        throw new InvalidOperationException("LocalEndPoint can't be set while the acceptor is bound.");

                    _defaultLocalEndPoints.Clear();
                    _defaultLocalEndPoints.Add(value);
                }
            }
        }

        public IEnumerable<EndPoint> LocalEndPoints
        {
            get { return _boundEndPoints; }
        }

        public EndPoint LocalEndPoint
        {
            get { return _boundEndPoints.Count == 0 ? null : _boundEndPoints[0]; }
        }

        public void Bind()
        {
            Bind(DefaultLocalEndPoints);
        }

        public void Bind(EndPoint localEP)
        {
            if (localEP == null)
                throw new ArgumentNullException("localEP");
            Bind(new EndPoint[] { localEP });
        }

        public void Bind(params EndPoint[] localEndPoints)
        {
            if (localEndPoints.Length == 0)
                Bind(DefaultLocalEndPoints);
            else
                Bind((IEnumerable<EndPoint>)localEndPoints);
        }

        public void Bind(IEnumerable<EndPoint> localEndPoints)
        {
            if (localEndPoints == null)
                throw new ArgumentNullException("localEndPoints");

            Boolean active = false;
            lock (((ICollection)_boundEndPoints).SyncRoot)
            {
                if (_boundEndPoints.Count == 0)
                    active = true;

                IEnumerable<EndPoint> eps = BindInternal(localEndPoints);
                _boundEndPoints.AddRange(eps);
            }

            if (active)
                ((IoServiceSupport)this).FireServiceActivated();
        }

        public void Unbind()
        {
            Unbind(LocalEndPoints);
        }

        public void Unbind(EndPoint localEP)
        {
            if (localEP == null)
                throw new ArgumentNullException("localEP");
            Unbind(new EndPoint[] { localEP });
        }

        public void Unbind(params EndPoint[] localEndPoints)
        {
            Unbind((IEnumerable<EndPoint>)localEndPoints);
        }

        public void Unbind(IEnumerable<EndPoint> localEndPoints)
        {
            if (localEndPoints == null)
                throw new ArgumentNullException("localEndPoints");

            Boolean deactivate = false;
            lock (((ICollection)_boundEndPoints).SyncRoot)
            {
                if (_boundEndPoints.Count == 0)
                    return;

                try
                {
                    UnbindInternal(localEndPoints);
                }
                catch
                {
                    throw;
                }

                if (_boundEndPoints == localEndPoints)
                {
                    _boundEndPoints.Clear();
                }
                else
                {
                    foreach (EndPoint ep in localEndPoints)
                    {
                        _boundEndPoints.Remove(ep);
                    }
                }

                if (_boundEndPoints.Count == 0)
                    deactivate = true;
            }

            if (deactivate)
                ((IoServiceSupport)this).FireServiceDeactivated();
        }

        protected abstract IEnumerable<EndPoint> BindInternal(IEnumerable<EndPoint> localEndPoints);
        protected abstract void UnbindInternal(IEnumerable<EndPoint> localEndPoints);
    }
}
