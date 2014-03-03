using System;

namespace Mina.Example.Haiku
{
    class HaikuValidator
    {
        private static readonly int[] SYLLABLE_COUNTS = { 5, 7, 5 };

        public void Validate(Haiku haiku)
        {
            String[] phrases = haiku.Phrases;

            for (int i = 0; i < phrases.Length; i++)
            {
                String phrase = phrases[i];
                int count = PhraseUtilities.CountSyllablesInPhrase(phrase);

                if (count != SYLLABLE_COUNTS[i])
                {
                    throw new InvalidHaikuException(i + 1, phrase, count,
                            SYLLABLE_COUNTS[i]);
                }
            }
        }
    }
}
