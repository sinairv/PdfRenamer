using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Diagnostics;

namespace PdfToTextViewer
{
    [Flags]
    public enum WordTokenizerOptions
    {
        None = 0x0,
        ReturnPunctuations = 0x1,
        ReturnWhitespaces = 0x2,
        /// <summary>
        /// Returns whitespace chunks character by character instead of returning them all in a single token.
        /// </summary>
        ReturnWhitespacesCharacterByCharacter = 0x4,
        TreatNumberArabicCharCombinationAsOneWords = 0x8,
        TreatNumberNonArabicCharCombinationAsOneWords = 0x10,
        TreatArabicNonArabicCharCombinationAsOneWords = 0x11
    }

    /// <summary>
    /// A general purpose (English, or Persian), customizable, and fast word tokenizer
    /// </summary>
    public class WordTokenizer
    {
        private enum CharCats
        {
            AlphaArabic, AlphaNonArabic, Digit, Punctuation, WhiteSpace
        }

        private readonly bool m_retPuncs;
        private readonly bool m_retWs;
        private readonly bool m_retWsCharByChar;
        private readonly bool m_isNumAr1Word;
        private readonly bool m_isNumNonAr1Word;
        private readonly bool m_isArNonAr1Word;

        public WordTokenizer(WordTokenizerOptions options)
        {
            // read options
            m_retPuncs = IsFlagOn(options, WordTokenizerOptions.ReturnPunctuations);
            m_retWs = IsFlagOn(options, WordTokenizerOptions.ReturnWhitespaces);
            m_retWsCharByChar = IsFlagOn(options, WordTokenizerOptions.ReturnWhitespacesCharacterByCharacter);
            m_isNumAr1Word = IsFlagOn(options, WordTokenizerOptions.TreatNumberArabicCharCombinationAsOneWords);
            m_isNumNonAr1Word = IsFlagOn(options, WordTokenizerOptions.TreatNumberNonArabicCharCombinationAsOneWords);
            m_isArNonAr1Word = IsFlagOn(options, WordTokenizerOptions.TreatArabicNonArabicCharCombinationAsOneWords);
        }

        public IEnumerable<TokenInfo> Tokenize(string line)
        {
            return Tokenize(line, 0);
        }

        private static bool CanHappenInEnglishTransliteration(char ch)
        {
            return StringUtil.IsSingleQuote(ch) || ch == '@';
        }

        public IEnumerable<TokenInfo> Tokenize(string line, int fromChar)
        {
            // check exceptional conditions
            if(String.IsNullOrEmpty(line) || fromChar >= line.Length)
            {
                yield break;
            }
            
            // now start reading words
            char ch0 = line[fromChar];
            var charCat0 = GetCharCat(ch0);

            int startIndex = fromChar;
            for (int i = fromChar + 1; i < line.Length; i++)
            {
                char ch1 = line[i];
                var charCat1 = GetCharCat(ch1);

                if(charCat0 != charCat1)
                {
                    // make this flag true, the upcoming rules have the chance to turn it off
                    bool isNewWordMet = true;

                    // apostraphe is considered inside word if preceeded by an english letter
                    if (CanHappenInEnglishTransliteration(ch1) && charCat0 == CharCats.AlphaNonArabic)
                    {
                        charCat1 = CharCats.AlphaNonArabic;
                        isNewWordMet = false;
                    }
                    else if(charCat1 == CharCats.Digit && (ch0 == '+' || ch0 == '-') &&
                        (i <= 1 || (i >= 2 && Char.IsWhiteSpace(line[i - 2]))))
                    {
                        isNewWordMet = false;
                    }
                    // decimal separator after a digit or + or - sign is considered as in number
                    else if(StringUtil.IsDecimalSeparator(ch1) && (charCat0 == CharCats.Digit || ch0 == '+' || ch0 == '-'))
                    {
                        charCat1 = CharCats.Digit;
                        isNewWordMet = false;
                    }
                    // decimal separator before a digit is considered as in number
                    else if (StringUtil.IsDecimalSeparator(ch1) && i < line.Length - 1 && Char.IsDigit(line[i + 1]))
                    {
                        charCat1 = CharCats.Digit;
                        // but do not turn off isNewWordMet
                    }
                    else if((ch1 == 'e' || ch1 == 'E') && charCat0 == CharCats.Digit)
                    {
                        charCat1 = CharCats.Digit;
                        isNewWordMet = false;
                    }
                    else if (m_isNumAr1Word && charCat1 == CharCats.Digit && charCat0 == CharCats.AlphaArabic)
                    {
                        isNewWordMet = false;
                        charCat1 = CharCats.AlphaArabic;
                    }
                    else if (m_isNumAr1Word && charCat1 == CharCats.AlphaArabic && charCat0 == CharCats.Digit)
                    {
                        isNewWordMet = false;
                        // no need to change the character category 1
                    }
                    else if (m_isNumNonAr1Word && charCat1 == CharCats.Digit && charCat0 == CharCats.AlphaNonArabic)
                    {
                        isNewWordMet = false;
                        charCat1 = CharCats.AlphaNonArabic;
                    }
                    else if (m_isNumNonAr1Word && charCat1 == CharCats.AlphaNonArabic && charCat0 == CharCats.Digit)
                    {
                        isNewWordMet = false;
                        // no need to change the character category 1
                    }
                    else if(m_isArNonAr1Word && charCat1 == CharCats.AlphaArabic && charCat0 == CharCats.AlphaNonArabic)
                    {
                        isNewWordMet = false;
                        // do not change character category
                    }
                    else if(m_isArNonAr1Word && charCat1 == CharCats.AlphaNonArabic && charCat0 == CharCats.AlphaArabic)
                    {
                        isNewWordMet = false;
                        // do not change character category
                    }

                    if (isNewWordMet)
                    {
                        string wordContent = line.Substring(startIndex, i - startIndex);

                        // NOTE: this condition is exactly duplicated in a few lines later
                        if (!((!m_retPuncs && charCat0 == CharCats.Punctuation) ||
                            (!m_retWs && charCat0 == CharCats.WhiteSpace)))
                        {
                            if (charCat0 == CharCats.WhiteSpace && m_retWsCharByChar)
                            {
                                int cicount = wordContent.Length;
                                for(int ci = 0; ci < cicount; ci++)
                                {
                                    yield return new TokenInfo(wordContent[ci].ToString(), startIndex + ci);
                                }
                            }
                            else
                            {
                                yield return new TokenInfo(wordContent, startIndex);
                            }
                        }

                        startIndex = i;
                    }
                }

                ch0 = ch1;
                charCat0 = charCat1;
            }

            // NOTE: this condition is exactly duplicated in a few lines earlier
            string lastWordContent = line.Substring(startIndex);
            if (!((!m_retPuncs && charCat0 == CharCats.Punctuation) ||
                (!m_retWs && charCat0 == CharCats.WhiteSpace)))
            {
                if (charCat0 == CharCats.WhiteSpace && m_retWsCharByChar)
                {
                    int cicount = lastWordContent.Length;
                    for (int ci = 0; ci < cicount; ci++)
                    {
                        yield return new TokenInfo(lastWordContent[ci].ToString(), startIndex + ci);
                    }
                }
                else
                {
                    yield return new TokenInfo(lastWordContent, startIndex);
                }
            }
        }

        private static CharCats GetCharCat(char ch)
        {
            if (StringUtil.IsInArabicWord(ch))
                return CharCats.AlphaArabic;
            else if (Char.IsLetter(ch))
                return CharCats.AlphaNonArabic;
            else if (Char.IsDigit(ch))
                return CharCats.Digit;
            else if (Char.IsSymbol(ch) || Char.IsPunctuation(ch))
                return CharCats.Punctuation;
            else if (Char.IsControl(ch) || Char.IsWhiteSpace(ch))
                return CharCats.WhiteSpace;

            //Debug.Assert(false, "Unknown character category in word-tokenizer!");
            return CharCats.WhiteSpace;
        }

        private static bool IsFlagOn(WordTokenizerOptions allFlags, WordTokenizerOptions singleFlag)
        {
            var f = (int) singleFlag;
            var r = (int) allFlags & f;
            return (r == f);
        }
    }
}
