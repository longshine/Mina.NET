using System;

namespace Mina.Example.Haiku
{
    class Haiku
    {
        private readonly String[] _phrases;

        public Haiku(params String[] lines)
        {
            if (lines == null || lines.Length != 3)
                throw new ArgumentException("Must pass in 3 phrases of text");
            _phrases = lines;
        }

        public String[] Phrases
        {
            get { return _phrases; }
        }

        public override bool Equals(object obj)
        {
            if (Object.ReferenceEquals(obj, this))
                return true;

            Haiku haiku = obj as Haiku;
            if (haiku == null)
                return false;

            if (_phrases.Length != haiku._phrases.Length)
                return false;

            for (int i = 0; i < _phrases.Length; i++)
            {
                if (!String.Equals(_phrases[i], haiku._phrases[i]))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int result = 1;

            foreach (String s in _phrases)
                result = 31 * result + (s == null ? 0 : s.GetHashCode());

            return result;
        }

        public override string ToString()
        {
            return "[" + String.Join(", ", _phrases) + "]";
        }
    }
}
