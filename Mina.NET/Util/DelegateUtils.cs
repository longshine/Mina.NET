using System;

namespace Mina.Util
{
    public static class DelegateUtils
    {
        public static void SaveInvoke(EventHandler handler, Object sender)
        {
            if (handler != null)
            {
                foreach (Delegate d in handler.GetInvocationList())
                {
                    try
                    {
                        ((EventHandler)d)(sender, EventArgs.Empty);
                    }
                    catch (Exception e)
                    {
                        ExceptionMonitor.Instance.ExceptionCaught(e);
                    }
                }
            }
        }

        public static void SaveInvoke<TEventArgs>(EventHandler<TEventArgs> handler, Object sender, TEventArgs e)
            where TEventArgs : EventArgs
        {
            if (handler != null)
            {
                foreach (Delegate d in handler.GetInvocationList())
                {
                    try
                    {
                        ((EventHandler<TEventArgs>)d)(sender, e);
                    }
                    catch (Exception ex)
                    {
                        ExceptionMonitor.Instance.ExceptionCaught(ex);
                    }
                }
            }
        }

        public static void SaveInvoke(Action act)
        {
            if (act != null)
            {
                foreach (Delegate d in act.GetInvocationList())
                {
                    try
                    {
                        ((Action)d)();
                    }
                    catch (Exception e)
                    {
                        ExceptionMonitor.Instance.ExceptionCaught(e);
                    }
                }
            }
        }

        public static void SaveInvoke<T>(Action<T> act, T obj)
        {
            if (act != null)
            {
                foreach (Delegate d in act.GetInvocationList())
                {
                    try
                    {
                        ((Action<T>)d)(obj);
                    }
                    catch (Exception e)
                    {
                        ExceptionMonitor.Instance.ExceptionCaught(e);
                    }
                }
            }
        }

        public static void SaveInvoke<T1, T2>(Action<T1, T2> act, T1 obj1, T2 obj2)
        {
            if (act != null)
            {
                foreach (Delegate d in act.GetInvocationList())
                {
                    try
                    {
                        ((Action<T1, T2>)d)(obj1, obj2);
                    }
                    catch (Exception e)
                    {
                        ExceptionMonitor.Instance.ExceptionCaught(e);
                    }
                }
            }
        }

        public static void SaveInvoke<T1, T2, T3>(Action<T1, T2, T3> act, T1 obj1, T2 obj2, T3 obj3)
        {
            if (act != null)
            {
                foreach (Delegate d in act.GetInvocationList())
                {
                    try
                    {
                        ((Action<T1, T2, T3>)d)(obj1, obj2, obj3);
                    }
                    catch (Exception e)
                    {
                        ExceptionMonitor.Instance.ExceptionCaught(e);
                    }
                }
            }
        }
    }
}
