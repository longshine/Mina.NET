using System;

namespace Mina.Example.Haiku
{
    class InvalidHaikuException : Exception
    {
        private readonly int position;
        private readonly String phrase;
        private readonly int syllableCount;
        private readonly int expectedSyllableCount;

        public InvalidHaikuException(int position, String phrase,
                int syllableCount, int expectedSyllableCount)
            : base("phrase " + position + ", '" + phrase + "' had " + syllableCount
                        + " syllables, not " + expectedSyllableCount)
        {
            this.position = position;
            this.phrase = phrase;
            this.syllableCount = syllableCount;
            this.expectedSyllableCount = expectedSyllableCount;
        }

        public int ExpectedSyllableCount
        {
            get { return expectedSyllableCount; }
        }

        public String Phrase
        {
            get { return phrase; }
        }

        public int SyllableCount
        {
            get { return syllableCount; }
        }

        public int PhrasePositio
        {
            get { return position; }
        }
    }
}
