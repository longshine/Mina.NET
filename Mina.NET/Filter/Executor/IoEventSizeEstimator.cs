using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Mina.Core.Buffer;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.Executor
{
    /// <summary>
    /// Estimates the amount of memory that the specified <see cref="IoEvent"/> occupies.
    /// </summary>
    public interface IoEventSizeEstimator
    {
        /// <summary>
        /// Estimate the IoEvent size in number of bytes.
        /// </summary>
        /// <param name="ioe">the event we want to estimate the size of</param>
        /// <returns>the estimated size of this event</returns>
        Int32 EstimateSize(IoEvent ioe);
    }

    class DefaultIoEventSizeEstimator : IoEventSizeEstimator
    {
        static readonly Dictionary<Type, Int32> _type2size = new Dictionary<Type, Int32>();

        static DefaultIoEventSizeEstimator()
        {
            _type2size[typeof(Boolean)] = sizeof(Boolean);
            _type2size[typeof(Boolean)] = sizeof(Boolean);
            _type2size[typeof(Boolean)] = sizeof(Boolean);
            _type2size[typeof(Boolean)] = sizeof(Boolean);
            _type2size[typeof(Boolean)] = sizeof(Boolean);
            _type2size[typeof(Boolean)] = sizeof(Boolean);
            _type2size[typeof(Boolean)] = sizeof(Boolean);
            _type2size[typeof(Boolean)] = sizeof(Boolean);
            _type2size[typeof(Boolean)] = sizeof(Boolean);
        }

        public Int32 EstimateSize(IoEvent ioe)
        {
            return EstimateSize((Object)ioe) + EstimateSize(ioe.Parameter);
        }

        private Int32 EstimateSize(Object obj)
        {
            if (obj == null)
                return 8;

            Int32 answer = 8 + EstimateSize(obj.GetType(), null);

            if (obj is IoBuffer)
                answer += ((IoBuffer)obj).Remaining;
            else if (obj is IWriteRequest)
                answer += EstimateSize(((IWriteRequest)obj).Message);
            else if (obj is String)
                answer += ((String)obj).Length << 1;
            else if (obj is IEnumerable)
                foreach (Object m in (IEnumerable)obj)
                {
                    answer += EstimateSize(m);
                }

            return Align(answer);
        }

        private Int32 EstimateSize(Type type, HashSet<Type> visitedTypes)
        {
            Int32 answer;

            if (_type2size.TryGetValue(type, out answer))
                return answer;

            if (visitedTypes == null)
                visitedTypes = new HashSet<Type>();
            else if (visitedTypes.Contains(type))
                return 0;

            visitedTypes.Add(type);

            answer = 8; // Basic overhead.

            for (Type t = type; t != null; t = t.BaseType)
            {
                FieldInfo[] fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                foreach (FieldInfo fi in fields)
                {
                    answer += EstimateSize(fi.FieldType, visitedTypes);
                }
            }

            visitedTypes.Remove(type);

            // Some alignment.
            answer = Align(answer);

            // Put the final answer.
            lock (((ICollection)_type2size).SyncRoot)
            {
                if (_type2size.ContainsKey(type))
                    answer = _type2size[type];
                else
                    _type2size[type] = answer;
            }

            return answer;
        }

        private static Int32 Align(Int32 size)
        {
            if (size % 8 != 0)
            {
                size /= 8;
                size++;
                size *= 8;
            }
            return size;
        }
    }
}
