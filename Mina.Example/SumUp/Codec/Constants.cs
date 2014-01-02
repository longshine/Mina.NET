namespace Mina.Example.SumUp.Codec
{
    static class Constants
    {
        public static readonly int TYPE_LEN = 2;

        public static readonly int SEQUENCE_LEN = 4;

        public static readonly int HEADER_LEN = TYPE_LEN + SEQUENCE_LEN;

        public static readonly int BODY_LEN = 12;

        public static readonly int RESULT = 0;

        public static readonly int ADD = 1;

        public static readonly int RESULT_CODE_LEN = 2;

        public static readonly int RESULT_VALUE_LEN = 4;

        public static readonly int ADD_BODY_LEN = 4;

        public static readonly int RESULT_OK = 0;

        public static readonly int RESULT_ERROR = 1;
    }
}
