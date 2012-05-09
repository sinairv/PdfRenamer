using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PdfRenamer
{
    public class StringUtils
    {
        private static readonly Dictionary<string, string> dicLigatures = new Dictionary<string, string>
            {
                { "\u00C6", "AE" }, { "\u00E6", "ae" }, 
                { "\u0152", "OE" }, { "\u0153", "oe" },
                { "\u0132", "IJ" }, { "\u1D6B", "ue" },
                { "\uFB00", "ff" }, { "\u0133", "ij" },
                { "\uFB01", "fi" }, { "\uFB02", "fl" },
                { "\uFB03", "ffi" }, { "\uFB04", "ffl" },
                { "\uFB05", "ſt" }, { "\uFB06", "st" },
                { "\u0238", "db" }, { "\u0239", "qp" },
                { "\u02A3", "dz" }, { "\u02A5", "dʑ" },
                { "\u02A4", "dʒ" }, { "\u02A9", "fŋ" },
                { "\u02AA", "ls" }, { "\u02AB", "lz" },
                { "\u026E", "lʒ" }, { "\u02A8", "tɕ" },
                { "\u02A6", "ts" }, { "\u02A7", "tʃ" },
            };

        private static readonly Dictionary<string, string> dicUnifications = new Dictionary<string, string>
            {
                {"”","\""}, {"’", "'"}, {"∗", "*"}, {"\u2029", " "}, {"\u00A0"," " }, 
                {"\u00AD", "-"}, {"´ ","e"}, {"´","e"}, {"–", "-"}, 
                {"†", ""}, {"‡",""}, {"●", " "}, {"—","-"}, {"✩", ""}, {"˜ ",""}, {"\u0003",""},
                {"ý", ""}, {"þ", ""}, {"Ý", ""}, {"Þ", ""}
            };

        public static string UnifyString(string str)
        {
            var sb = new StringBuilder(str);
            foreach (var strKey in dicUnifications.Keys)
            {
                sb.Replace(strKey, dicUnifications[strKey]);
            }
            return sb.ToString();
        }

        public static string RemoveLigatures(string str)
        {
            var sb = new StringBuilder(str);
            foreach (var strKey in dicLigatures.Keys)
            {
                sb.Replace(strKey, dicLigatures[strKey]);
            }
            return sb.ToString();
        }

        public static string GetVisibleFileContent(string fileName)
        {
            string content = File.ReadAllText(fileName);
            var lines = ExtractParagraphs(content, true);

            var sbVisibleContent = new StringBuilder();
            foreach (var line in lines)
            {
                sbVisibleContent.AppendLine(MakeStringVisible(line));
            }

            return sbVisibleContent.ToString();
        }


        public static List<string> ExtractParagraphs(string text, bool returnDelimiters)
        {
            var lstPatternInfos = new List<string>();

            int startPar = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char ch0 = text[i];
                if (ch0 == '\r' || ch0 == '\n')
                {
                    string curPar = text.Substring(startPar, i - startPar);
                    if (curPar.Length > 0)
                        lstPatternInfos.Add(curPar);
                    startPar = i;

                    string curNewLine = ch0.ToString();
                    char ch1;
                    if ((i + 1 < text.Length) && (((ch1 = text[i + 1]) == '\r') || ch1 == '\n') && (ch1 != ch0))
                    {
                        curNewLine += ch1;
                        lstPatternInfos.Add(curNewLine);
                        startPar += 2;
                        i++; // skip processing next char
                    }
                    else
                    {
                        lstPatternInfos.Add(curNewLine);
                        startPar++;
                    }
                }
            }

            string lastPar = text.Substring(startPar);
            if (lastPar.Length > 0)
                lstPatternInfos.Add(lastPar);

            return lstPatternInfos;
        }

        public static bool StringContainsWord(string str, string word)
        {
            string regexPat = @"\b" + word + @"\b";
            var m = Regex.Match(str, regexPat);
            return m != null && m.Success;
        }

        //public static void RemoveNonLetterChars(string str)
        //{
        //    var sb = new StringBuilder(str.Length);
        //    int len = str.Length;
        //    for(int i = 0; i < len; i++)
        //    {
        //        char ch = str[i];
        //        if(Char.IsWhiteSpace(ch) || Char.IsLetter(ch))
        //        {
                    
        //        }
        //    }

        //}

        /// <summary>
        /// Returns a visible version of the string, by making
        /// its whitespace and control characters visible
        /// using well known escape sequences, or the equivalant 
        /// hexa decimal value.
        /// </summary>
        /// <param name="str">The string to be made visible.</param>
        /// <returns></returns>
        public static string MakeStringVisible(string str)
        {
            if (str == null)
                return "{null}";

            var sb = new StringBuilder();

            foreach (char ch in str)
            {
                if (Char.IsControl(ch) || Char.IsWhiteSpace(ch))
                {
                    switch (ch)
                    {
                        case '\a':
                            sb.Append("\\a");
                            break;
                        case ' ':
                            sb.Append("\\s");
                            break;
                        case '\n':
                            sb.Append("\\n");
                            break;
                        case '\r':
                            sb.Append("\\r");
                            break;
                        case '\t':
                            sb.Append("\\t");
                            break;
                        case '\v':
                            sb.Append("\\v");
                            break;
                        case '\f':
                            sb.Append("\\f");
                            break;
                        default:
                            sb.AppendFormat("\\u{0:X};", (int)ch);
                            break;
                    }
                }
                else if (IsHalfSpace(ch))
                {
                    sb.Append("\\z");
                }
                else
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Determines whether the specified character is a half-space character.
        /// </summary>
        /// <param name="ch">The character</param>
        /// <returns>
        /// 	<c>true</c> if the specified character is a half-space character; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsHalfSpace(char ch)
        {
            switch (ch)
            {
                case '\u200B':
                case '\u200C':
                case '\u00AC':
                case '\u00AD':
                case '\u001F':
                case '\u200D':
                case '\u200E':
                case '\u200F':
                    return true;
            }
            return false;
        }

        private static TextInfo s_textInfo = new CultureInfo("en-US", false).TextInfo;


        public static string ToTitleCase(string str)
        {
            return s_textInfo.ToTitleCase(str);
        }
    }
}
