using System;
using System.Collections.Generic;
using System.Text;

namespace PdfRenamer
{
    public class PdfExtractedData
    {
        public List<string> AuthorNames = null;
        public string AuthorsLine = null;
        public string Title = null;
        public int Year = -1;
        public string PubName = null;
        public List<string> Dois = null;

        public bool IsTitleInValid()
        {
            if (String.IsNullOrWhiteSpace(Title))
                return true;

            // if title contains 3 consequtive spaces it is invalid
            if (Title.Contains("   "))
                return true;

            foreach (char c in Title)
                if (Char.IsControl(c))
                    return true;

            return false;
        }


        public void CleanContent()
        {
            Title = Title.Trim();
            if (!String.IsNullOrEmpty(Title))
            {
                if (!ContainsLowerCase(Title))
                {
                    Title = StringUtils.ToTitleCase(Title.ToLower());
                }

                int endi = Title.Length - 1;
                // * should be treated differently because it may be legal in the names e.g., the A* algorithm
                if (endi >= 1 && Title[endi] == '*' && Char.IsWhiteSpace(Title[endi - 1]))
                    Title = Title.Substring(0, endi).Trim();
                else if(Char.IsPunctuation(Title[endi]) || Char.IsSymbol(Title[endi]))
                    Title = Title.Substring(0, endi).Trim();
            }

            if (AuthorNames != null && AuthorNames.Count > 0)
            {
                string authStr = AuthorsToString;
                if(!ContainsLowerCase(authStr))
                    m_authorsString = StringUtils.ToTitleCase(authStr.ToLower());
            }

            if (!String.IsNullOrEmpty(PubName))
            {
                if (IsOneWord(PubName))
                    PubName = PubName.ToUpper();
                else
                {
                    if (!ContainsLowerCase(PubName))
                    {
                        PubName = StringUtils.ToTitleCase(PubName.ToLower());
                    }
                }
            }
        }

        private static bool IsOneWord(string str)
        {
            return str.Split(new [] {' ', '\t', '\n', '\r'}, StringSplitOptions.RemoveEmptyEntries).Length <= 1;
        }

        private static bool ContainsLowerCase(string str)
        {
            foreach (char ch in str)
                if ('a' <= ch && ch <= 'z')
                    return true;

            return false;
        }


        public override string ToString()
        {
            var sb = new StringBuilder();

            if(!(IsTitleInValid() && Dois != null && Dois.Count > 0))
            {
                sb.AppendFormat("Title: {0}", Title).AppendLine();
                sb.AppendFormat("Authors: {0}", AuthorsToString).AppendLine();

                if (Year > 0)
                    sb.AppendFormat("Year: {0}", Year).AppendLine();

                if (!String.IsNullOrWhiteSpace(PubName))
                    sb.AppendFormat("PubName: {0}", PubName).AppendLine();
            }
            
            if (Dois != null)
            {
                foreach (var doi in Dois)
                {
                    sb.AppendFormat("DOI: {0}", doi).AppendLine();
                }
            }

            return sb.ToString();
        }

        private string m_authorsString = null;

        public string AuthorsToString
        {
            get
            {
                if (m_authorsString != null)
                {
                    return m_authorsString;
                }

                m_authorsString = "";
                if (AuthorNames != null)
                {
                    bool isFirst = true;
                    foreach (var name in AuthorNames)
                    {
                        if (isFirst)
                        {
                            isFirst = !isFirst;
                        }
                        else
                        {
                            m_authorsString += " - ";
                        }

                        m_authorsString += name;
                    }
                }
                else
                {
                    m_authorsString= "";
                }

                return m_authorsString;
            }
        }

    }
}
