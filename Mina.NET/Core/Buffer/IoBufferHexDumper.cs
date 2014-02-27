using System;
using System.Text;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// Provides utility methods to dump an <see cref="IoBuffer"/> into a hex formatted string.
    /// </summary>
    static class IoBufferHexDumper
    {
        private static readonly Char[] highDigits;
        private static readonly Char[] lowDigits;

        static IoBufferHexDumper()
        {
            Char[] digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

            Char[] high = new Char[256];
            Char[] low = new Char[256];

            for (Int32 i = 0; i < 256; i++)
            {
                high[i] = digits[i >> 4];
                low[i] = digits[i & 0x0F];
            }

            highDigits = high;
            lowDigits = low;
        }

        public static String GetHexdump(IoBuffer buf, Int32 lengthLimit)
        {
            if (lengthLimit <= 0)
                throw new ArgumentException("lengthLimit: " + lengthLimit + " (expected: 1+)");
            Boolean truncate = buf.Remaining > lengthLimit;
            Int32 size = truncate ? lengthLimit : buf.Remaining;

            if (size == 0)
                return "empty";

            StringBuilder sb = new StringBuilder(size * 3 + 3);
            Int32 oldPos = buf.Position;

            // fill the first
            Int32 byteValue = buf.Get() & 0xFF;
            sb.Append((char)highDigits[byteValue]);
            sb.Append((char)lowDigits[byteValue]);
            size--;

            // and the others, too
            for (; size > 0; size--)
            {
                sb.Append(' ');
                byteValue = buf.Get() & 0xFF;
                sb.Append((char)highDigits[byteValue]);
                sb.Append((char)lowDigits[byteValue]);
            }

            buf.Position = oldPos;

            if (truncate)
                sb.Append("...");

            return sb.ToString();
        }
    }
}
