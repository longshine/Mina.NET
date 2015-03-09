namespace Mina.Util
{
    /// <summary>
    /// Interlock Utils
    /// </summary>
    public static class InterlockedUtil
    {

        /*****************************************************************
         * CompareExchange<T>
         * 
         * if mina.net.unity3d run in ios environment
         * can't cross compiler for System.Threading.Interlocked.CompareExchange<T>
         * because System.Threading.Interlocked.CompareExchange<T> use JIT compiler.
         *****************************************************************/
        public static T CompareExchange<T>(ref T location1, T value, T comparand) where T : class
        {
#if UNITY
            var result = location1;// return value
            if (location1 == comparand)
                location1 = value;
            return result;
#else
            return System.Threading.Interlocked.CompareExchange(ref location1, value, comparand);
#endif
        }
    }
}
