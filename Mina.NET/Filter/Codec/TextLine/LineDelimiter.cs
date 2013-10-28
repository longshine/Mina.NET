using System;
using System.Text;

namespace Mina.Filter.Codec.TextLine
{
    /// <summary>
    /// A delimiter which is appended to the end of a text line, such as
    /// <tt>CR/LF</tt>. This class defines default delimiters for various OS.
    /// </summary>
    public class LineDelimiter
    {
        /// <summary>
        /// The line delimiter constant of the current O/S.
        /// </summary>
        public static readonly LineDelimiter Default = new LineDelimiter(Environment.NewLine);

        /// <summary>
        /// A special line delimiter which is used for auto-detection of
        /// EOL in <see cref="TextLineDecoder"/>.  If this delimiter is used,
        /// <see cref="TextLineDecoder"/> will consider both  <tt>'\r'</tt> and
        /// <tt>'\n'</tt> as a delimiter.
        /// </summary>
        public static readonly LineDelimiter Auto = new LineDelimiter(String.Empty);

        /// <summary>
        /// The CRLF line delimiter constant (<tt>"\r\n"</tt>)
        /// </summary>
        public static readonly LineDelimiter CRLF = new LineDelimiter("\r\n");

        /// <summary>
        /// The line delimiter constant of UNIX (<tt>"\n"</tt>)
        /// </summary>
        public static readonly LineDelimiter Unix = new LineDelimiter("\n");

        /// <summary>
        /// The line delimiter constant of MS Windows/DOS (<tt>"\r\n"</tt>)
        /// </summary>
        public static readonly LineDelimiter Windows = CRLF;

        /// <summary>
        /// The line delimiter constant of Mac OS (<tt>"\r"</tt>)
        /// </summary>
        public static readonly LineDelimiter Mac = new LineDelimiter("\r");

        /// <summary>
        /// The line delimiter constant for NUL-terminated text protocols
        /// such as Flash XML socket (<tt>"\0"</tt>)
        /// </summary>
        public static readonly LineDelimiter NUL = new LineDelimiter("\0");

        private readonly String _value;

        /// <summary>
        /// Creates a new line delimiter with the specified <tt>delimiter</tt>.
        /// </summary>
        public LineDelimiter(String value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            _value = value;
        }

        /// <summary>
        /// Gets the delimiter string.
        /// </summary>
        public String Value
        {
            get { return _value; }
        }

        public override Int32 GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override Boolean Equals(Object obj)
        {
            if (Object.ReferenceEquals(this, obj))
                return true;
            LineDelimiter that = obj as LineDelimiter;
            if (that == null)
                return false;
            else
                return this._value.Equals(that._value);
        }

        public override String ToString()
        {
            if (_value.Length == 0)
                return "delimiter: auto";
            else
            {
                StringBuilder buf = new StringBuilder();
                buf.Append("delimiter:");

                for (Int32 i = 0; i < _value.Length; i++)
                {
                    buf.Append(" 0x");
                    buf.AppendFormat("{0:X}", _value[i]);
                }

                return buf.ToString();
            }
        }
    }
}
