using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PdfRenamer
{
    public class ContentRetreivalUtils
    {
        private static readonly Regex s_doiRegex = new Regex(@"(10\.(\d)+/(\S)+)", RegexOptions.Compiled);
        //10.2200/S00090ED1V01Y200705AIM001

        private static readonly string s_namePartPattern =
            @"((\p{Lu}\.)+)|(\b\p{Lu}\p{L}+(\-\p{L}+)*(\.)?)|(\b\p{Lu}\b)|(\bde\p{Lu}\p{L}+\b)";

        private static readonly string s_namePattern =
            @"(" + s_namePartPattern +  @")([ \f\t\v](" + s_namePartPattern + @"))+";

        private static readonly string s_nameGroupsPattern =
            "(?<name>" + s_namePattern + @")(\s*(\,|and)\s*(?<name>" + s_namePattern + "))*";

        private static readonly string s_emailGroupPattern =
            @"(\{)?\s*([\p{L}\p{N}\.\-_]+)(\s*\,\s*[\p{L}\p{N}\.\-_]+)*\s*(\})?\s*@\s*([\p{L}\p{N}\-_]+)\s*(\.\s*[\p{L}\p{N}\-_]+)+";
        private static readonly Regex s_nameRegex = new Regex(s_namePattern, RegexOptions.Compiled);

        private static readonly Regex s_nameEmailPairRegex = new Regex(@"(" + s_nameGroupsPattern + @")[^\{\p{L}]+(?<email>" + s_emailGroupPattern + ")", RegexOptions.Compiled);

        private static readonly Regex s_headerDateRegex = new Regex(@"\((?<year>\d{2,4})\)", RegexOptions.Compiled);

        private static readonly Regex s_headerConfRegex = new Regex(@"\((?<conf>\p{Lu}+\-?(?<year>\d{2,4}))\)", RegexOptions.Compiled);

        private static readonly Regex s_headerIeeeTransRegex = new Regex(@"^ieee\stransactions.+\w+\s(?<year>\d+)(\s+\d+)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);


        private static readonly Regex s_headerPageRangesRegex = new Regex(@"(?<num1>\d+)\-(?<num2>\d+)", RegexOptions.Compiled);



        #region File Name Patterns
        static readonly Regex s_fileConfYear = new Regex(@"\b(?<name>\p{L}{2,})(?<year>\d{2,4})\p{L}*", RegexOptions.Compiled);
        
        #endregion


        public static PdfExtractedData GetExtractedInfo(string pdfFilePath)
        {
            string firstPageContent = PdfReadingUtils.ReadPdfContent(pdfFilePath, 1);
            var firstPartData = BuildFirstPartPdfData(firstPageContent);

            PdfExtractedData pdfData = null;
            pdfData = ExtractFromUnknownPdf(firstPartData);

            if (pdfData == null || pdfData.IsTitleInValid())
            {
                string allFileContent = PdfReadingUtils.ReadPdfContent(pdfFilePath);
                var dois = FindDoi(allFileContent);
                if (dois != null && dois.Count > 0)
                {
                    pdfData = new PdfExtractedData { Dois = dois };
                }
            }

            if (pdfData != null) // || String.IsNullOrWhiteSpace(pdfData.Title))
            {
                string baseFileName = System.IO.Path.GetFileNameWithoutExtension(pdfFilePath);
                int fileNameYear;
                string fileNamePub;
                if(ExtractInfoFromFileName(baseFileName, out fileNameYear, out fileNamePub))
                {
                    if (pdfData.Year < 0)
                        pdfData.Year = fileNameYear;
                    if (String.IsNullOrWhiteSpace(pdfData.PubName))
                        pdfData.PubName = fileNamePub;
                }

                if (pdfData.IsTitleInValid() && pdfData.Dois == null)
                {
                    return null;
                }
                else
                {
                    return pdfData;
                }
            }
            
            return null;
        }

        private static bool ExtractInfoFromFileName(string baseFileName, out int fileNameYear, out string fileNamePub)
        {
            fileNameYear = -1;
            fileNamePub = null;
            if (baseFileName.IndexOf(' ') >= 0)
                return false;

            var m = s_fileConfYear.Match(baseFileName);
            if(m != null && m.Success)
            {
                fileNamePub = m.Groups["name"].Value.ToUpper();
                if (fileNamePub.Length > 6)
                    fileNamePub = null;
                int n;
                if(Int32.TryParse(m.Groups["year"].Value, out n))
                {
                    if (n < 50) n += 2000;
                    else if (n >= 50 && n < 100) n += 1900;
                    else if(n < 0 || n > 2999)
                    {
                        return false;
                    }

                    fileNameYear = n;
                }

                return true;
            }
            return false;
        }

        private static string ExtractPubNameFromHeader(string str)
        {
            string[] words = str.Split(new [] { ' ', '\t', '\v', '\f' }, StringSplitOptions.RemoveEmptyEntries);

            var sbInitials = new StringBuilder();
            int wstart = 0;
            if (words.Length > 2 && words[0].ToLower() == "journal" && words[1].ToLower() == "of")
            {
                sbInitials.Append("J");
                wstart = 2;
            }
            else if(words.Length >= 2 && words[0].ToLower().StartsWith("mach") && words[1].ToLower().StartsWith("learn"))
            {
                return "Machine Learning";
            }
            else
            {
                return null;
            }

            for (int i = wstart; i < words.Length; i++)
            {
                string curWord = words[i];
                if (curWord.Length <= 0)
                    continue;

                if (Char.IsDigit(words[i][0]))
                    break;

                if(!Char.IsLetter(words[i][0]))
                    break;

                sbInitials.Append(curWord[0].ToString().ToUpper());
            }

            if (sbInitials.Length > 0)
                return sbInitials.ToString();

            return null;
        }

        private static void ExtractHeaderInfo(string str, out int year, out string pubName)
        {
            pubName = null;
            year = -1;

            bool found = false;

            foreach (Match m in s_headerIeeeTransRegex.Matches(str))
            {
                if(m.Success)
                {
                    pubName = "IEEE Trans";
                    int y;
                    if (Int32.TryParse(m.Groups["year"].Value, out y))
                    {
                        year = y;
                    }

                    found = true;
                }
            }

            if (!found)
            {
                foreach (Match m in s_headerConfRegex.Matches(str))
                {
                    if (m.Success)
                    {
                        pubName = m.Groups["conf"].Value;
                        int y;
                        if (Int32.TryParse(m.Groups["year"].Value, out y))
                        {
                            year = y;
                        }

                        found = true;
                    }
                }
            }

            if (year >= 0)
            {
                if (year < 100)
                {
                    if (year > 50)
                        year += 1900;
                    else
                        year += 2000;
                }
            }

            if(pubName == null)
            {
                year = ExtractYearFromHeader(str);
                pubName = ExtractPubNameFromHeader(str);
            }
        }

        private static int ExtractYearFromHeader(string str)
        {
            foreach (Match m in s_headerDateRegex.Matches(str))
            {
                if (m.Success)
                {
                    int y;
                    if (Int32.TryParse(m.Groups["year"].Value, out y))
                    {
                        return y;
                    }
                }
            }
            return -1;
        }

        private static PdfExtractedData ExtractFromUnknownPdf(PdfFirstPartData firstPartData)
        {
            List<string> authorNamesRegular = null, authorNamesByEmail = null, tempAuthNames = null;
            bool isAuthsByEmail = false;
            bool isAuthSure;
            int year = -1;
            string pubName = null;
            int authorsLineNo = -1;
            int titleLineEnd = -1;
            int titleLineNo = -1;

            int firstNonEmptyLine = FirstNonEmptyLine(firstPartData.WordsPerLine, 0);
            if (firstNonEmptyLine < 0)
                return null;

            bool titleEndIsMet = false;
            titleLineEnd = titleLineNo;

            for (int i = firstNonEmptyLine; i < firstPartData.Count; i++)
            {
                if (firstPartData.WordsPerLine[i] == 0)
                {
                    if(titleLineNo >= 0) titleEndIsMet = true;
                    continue;
                }

                if (titleLineNo < 0 && (StringLooksLikePdfHeader(firstPartData.LineContents[i]) || 
                    (i+1 < firstPartData.Count && StringLooksLikePdfHeader(firstPartData.LineContents[i+1]))))
                {
                    ExtractHeaderInfo(firstPartData.LineContents[i], out year, out pubName);
                    // now skip headers and change i 
                    int j = i + 1;
                    for (; j < firstPartData.Count; j++)
                        if (firstPartData.WordsPerLine[j] == 0)
                            break;

                    if(j < firstPartData.Count) // i.e., loop breaked
                    {
                        titleLineNo = FirstNonEmptyLine(firstPartData.WordsPerLine, j);
                        i = titleLineNo;
                    }
                    else
                    {
                        break;
                    }
                }
                else if(titleLineNo < 0)
                {
                    titleLineNo = i;
                }

                // the email part may look like affiliation, so it must be placed above affiliation check
                if (titleLineNo >= 0 && IsAuthorNameEmailPair(firstPartData.LineContents[i], out tempAuthNames))
                {
                    if (!isAuthsByEmail && authorsLineNo >= 0)
                    {
                        if (titleLineEnd < authorsLineNo)
                            titleLineEnd = authorsLineNo;
                    }
                    isAuthsByEmail = true;
                    authorsLineNo = i;
                    if (authorNamesByEmail == null)
                        authorNamesByEmail = new List<string>();

                    authorNamesByEmail.AddRange(tempAuthNames);
                    titleEndIsMet = true;
                }
                else if (titleLineNo >= 0 && StringLooksLikeAbstractHeader(firstPartData.LineContents[i]))
                {
                    break;
                }
                else if (titleLineNo >= 0 && StringLooksLikeAffiliation(firstPartData.LineContents[i]))
                {
                    titleEndIsMet = true;
                    // do nothing yet
                    //break;
                }
                else if (!titleEndIsMet && titleLineNo >= 0 && StringLooksLikeTitle(firstPartData.LineContents[i]))
                {
                    titleLineEnd = i;
                }
                else if (titleLineNo >= 0 && !titleEndIsMet && i >= 1 && firstPartData.LineContents[i - 1].EndsWith(":"))
                {
                    titleLineEnd = i;
                }
                else if (titleLineNo >= 0 && i != titleLineNo && !isAuthsByEmail && StringLooksLikeAuthorNames(firstPartData.LineContents[i], out isAuthSure, out tempAuthNames))
                {
                    // TODO: u may turn back the commented lines below
                    //if (!titleEndIsMet && authorsLineNo >= 0 && authorsLineNo >= titleLineEnd)
                    //{
                    //    if(authorNamesRegular != null)
                    //        authorNamesRegular.Clear();
                    //    titleLineEnd = authorsLineNo;
                    //}
                    if(authorNamesRegular == null)
                        authorNamesRegular = new List<string>();
                    authorNamesRegular.AddRange(tempAuthNames);
                    authorsLineNo = i;

                    if (isAuthSure)
                    {
                        titleEndIsMet = true;
                    }

                }
                else if (titleLineNo >= 0 && !titleEndIsMet && authorsLineNo < 0)
                {
                    titleLineEnd = i;
                }
            }

            if (titleLineEnd < 0 && titleLineNo >= 0 && authorsLineNo > titleLineNo)
                titleLineEnd = authorsLineNo - 1;

            string strTitle = "null", strAuthors = "null";
            if (titleLineNo >= 0)
            {
                if (titleLineNo == titleLineEnd)
                {
                    strTitle = firstPartData.LineContents[titleLineNo];
                }
                else
                {
                    strTitle = "";
                    for (int i = titleLineNo; i <= titleLineEnd; i++)
                    {
                        strTitle += firstPartData.LineContents[i] + " ";
                    }

                    strTitle = strTitle.Trim();
                }
            }

            if (authorsLineNo >= 0)
            {
                strAuthors = firstPartData.LineContents[authorsLineNo];
            }

            return new PdfExtractedData { AuthorNames = isAuthsByEmail ? authorNamesByEmail : authorNamesRegular, 
                AuthorsLine = strAuthors, Title = strTitle, Year = year, PubName = pubName };
        }


        private static string[] AbstractPatterns = new[]
        {
            @"^abstract\b", @"^abstract\s{3,}", @"^extended\s+summary$"
        };

        private static bool StringLooksLikeAbstractHeader(string str)
        {
            str = str.Trim().ToLower();
            foreach(var pat in AbstractPatterns)
            {
                var m = Regex.Match(str, pat);
                if (m.Success)
                    return true;
            }

            return false;
        }

        private static PdfFirstPartData BuildFirstPartPdfData(string fileContent)
        {
            var stringReader = new StringReader(fileContent);

            string curLine;
            int lineNo = 0;

            var lineContents = new List<string>();
            var lineLengths = new List<int>();
            var lineStarts = new List<int>();
            var wordsPerLine = new List<int>();

            while ((curLine = stringReader.ReadLine()) != null)
            {
                lineNo++;
                if (IsEmptyLine(curLine))
                {
                    lineContents.Add("");
                    lineLengths.Add(0);
                    lineStarts.Add(0);
                    wordsPerLine.Add(0);
                    continue;
                }

                curLine = StringUtils.UnifyString(StringUtils.RemoveLigatures(curLine)); 
                string mainContent = curLine.Trim(); 
                int mainIdx = curLine.IndexOf(mainContent);
                Debug.Assert(mainIdx >= 0);

                int preLength = mainIdx;
                //int postLength = curLine.Length - (mainIdx + mainContent.Length);

                int numWords = mainContent.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

                lineContents.Add(mainContent);
                lineStarts.Add(preLength);
                lineLengths.Add(mainContent.Length);
                wordsPerLine.Add(numWords);

                if (lineNo > 30)
                    break;
            }

            var firstPartData = new PdfFirstPartData()
            {
                LineContents = lineContents,
                LineLengths = lineLengths,
                LineStarts = lineStarts,
                WordsPerLine = wordsPerLine
            };

            //firstPartData.PdfStyle = DetectPdfStyle(firstPartData);
            return firstPartData;
        }

        //// these happen with some characters that have hats or some signs above, as in spanish e. In these situations
        //// there's one line with a single e in it observed. There are several of them visible when the name contains more than
        //// such characters
        //private static Regex s_emptyLinePattern = new Regex(@"^\s*\S(\s+\S)*\s*$", RegexOptions.Compiled);
        //// these sometimes happen in elsevier papers, the author name superscripts fall in one line earlier
        //private static Regex s_nonesenseLinePattern = new Regex(@"^\s*\p{Ll}(\,[\p{Ll}\p{S}\p{P}])*\s*$", RegexOptions.Compiled);

        // if you can find two consequitive letters then we assume that this line is not empty
        private static Regex s_nonemptyLinePattern = new Regex(@"\p{L}\p{L}", RegexOptions.Compiled);

        private static bool IsEmptyLine(string curLine)
        {
            if (String.IsNullOrWhiteSpace(curLine))
                return true;

            var m = s_nonemptyLinePattern.Match(curLine);
            if (m != null && m.Success)
                return false;

            return true;
        }

        private static bool IsJairStyle(PdfFirstPartData firstPartData)
        {
            if (firstPartData.Count <= 4)
                return false;


            if (firstPartData.LineStarts[0] == 0 && firstPartData.LineContents[0].ToLower().Contains("journal of"))
            {
                if (firstPartData.LineLengths[1] == 0 &&
                    firstPartData.LineLengths[2] == 0 &&
                    firstPartData.LineLengths[3] == 0)
                    return true;
            }

            return false;
        }

        private static bool StringLooksLikeAuthorNames(string str, out bool sure, out List<string> names)
        {
            sure = false;
            names = new List<string>();
            var nameIdx = new List<int>();
            foreach (Match match in s_nameRegex.Matches(str))
            {
                if (match.Success)
                {
                    if (match.Value.ToLower() == "student member")
                        continue;
                    names.Add(StringUtils.ToTitleCase(match.Value.ToLower()));
                    nameIdx.Add(match.Index);
                }
            }

            if (names.Count <= 0)
                return false;

            int startat = 0;

            for (int ni = 0; ni < names.Count; ni++)
            {
                string priorName = str.Substring(startat, nameIdx[ni] - startat);
                if (ni == 0 && !String.IsNullOrWhiteSpace(priorName))
                    return false;

                if (ni > 0 && IsStringWhitespace(priorName) && priorName.Length >= 2)
                {
                    sure = true;
                    return true;
                }

                if (ni > 0 && !StringLooksLikePriorName(priorName))
                    return false;

                // otherwise continue

                startat = nameIdx[ni] + names[ni].Length;
            }

            if (startat < str.Length)
            {
                string postName = str.Substring(startat);
                return StringLooksLikePostName(postName);
            }

            return true;
        }

        private static bool IsAuthorNameEmailPair(string str, out List<string> names)
        {
            names = new List<string>();
            var emails = new List<string>();
            foreach (Match match in s_nameEmailPairRegex.Matches(str))
            {
                if (match.Success)
                {
                    foreach (Capture capt in match.Groups["name"].Captures)
                    {
                        names.Add(capt.Value);
                    }
                    emails.Add(match.Groups["email"].Value);
                }
            }

            if (names.Count > 0)
                return true;
            return false;
        }

        private static readonly string[] PostNamePattrens = new[] 
        {
            @"\bstd.\b", @"\badvisor\b", @"\(advisor\)", @"\bieee\b"
        };

        private static readonly Regex s_regexElsevierPostNamePattern =
            new Regex(@"\p{Ll}(\,[\p{Ll}\p{S}\p{P}])*(\s+\,)?\s*", RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static bool StringLooksLikePostName(string postName)
        {
            string trimmedPostName = postName.Trim().ToLower();

            var elsevierMatch = s_regexElsevierPostNamePattern.Match(trimmedPostName);
            if(elsevierMatch != null && elsevierMatch.Success)
            {
                if (elsevierMatch.Value.Equals(trimmedPostName, StringComparison.Ordinal))
                    return true;
            }

            // support for student and advisor indication in author names))
            foreach (var ngPat in PostNamePattrens)
            {
                var m = Regex.Match(trimmedPostName, ngPat);
                if (m != null && m.Success)
                    return true;
            }

            // post name should contain no letters
            for(int i = 0; i < postName.Length; i++ )
            {
                char ch = postName[i];
                // a number with more than 1 digit is not allowd
                if (Char.IsDigit(ch) && i + 1 < postName.Length && Char.IsDigit(postName[i + 1]))
                    return false;

                if (Char.IsLetter(ch))
                    return false;
            }

            return true;
        }

        private static bool StringLooksLikePriorName(string priorName)
        {
            if (StringLooksLikePostName(priorName))
                return true;

            priorName = priorName.ToLower().Trim();
            string[] words = priorName.Split(new[] { ' ', '\t', ',', ';', '-', '*', '#', '∗', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < words.Length; i++)
            {
                if (words[i] == "and" || StringLooksLikePostName(words[i]))
                    continue;

                return false;
            }

            return true;
        }

        private static bool IsStringWhitespace(string str)
        {
            for (int i = 0; i < str.Length; i++)
                if (!Char.IsWhiteSpace(str[i]))
                    return false;

            return true;
        }

        private static readonly string[] AffiliationPattrens = new[]
        {
            @"\bdepartment\b", @"\bschool\b", @"\buniversity\b", @"\buniversitat\b", @"\buniversidad\b", @"\becole\b",
            @"\bdept\b", @"\blaborator(y|ies)\b",
            @"\binstitute\b", @"\bmicrosoft\b", @"\bgoogle\b", @"\bresearch\b", @"\bfaculty\b", @"\blabs\b",
            @"\bscience(s)?\b", @"\bengineering\b", @"\binformati", @"\btechnolog(y|ies)\b", @"\byahoo\!", @"\bict\b",
            @"\bcent(re|er)\s+(for|of)\b", @"\bcollegium\b", "\bcollege\b"
        };

        private static bool StringLooksLikeAffiliation(string str)
        {
            str = str.ToLower();

            foreach (var ngPat in AffiliationPattrens)
            {
                var m = Regex.Match(str, ngPat);
                if (m != null && m.Success)
                    return true;
            }

            return false;
        }

        private static readonly string[] PdfHeaderPatterns = new []
        {
            @"\bto\s+appear\b", @"\bjournal\s+of\b", @"\bpre\-publication\s+draft\b", @"\bmach\s+learn\b",
            @"\bsubmitted\s+\bto\b", @"\bdon\'t\s+distribute\b", @"\bin\s+press\b", @"\bprinted\s+in\b", 
            @"\bworkshop\s+on\b", @"\bdraft\b", @"\bproc\.\b", @"\binter\.\b", @"\bconf\.\b", @"\bproceeding\b", 
            @"\bconference\b", @"\binternational\b", @"\bnational\b", @"\bdoi\b", @"\bsubmission(s)?\b",
            @"\bpre\-publication\b", @"\bannual\b", @"\bkluwer\b", @"\bieee\b", @"\bmanuscript\b", @"\beditor(s)?\b"
        };

        private static bool StringLooksLikePdfHeader(string str)
        {
            str = str.ToLower();
            

            foreach (Match m in s_headerPageRangesRegex.Matches(str))
            {
                if(m.Success)
                {
                    int num1, num2;
                    if (Int32.TryParse(m.Groups["num1"].Value, out num1) && Int32.TryParse(m.Groups["num2"].Value, out num2) && num1 < num2)
                        return true;
                    
                }
            }

            foreach (var ngPat in PdfHeaderPatterns)
            {
                var m = Regex.Match(str, ngPat);
                if (m != null && m.Success)
                    return true;
            }

            return false;

        }


        private static readonly string[] PatternsOnlyInTitle = new[] 
        {
            @"\bdecision", @"\bprocess", @"\breward", @"\bsocial", @"\bintelligen", @"\bcharacter", @"\blearn", @"\bmachine", @"\breinforce",
            @"\bproblem", @"\baverag", @"\bhierarch", @"\bstochastic", @"\bstatistic", @"\bprobab", @"\bcontinu", @"\btime", @"\bgame", @"\bfuzz(y|i)",
            @"\bobserv", @"\btrain", @"\bsvm(s)?\b", @"\bapproach", @"\bbayes", @"\bmulti", @"\bsystem", @"\bimage", @"\bpicture", @"\bgraph", @"\bneur",
            @"\bevolu", @"\bscatter", @"\bambigui", @"\bdynamic", @"\bstatic", @"\brisk", @"\bmanage", @"\biterat", @"\bby\b", @"\bwith\b",
            @"\btransition", @"\b(de)?centralize(d)?\b", @"\b(dec\-)?(po)?mdp(s)?\b", @"\bintegerat", @"\bmcmc\b", @"\bhybrid", @"\bsolv", @"\bfactor",
            @"\blinear", @"\bsolution", @"\bplanning", @"\befficient", @"\befficiency", @"\bheuristic", @"\bvariable", @"\-order", @"\bretriev(al)?", @"\btrain",
            @"\blatent", @"\bmodel(s|ling|ing)\b", @"\baudio", @"\bvideo", @"\bspeech", @"\bcode(s)?\b", @"\b(en|de)?coding\b", @"\bpossible", 
            @"\bextension", @"\bextend", @"\bpercept", @"\bnois", @"\bsensor"
        };

        private static bool StringLooksLikeTitle(string str)
        {
            if (str.Contains(":") || str.Contains("\""))
                return true;

            str = str.Trim().ToLower();

            var words = str.Split(new [] {' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 1 && words[0].Length >= 2 && Char.IsLetter(words[0][0]) && Char.IsLetter(words[0][1]))
                return true;

            foreach (var ngPat in PatternsOnlyInTitle)
            {
                var m = Regex.Match(str, ngPat);
                if (m != null && m.Success)
                    return true;
            }

            return false;
        }

        private static int FirstNonEmptyLine(List<int> wordsPerLine, int startat)
        {
            for (int i = startat; i < wordsPerLine.Count; i++)
            {
                if (wordsPerLine[i] > 0)
                    return i;
            }

            return -1;
        }

        private static bool IsTwoLongLinesBeforeSomeEmptyLines(List<int> wordsPerLine)
        {
            if (wordsPerLine.Count <= 5)
                return false;

            if (wordsPerLine[0] >= 2 && wordsPerLine[1] >= 2 && wordsPerLine[2] == 0 && wordsPerLine[3] == 0 && wordsPerLine[4] == 0)
                return true;

            return false;
        }

        private static bool IsLongLineBeforeSomeEmptyLines(List<int> wordsPerLine)
        {
            if (wordsPerLine.Count <= 4)
                return false;

            if (wordsPerLine[0] >= 3 && wordsPerLine[1] == 0 && wordsPerLine[2] == 0 && wordsPerLine[3] == 0)
                return true;

            return false;
        }

        private static List<string> FindDoi(string fileContent)
        {
            var dois = new List<string>();
            foreach (Match match in s_doiRegex.Matches(fileContent))
            {
                if (match.Success)
                    dois.Add(match.Value);
            }
            return dois;
        }

    }
}
